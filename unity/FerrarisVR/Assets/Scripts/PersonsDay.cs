using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace Ferraris
{
    [Serializable] public class StoryStop { public string title,text,kind,dialogue,question,answer,follow;public float x,z; }
    [Serializable] public class StoryContent { public string title,introduction,evidence;public StoryStop[] stops; }

    public class PersonsDay : MonoBehaviour
    {
        public StoryContent Content { get; private set; }
        public GuideProgress Progress { get; }=new();
        public StoryPerson Person { get; private set; }
        public bool Ready=>navigation?.Ready==true;
        public bool VoicePlaying=>voice!=null&&voice.isPlaying;
        public bool CanTalk=>Ready&&app.InWorld&&Distance<=GuideProgress.TalkDistance&&Visible;
        public float Distance=>Person!=null&&app.InWorld?Vector3.Distance(app.Player.position,Person.transform.position):float.PositiveInfinity;
        public Vector3 Destination=>navigation.Stops[Mathf.Min(Progress.Step,Content.stops.Length-1)];
        public bool Arrived=>agent!=null&&agent.isOnNavMesh&&!agent.pathPending&&agent.remainingDistance<=agent.stoppingDistance+.15f;
        VisitorUI ui;FerrarisApp app;StoryNavigation navigation;NavMeshAgent agent;
        GameObject panel,hud;UnityEngine.UI.Text direction;AudioSource voice;
        bool evidence,answer,blocked,wasWorld,focused=true,voicePaused,essentialVoice,voiceMuted;int evidencePage;float lastDistance;GuideState lastState;
        string callout;float calloutUntil;bool wasNear,dismissed;

        bool Visible
        {
            get
            {
                if(Person==null)return false;
                var target=Person.transform.position+Vector3.up*1.45f;
                return !Physics.Linecast(app.View.transform.position,target,~0,QueryTriggerInteraction.Ignore);
            }
        }
        void Start()
        {
            app=GetComponent<FerrarisApp>();ui=GetComponent<VisitorUI>();
            Content=app.World.Tableaux.Content;
            hud=ui.Box(new Rect(28,695,760,165));direction=ui.Text(new Rect(48,709,710,92),"",22,hud.transform);
            ui.Button(new Rect(48,807,290,42),"Praat met Marie",Open,hud.transform,21);
            ui.Button(new Rect(370,807,390,42),"Verhaal verlaten",Pause,hud.transform,21);hud.SetActive(false);
            navigation=new StoryNavigation(app.World,app.Area,app.Data,Content.stops);
            if(!Ready)return;
            var go=new GameObject("Marie — illustratief personage");go.transform.SetParent(app.World.transform,false);go.transform.position=navigation.Stops[0];
            Person=go.AddComponent<StoryPerson>();Person.Build();
            agent=go.AddComponent<NavMeshAgent>();agent.enabled=false;agent.agentTypeID=navigation.AgentType;
            agent.speed=1.5f;agent.acceleration=4;agent.angularSpeed=150;agent.radius=.35f;agent.height=1.75f;agent.stoppingDistance=.35f;
            voice=go.AddComponent<AudioSource>();voice.spatialBlend=1;voice.rolloffMode=AudioRolloffMode.Linear;voice.minDistance=1;voice.maxDistance=8;voice.playOnAwake=false;
        }
        void Update()
        {
            if(!Ready)return;
            if(!app.InWorld)
            {
                if(wasWorld){Progress.Pause();StopVoice();}
                wasWorld=false;hud.SetActive(false);return;
            }
            if(!wasWorld)
            {
                agent.enabled=true;
                if(!agent.isOnNavMesh)agent.Warp(Person.transform.position);
                wasWorld=true;
            }
            if(!agent.isOnNavMesh){blocked=true;return;}
            float distance=Distance;bool near=distance<=8&&Visible;
            if(distance>8)dismissed=false;
            if(near&&!wasNear&&Progress.State==GuideState.Available)Say("Dag! Heb je even tijd? Kom gerust dichterbij.","greeting");
            wasNear=near;
            bool suspended=!focused||ui.PanelOpen||ui.HasKeyboardFocus;
            var previous=Progress.State;
            Vector3 heading=agent.steeringTarget-Person.transform.position,toVisitor=app.Player.position-Person.transform.position;
            heading.y=toVisitor.y=0;
            bool visitorAhead=Vector3.Dot(heading.normalized,toVisitor.normalized)>.65f;
            if(!suspended&&!blocked)Progress.Tick(Visible?distance:Mathf.Max(distance,GuideProgress.WaitDistance+1),Arrived,visitorAhead);
            if(previous!=Progress.State)
            {
                if(Progress.State==GuideState.Waiting)Say("Ik wacht hier. Kom maar als je zover bent.","wait");
                if(Progress.State==GuideState.Guiding)Say("Daar ben je. Loop maar mee.","rejoin");
                if(Progress.State==GuideState.Speaking){answer=false;Open();}
            }
            agent.isStopped=!focused||ui.PanelOpen||ui.HasKeyboardFocus||blocked||Progress.State!=GuideState.Guiding;
            agent.updateRotation=!agent.isStopped;
            if(!suspended&&Progress.State==GuideState.Guiding&&!agent.pathPending&&agent.pathStatus!=NavMeshPathStatus.PathComplete)
            {blocked=true;agent.isStopped=true;StopVoice();}
            bool hear=focused&&Visible&&distance<=(essentialVoice?GuideProgress.TalkDistance:8);
            if(voice.isPlaying&&!hear){voice.Pause();voicePaused=true;}
            else if(voicePaused&&hear){voice.UnPause();voicePaused=false;}
            Person.Animate(agent.isStopped?0:agent.velocity.magnitude,app.Player.position,Progress.State==GuideState.Speaking&&!answer,Time.deltaTime);
            Person.SetSpeaking(voice.isPlaying);
            hud.SetActive(!ui.PanelOpen&&!dismissed&&(near||Progress.Running));
            string status=blocked?"Ik vind hier geen doorgang. Probeer later opnieuw.":Progress.State switch
            {
                GuideState.Available=>"Marie staat bij het erf. Kom dichterbij om te praten.",
                GuideState.Speaking=>"Marie vertelt. Open het gesprek om verder te gaan.",
                GuideState.Guiding=>$"Volg Marie naar {Content.stops[Progress.Step].title.ToLowerInvariant()}.",
                GuideState.Waiting=>"Marie wacht op je. Loop rustig naar haar toe.",
                GuideState.Paused=>"Verhaal verlaten. Marie bewaart je plek in de dag.",
                _=>"De dag is rond. Je kunt vrij verder wandelen."
            };
            direction.text=$"Marie • {Mathf.CeilToInt(distance)} m\n{(Time.time<calloutUntil?callout:status)}";
            if(panel!=null&&(lastState!=Progress.State||(distance<=3)!=(lastDistance<=3)))Draw();
            lastState=Progress.State;lastDistance=distance;
        }
        public bool SelectRay(Ray ray)
        {
            if(!CanTalk||!Physics.Raycast(ray,out var hit,5,~0,QueryTriggerInteraction.Collide)||hit.collider.GetComponentInParent<StoryPerson>()!=Person)return false;
            Open();return true;
        }
        public void Open()
        {
            ui.ClosePanel?.Invoke();dismissed=false;evidence=false;answer=false;Draw();
            if(!CanTalk)return;
            if(Progress.State==GuideState.Available)Say("Dag, ik ben Marie. Ik wil de oogst binnenhalen voor het weer omslaat. Loop je een stukje mee?","invitation");
            else if(Progress.State==GuideState.Speaking)Say(Content.stops[Progress.Step].dialogue,$"stop-{Progress.Step}");
        }
        public void Close(){ui.RemovePanel(panel);panel=null;ui.PanelOpen=false;ui.ClosePanel=null;ui.ClearKeyboardFocus();StopVoice();}
        public void Pause(){Progress.Pause();Halt();StopVoice();calloutUntil=0;dismissed=true;hud?.SetActive(false);Close();}
        void Halt(){if(agent!=null&&agent.isOnNavMesh){agent.isStopped=true;agent.ResetPath();agent.velocity=Vector3.zero;}}
        public void SuspendForMap()
        {
            Progress.Pause();StopVoice();calloutUntil=0;
            Halt();
            if(panel!=null)Close();
        }
        public void Resume()
        {
            if(!CanTalk)return;
            bool accepted=Progress.State==GuideState.Available?Progress.Begin(Distance):Progress.Resume(Distance);
            if(!accepted)return;
            blocked=false;
            if(Progress.State is GuideState.Guiding or GuideState.Waiting){SetDestination();Close();}
            else {Draw();Say(Content.stops[Progress.Step].dialogue,$"stop-{Progress.Step}");}
        }
        public void Advance()
        {
            if(!CanTalk||!Progress.Continue(Distance,Content.stops.Length))return;
            if(Progress.State==GuideState.Completed){StopVoice();Draw();return;}
            string follow=Content.stops[Progress.Step-1].follow;Close();SetDestination();Say(follow,$"follow-{Progress.Step-1}");
        }
        void SetDestination()
        {
            var path=new NavMeshPath();
            blocked=!agent.isOnNavMesh||!agent.CalculatePath(Destination,path)||path.status!=NavMeshPathStatus.PathComplete;
            if(!blocked)agent.SetPath(path);
        }
        public void ResetStory()
        {
            Progress.Reset();blocked=false;StopVoice();calloutUntil=0;
            if(agent!=null){if(agent.isOnNavMesh){agent.ResetPath();agent.Warp(navigation.Stops[0]);}else Person.transform.position=navigation.Stops[0];}
            Draw();
        }
        void FindMarie()
        {
            var p=Person.transform.position;Close();app.Locate(p.x,p.z);app.EnterWorld(p.x,p.z-2);
        }
        void Say(string text,string clip)
        {
            callout=text;calloutUntil=Time.time+8;
            if(voice==null)return;
            voice.Stop();voicePaused=false;essentialVoice=clip.StartsWith("stop-")||clip.StartsWith("answer-")||clip=="invitation";
            voice.clip=Resources.Load<AudioClip>("Discovery/Voice/Marie/"+clip);
            if(voice.clip!=null&&focused&&Visible&&Distance<=(essentialVoice?GuideProgress.TalkDistance:8))voice.Play();
        }
        void StopVoice(){voicePaused=false;if(voice!=null)voice.Stop();}
        public void SetVoiceMuted(bool muted){voiceMuted=muted;if(voice!=null)voice.mute=muted;}
        void OnApplicationFocus(bool hasFocus){focused=hasFocus;if(!hasFocus){StopVoice();if(agent!=null&&agent.isOnNavMesh)agent.isStopped=true;}}
        void Draw()
        {
            ui.RemovePanel(panel);panel=ui.Box(new Rect(28,180,620,680));ui.PanelRoot=panel.transform;ui.PanelOpen=true;ui.ClosePanel=Close;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            ui.Text(new Rect(48,197,485,40),Content.title,28,panel.transform);ui.Button(new Rect(550,197,76,40),"Sluiten",Close,panel.transform,19);
            if(!Ready){ui.Text(new Rect(48,255,575,300),"Marie kan deze route niet lopen. Je kunt het landschap vrij verkennen en het verhaal later opnieuw proberen.",25,panel.transform);return;}
            bool end=Progress.State==GuideState.Completed;
            var stop=Content.stops[Mathf.Min(Progress.Step,Content.stops.Length-1)];
            string text=evidence?string.Join("\n\n",Content.evidence.Split("\n\n").Skip(evidencePage*3).Take(3)):
                end?"De dag is rond\n\nMarie: Bedankt voor je gezelschap. Je kunt nu vrij verder wandelen.\n\nDit was een verzonnen dag, geen gevonden levensverhaal.":
                !CanTalk?$"{Content.introduction}\n\n{(app.InWorld?$"Marie staat op {Mathf.CeilToInt(Distance)} meter. Loop naar haar toe om te praten.":"Marie wacht bij haar laatste plek. Zoek haar in het landschap.")}\n\n{(Progress.State==GuideState.Paused?"Je stop en Marie's plek zijn bewaard.":"Je kiest zelf of je met haar meeloopt.")}":
                Progress.State==GuideState.Available?"Marie: Dag, ik ben Marie. Ik wil de oogst binnenhalen voor het weer omslaat. Loop je een stukje mee?\n\n"+Content.introduction:
                Progress.State==GuideState.Paused?"Marie: Daar ben je weer. Zullen we verdergaan waar we gebleven waren?\n\nJe kunt ook opnieuw beginnen.":
                Progress.State==GuideState.Speaking?$"{Progress.Step+1}/{Content.stops.Length} — {stop.title}\n\nMarie: {(answer?stop.answer:stop.dialogue)}":
                blocked?"Marie: Ik vind hier geen doorgang. Je kunt het verhaal verlaten en later hervatten.":"Marie: Loop maar mee. Ik wacht als je even achterblijft.";
            ui.Text(new Rect(48,255,575,325),text,22,panel.transform);
            ui.Button(new Rect(48,585,275,44),"Hoe weten we dit?",()=>{evidence=!evidence;Draw();},panel.transform);
            if(evidence)ui.Button(new Rect(335,585,290,44),evidencePage==0?"Volgende bronnen":"Vorige bronnen",()=>{evidencePage=1-evidencePage;Draw();},panel.transform);
            else ui.Button(new Rect(345,585,280,44),voiceMuted?"Stem: uit":"Stem: aan",()=>{SetVoiceMuted(!voiceMuted);Draw();},panel.transform,21);
            if(!evidence&&CanTalk&&Progress.State==GuideState.Speaking)
            {
                ui.Button(new Rect(48,640,320,44),answer?"Terug naar het verhaal":stop.question,()=>{answer=!answer;Draw();Say(answer?stop.answer:stop.dialogue,answer?$"answer-{Progress.Step}":$"stop-{Progress.Step}");},panel.transform,22);
                ui.Button(new Rect(385,640,240,44),"Nog eens vertellen",()=>Say(answer?stop.answer:stop.dialogue,answer?$"answer-{Progress.Step}":$"stop-{Progress.Step}"),panel.transform,21);
            }
            if(end)ui.Button(new Rect(48,710,290,48),"Opnieuw beginnen",ResetStory,panel.transform);
            else if(!app.InWorld)ui.Button(new Rect(48,710,240,48),"Zoek Marie",FindMarie,panel.transform);
            else if(CanTalk)
            {
                if(Progress.State is GuideState.Available or GuideState.Paused)ui.Button(new Rect(48,710,240,48),Progress.State==GuideState.Available?"Ik loop mee":"Hervatten",Resume,panel.transform);
                else if(Progress.State==GuideState.Speaking)ui.Button(new Rect(48,710,240,48),Progress.Step==Content.stops.Length-1?"Rond de dag af":"Ik loop mee",Advance,panel.transform);
                else ui.Button(new Rect(48,710,240,48),"Loop verder",Close,panel.transform);
            }
            if(Progress.State==GuideState.Paused)ui.Button(new Rect(48,640,290,44),"Opnieuw beginnen",ResetStory,panel.transform,21);
            if(!end)ui.Button(new Rect(420,710,204,48),Progress.State==GuideState.Available?"Niet nu":"Verhaal verlaten",Pause,panel.transform,21);
            ui.Text(new Rect(48,782,575,65),"Verzonnen personage • Computerstem\nSluiten: gesprek dicht. Verlaten: stop en bewaar je plek.",19,panel.transform);
        }
        void OnDestroy(){navigation?.Dispose();if(Person!=null)Destroy(Person.gameObject);}
    }
}
