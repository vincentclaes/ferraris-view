using System;
using System.Linq;
using UnityEngine;
namespace Ferraris
{
    [Serializable] public class StoryStop { public string title,text,kind;public float x,z; }
    [Serializable] public class StoryContent { public string title,introduction,evidence;public StoryStop[] stops; }
    public class StoryProgress
    {
        public int Step { get; private set; }
        public bool Running { get; private set; }
        public bool Complete(int count)=>Step>=count;
        public void Resume()=>Running=true;
        public void Pause()=>Running=false;
        public bool Advance(bool arrived,int count){if(!Running||!arrived||Complete(count))return false;Step++;return true;}
        public void Reset(){Step=0;Running=false;}
    }
    public class PersonsDay : MonoBehaviour
    {
        public StoryContent Content { get; private set; }
        public StoryProgress Progress { get; }=new();
        VisitorUI ui;FerrarisApp app;GameObject panel,beacon,hud;UnityEngine.UI.Text direction;bool evidence;int evidencePage;int lastMetres=-1;
        public float Distance=>app.InWorld&&!Progress.Complete(Content.stops.Length)?Vector2.Distance(new Vector2(app.Player.position.x,app.Player.position.z),new Vector2(Content.stops[Progress.Step].x,Content.stops[Progress.Step].z)):float.PositiveInfinity;
        void Start()
        {
            app=GetComponent<FerrarisApp>();ui=GetComponent<VisitorUI>();Content=JsonUtility.FromJson<StoryContent>(Resources.Load<TextAsset>("Discovery/day").text);
            hud=ui.Box(new Rect(835,765,580,95));direction=ui.Text(new Rect(852,775,545,76),"",22,hud.transform);hud.SetActive(false);
            beacon=GameObject.CreatePrimitive(PrimitiveType.Sphere);Destroy(beacon.GetComponent<SphereCollider>());beacon.name="Verhaalrichtpunt";beacon.transform.localScale=Vector3.one*1.3f;beacon.GetComponent<Renderer>().material=new Material(Shader.Find("Unlit/Color")){color=new Color(1,.68f,.16f)};beacon.SetActive(false);
        }
        void Update()
        {
            bool active=Progress.Running&&!Progress.Complete(Content.stops.Length)&&app.InWorld;
            beacon.SetActive(active);hud.SetActive(active&&!ui.PanelOpen);
            if(!active)return;
            var stop=Content.stops[Progress.Step];beacon.transform.position=new Vector3(stop.x,app.Area.Height(stop.x,stop.z)+3,stop.z);
            int metres=Mathf.RoundToInt(Distance);
            direction.text=$"Dag van Marie • {Progress.Step+1}/{Content.stops.Length}: {stop.title}\n{metres} m naar het gouden richtpunt • Ontdek: volg de dag";
            if(panel!=null&&metres!=lastMetres&&(metres<=22||lastMetres<=22))Draw();lastMetres=metres;
        }
        public void Open(){ui.ClosePanel?.Invoke();evidence=false;Draw();}
        public void Close(){ui.RemovePanel(panel);panel=null;ui.PanelOpen=false;ui.ClosePanel=null;ui.ClearKeyboardFocus();}
        public void Pause(){Progress.Pause();Close();}
        public void Resume(){Progress.Resume();if(!app.InWorld&&!Progress.Complete(Content.stops.Length)){var s=Content.stops[Progress.Step];app.Locate(s.x,s.z);app.EnterWorld(s.x,s.z);}Draw();}
        public void Advance(){if(Progress.Advance(Distance<=22,Content.stops.Length))Draw();}
        void Draw()
        {
            ui.RemovePanel(panel);panel=ui.Box(new Rect(28,180,760,680));ui.PanelRoot=panel.transform;ui.PanelOpen=true;ui.ClosePanel=Close;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            ui.Text(new Rect(48,197,610,40),Content.title,28,panel.transform);ui.Button(new Rect(690,197,76,40),"Sluiten",Close,panel.transform,19);
            bool end=Progress.Complete(Content.stops.Length);
            string text=evidence?string.Join("\n\n",Content.evidence.Split("\n\n").Skip(evidencePage*3).Take(3)):end?"De dag is rond\nMarie legt haar mand neer. Van erf naar akker, boomgaard en dorp: voedsel, werk en ontmoetingen verbinden het landschap.\n\nDit was een verzonnen dag, geen gevonden levensverhaal. Welke taak zou jou het meeste tijd kosten? Je kunt nu vrij verder wandelen.":$"{Content.introduction}\n\nStop {Progress.Step+1}/{Content.stops.Length} — {Content.stops[Progress.Step].title}\n{Content.stops[Progress.Step].text}";
            ui.Text(new Rect(48,255,708,380),text,25,panel.transform);
            ui.Button(new Rect(48,645,290,46),"Hoe weten we dit?",()=>{evidence=!evidence;Draw();},panel.transform);
            if(evidence)ui.Button(new Rect(360,645,290,46),evidencePage==0?"Volgende bronnen":"Vorige bronnen",()=>{evidencePage=1-evidencePage;Draw();},panel.transform);
            if(end){ui.Button(new Rect(48,710,290,48),"Opnieuw beginnen",()=>{Progress.Reset();Draw();},panel.transform);return;}
            var stop=Content.stops[Progress.Step];
            if(!Progress.Running)ui.Button(new Rect(48,710,230,48),Progress.Step==0?"Start de dag":"Hervat de dag",Resume,panel.transform);
            else
            {
                ui.Button(new Rect(48,710,230,48),"Pauzeer de dag",Pause,panel.transform);
                if(Distance<=22)ui.Button(new Rect(300,710,240,48),"Gelezen, volgende",Advance,panel.transform);
                else ui.Button(new Rect(300,710,240,48),"Loop verder",Close,panel.transform);
            }
            ui.Button(new Rect(560,710,204,48),"Toon op kaart",()=>{app.Locate(stop.x,stop.z);Close();},panel.transform,22);
            ui.Text(new Rect(48,780,700,60),"Loop naar het gouden richtpunt en open dit verhaal weer.\nSluiten laat je vrij wandelen; pauzeren bewaart je stop.",21,panel.transform);
        }
        void OnDestroy(){if(beacon!=null)Destroy(beacon);}
    }
}
