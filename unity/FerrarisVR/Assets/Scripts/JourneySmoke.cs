using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Ferraris
{
    // Runs the real runtime in a built player, with screenshots and a nonzero
    // exit on failure. No headless/renderless substitutes for visual evidence.
    public class JourneySmoke : MonoBehaviour
    {
        IEnumerator Start()
        {
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../../artifacts"));
            string[] args=Environment.GetCommandLineArgs();
            int i=Array.IndexOf(args,"-evidence-dir");if(i>=0&&i+1<args.Length)output=args[i+1];Directory.CreateDirectory(output);
            StartCoroutine(Record(output));
            var app=FerrarisApp.Instance;
            yield return new WaitForSeconds(1);
            if(!Check(app.Ready&&!app.InWorld,"Map bootstrap",output))yield break;
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"01-map.png"));yield return new WaitForSeconds(.4f);
            // Feed real Input System events, including a whole drag within one
            // frame. This regression failed when input was polled only per frame.
            var mouse=InputSystem.AddDevice<Mouse>();var keyboard=InputSystem.AddDevice<Keyboard>();
            app.TracePointer=true;
            Vector2 button=new(200,41);
            InputSystem.QueueStateEvent(mouse,new MouseState{position=button}.WithButton(MouseButton.Left));
            InputSystem.QueueStateEvent(mouse,new MouseState{position=button});
            yield return null;yield return null;
            if(!Check(Mathf.Abs(app.MapZoom-1.4f)<.01f,"Input System toolbar click",output))yield break;
            Vector2 centre=new(Screen.width/2f,Screen.height/2f);
            InputSystem.QueueStateEvent(mouse,new MouseState{position=centre}.WithButton(MouseButton.Left));
            InputSystem.QueueStateEvent(mouse,new MouseState{position=centre+new Vector2(60,30)}.WithButton(MouseButton.Left));
            InputSystem.QueueStateEvent(mouse,new MouseState{position=centre+new Vector2(60,30)});
            yield return null;yield return null;
            Debug.Log($"DRAG_RESULT inWorld={app.InWorld} pan={app.MapPan}");
            if(!Check(!app.InWorld&&app.MapPan.magnitude>10,"Same-frame drag pans instead of selecting",output))yield break;
            // Reset through the actual toolbar, then test wheel input.
            button=new Vector2(330,41);
            InputSystem.QueueStateEvent(mouse,new MouseState{position=button}.WithButton(MouseButton.Left));InputSystem.QueueStateEvent(mouse,new MouseState{position=button});
            yield return null;yield return null;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=centre,scroll=new Vector2(0,120)});
            yield return null;yield return null;
            if(!Check(app.MapZoom>1.3f,"Input System wheel zoom",output))yield break;
            app.ZoomMap(1/app.MapZoom);app.PanMap(-app.MapPan);
            app.ZoomMap(2);app.PanMap(new Vector2(50,40));
            if(!Check(app.MapZoom==2 && app.MapPan==new Vector2(50,40),"Map pan and zoom",output))yield break;
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"02-map-zoom.png"));yield return new WaitForSeconds(.4f);
            Vector3 selectedScreen=app.View.WorldToScreenPoint(new Vector3(50,-50,0));
            InputSystem.QueueStateEvent(mouse,new MouseState{position=selectedScreen}.WithButton(MouseButton.Left));
            InputSystem.QueueStateEvent(mouse,new MouseState{position=selectedScreen});
            yield return new WaitForSeconds(.4f);
            if(!Check(app.InWorld&&app.Player!=null&&app.World.TerrainObject.activeInHierarchy,"World bootstrap",output))yield break;
            if(!Check(Mathf.Abs(app.Player.position.x-50)<26&&Mathf.Abs(app.Player.position.z+50)<26,"Selected position -> spawn",output))yield break;
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"03-world.png"));yield return new WaitForSeconds(.4f);
            // Walk on a known open field, away from building collisions.
            app.EnterWorld(-300,-100);Vector3 start=app.Player.position;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));
            yield return new WaitForSeconds(2);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
            if(!Check(Vector3.Distance(start,app.Player.position)>3,"Grounded locomotion",output))yield break;
            if(!Check(app.Player.position.y>=app.Area.Height(app.Player.position.x,app.Player.position.z)-.1f,"Terrain support",output))yield break;
            // Inspect the landmark and grazing animals in the actual player.
            app.enabled=false;
            app.View.transform.position=new Vector3(42,app.Area.Height(42,-22)+14,-22);app.View.transform.LookAt(new Vector3(2,15,26));
            yield return new WaitForSeconds(.7f);ScreenCapture.CaptureScreenshot(Path.Combine(output,"06-church.png"));yield return new WaitForSeconds(.4f);
            var animal=FindAnyObjectByType<GrazingAnimal>();
            if(animal!=null){Vector3 p=animal.transform.parent.position;app.View.transform.position=p+new Vector3(3,1.5f,4);app.View.transform.LookAt(p+Vector3.up*.8f);yield return new WaitForSeconds(.7f);ScreenCapture.CaptureScreenshot(Path.Combine(output,"07-pasture.png"));yield return new WaitForSeconds(.4f);}
            // Top-down render uses the same world meshes, enabling map comparison.
            app.enabled=false;RenderSettings.fog=false;
            app.View.transform.position=new Vector3(0,800,0);app.View.transform.rotation=Quaternion.Euler(90,0,0);app.View.orthographic=true;app.View.orthographicSize=550;
            yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,"04-world-topdown.png"));yield return new WaitForSeconds(.4f);
            app.enabled=true;RenderSettings.fog=true;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));yield return null;yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
            if(!Check(!app.InWorld && app.View.orthographic,"Return to map",output))yield break;
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"05-return.png"));yield return new WaitForSeconds(.5f);
            var home=app.GetComponent<HomeSearch>();var ui=app.GetComponent<VisitorUI>();
            home.Toggle();yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.I));
            foreach(char letter in "Winksele")InputSystem.QueueTextEvent(keyboard,letter);
            yield return null;yield return null;
            if(!Check(home.Query=="Winksele"&&home.IsOpen&&!app.GetComponent<ObjectDiscovery>().Active,"Address typing does not trigger global shortcuts",output))yield break;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());home.SetQuery("Dalenstraat");yield return null;
            if(!Check(ui.PanelOpen,"Address search panel opens",output))yield break;
            var addresses=HomeSearch.Search(app.GetComponent<AddressDisplay>().Book,"Dalenstraat");
            var address=Array.Find(addresses,a=>HomeSearch.Covered(a,app.Area.size));home.Choose(address);yield return null;
            if(!Check(!app.InWorld&&Mathf.Abs(app.Selected.x-address.x)<.01f,"Address locates original map point",output))yield break;
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"08-home-search.png"));yield return new WaitForSeconds(.5f);
            // Click the actual entry control through the same hit handling as desktop input.
            ui.ClickScreen(new Vector2(430,1000-547));yield return new WaitForSeconds(.4f);
            if(!Check(app.InWorld&&!ui.PanelOpen&&app.Selected.x==address.x,"Address entry button",output))yield break;
            var church=Array.Find(app.Data.buildings,b=>b.kind=="church");app.Locate(church.x,church.z);app.EnterWorld(church.x,church.z);
            if(!Check(app.Selected.x==church.x&&app.Selected.z==church.z&&Vector2.Distance(new Vector2(app.Player.position.x,app.Player.position.z),new Vector2(church.x,church.z))>.5f,"Safe spawn preserves original building coordinate",output))yield break;
            app.ReturnToMap();
            var day=app.GetComponent<PersonsDay>();
            if(!Check(day.Ready,"Guide has five connected walkable stops",output))yield break;
            var marie=day.Person;var nav=marie.GetComponent<UnityEngine.AI.NavMeshAgent>();
            var first=marie.transform.position;app.EnterWorld(first.x,first.z-2);yield return null;
            app.View.transform.LookAt(first+Vector3.up*1.5f);
            if(!Check(day.CanTalk&&day.Progress.State==GuideState.Available,"Approach does not accept the invitation",output))yield break;
            day.Open();day.Resume();yield return null;
            if(!Check(day.Progress.State==GuideState.Speaking,"Visitor accepts Marie's invitation",output))yield break;
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"09-story.png"));yield return new WaitForSeconds(.4f);
            day.Advance();yield return new WaitForSeconds(1);
            if(!Check(Vector3.Distance(first,marie.transform.position)>.2f,"Marie physically leads the visitor",output))yield break;
            app.EnterWorld(-300,-100);yield return new WaitForSeconds(.3f);
            if(!Check(day.Progress.State==GuideState.Waiting&&nav.isStopped,"Marie waits when the visitor falls behind",output))yield break;
            var waiting=marie.transform.position;day.Pause();yield return new WaitForSeconds(.3f);
            if(!Check(!ui.PanelOpen&&!day.Progress.Running&&Vector3.Distance(waiting,marie.transform.position)<.01f,"Leaving stops the guide immediately",output))yield break;
            app.EnterWorld(waiting.x,waiting.z-2);yield return null;day.Open();day.Resume();yield return null;
            if(!Check(day.Person==marie&&day.Progress.Step==1,"Resume preserves person, outfit and checkpoint",output))yield break;
            // Accelerate travel only in the smoke run; the shipped guide walks at 1.5 m/s.
            nav.speed=60;nav.acceleration=100;
            for(int stop=1;stop<day.Content.stops.Length;stop++)
            {
                float deadline=Time.time+30;
                while(day.Progress.State!=GuideState.Speaking&&Time.time<deadline)
                {
                    var p=marie.transform.position;app.EnterWorld(p.x,p.z-1.8f);
                    yield return null;
                }
                if(!Check(day.Progress.State==GuideState.Speaking&&day.Progress.Step==stop,"Guide follows a complete route to scene "+stop,output))yield break;
                if(!Check(TextFits(ui),"Guide dialogue fits scene "+stop,output))yield break;
                day.Advance();yield return null;
            }
            if(!Check(day.Progress.State==GuideState.Completed,"Story reaches ending",output))yield break;
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"10-story-ending.png"));yield return new WaitForSeconds(.5f);day.Close();app.ReturnToMap();
            var sound=app.GetComponent<LandscapeSound>();sound.Open();yield return null;
            if(!Check(sound.Sites.Count==3,"Three contextual sound sources",output))yield break;
            sound.SetVolume(.4f);sound.SetMuted(true);
            if(!Check(sound.Sites.TrueForAll(s=>s.source.volume==0),"Mute reaches every ambient source",output))yield break;
            sound.SetMuted(false);
            if(!Check(sound.Sites.TrueForAll(s=>Mathf.Abs(s.source.volume-.4f)<.01f&&s.source.spatialBlend==1&&s.source.rolloffMode==AudioRolloffMode.Linear),"Volume and native positional falloff",output))yield break;
            var sheep=sound.Sites[1];sound.Close();app.EnterWorld(sheep.source.transform.position.x+6,sheep.source.transform.position.z);yield return new WaitForSeconds(.5f);
            sound.Open();ScreenCapture.CaptureScreenshot(Path.Combine(output,"11-sound.png"));yield return new WaitForSeconds(.7f);sound.Close();app.ReturnToMap();
            var names=app.GetComponent<PlaceNames>();names.Open();yield return null;
            for(int name=0;name<names.Content.entries.Length;name++)
            {
                names.Choose(name);yield return null;
                if(!Check(!app.InWorld&&app.Selected.x==names.Content.entries[name].x,"Place name map link "+name,output))yield break;
                if(!Check(TextFits(ui),"Place name readable text "+name,output))yield break;
                ui.ClickScreen(new Vector2(170,197));yield return null;
                if(!Check(TextFits(ui),"Place name readable source "+name,output))yield break;
                if(name==0){ScreenCapture.CaptureScreenshot(Path.Combine(output,"12-place-name.png"));yield return new WaitForSeconds(.5f);}
                ui.ClickScreen(new Vector2(410,197));yield return null;
                if(!Check(app.InWorld&&!ui.PanelOpen,"Place name world link "+name,output))yield break;
                names.Open();
            }
            names.Close();app.ReturnToMap();
            var discovery=app.GetComponent<ObjectDiscovery>();
            // Each rendered volume type is selected using the same ray picker as the controller.
            foreach(string kind in new[]{"house","barn","farmhouse","church","tree","cow","sheep","barrel","woodpile","fence"})
            {
                app.EnterWorld(0,-70);discovery.Toggle();PickedObject picked=null;
                foreach(var volume in discovery.Picker.Volumes)
                {
                    if(volume.key!=kind)continue;
                    var ray=new Ray(volume.centre+volume.rotation*new Vector3(0,0,-volume.size.z/2-.05f),volume.rotation*Vector3.forward);
                    var candidate=discovery.Picker.Ray(ray);if(candidate.key==kind){discovery.SelectRay(ray);picked=candidate;break;}
                }
                if(!Check(picked!=null&&discovery.Selection.key==kind,"World object selection "+kind,output))yield break;
                discovery.Close(true);
            }
            // Land-cover selection follows actual map click dispatch, including unknown terrain.
            foreach(string kind in new[]{"road","soil","crop","grass","orchard","terrain"})
            {
                app.ReturnToMap();app.ZoomMap(1/app.MapZoom);app.PanMap(-app.MapPan);discovery.Toggle();MapPoint point=null;
                for(float x=-480;x<480&&point==null;x+=12)for(float z=-480;z<480&&point==null;z+=12)if(discovery.Picker.Map(x,z).key==kind)point=new MapPoint(x,z);
                if(!Check(point!=null,"Map sample exists "+kind,output))yield break;
                app.SelectUV(point.x/app.Area.size+.5f,point.z/app.Area.size+.5f);yield return null;
                if(!Check(!app.InWorld&&discovery.Selection.key==kind,"Map object selection "+kind,output))yield break;
                discovery.Close(true);
            }
            app.EnterWorld(0,-70);discovery.SelectRay(new Ray(app.View.transform.position+Vector3.up*40,Vector3.up));
            if(!Check(discovery.Selection.key=="sky","Sky selection has illustrative content",output))yield break;
            foreach(var definition in discovery.Content.entries)
            {
                discovery.Show(new PickedObject{key=definition.keys[0],point=Vector3.zero,radius=3});yield return null;
                if(!Check(discovery.Page==0&&TextFits(ui),"Readable summary "+definition.keys[0],output))yield break;
                ui.ClickScreen(new Vector2(180,306));yield return null;
                if(!Check(discovery.Page==1&&discovery.Section==-1&&TextFits(ui),"Expand only on request "+definition.keys[0],output))yield break;
                for(int section=0;section<definition.sections.Length;section++)
                {
                    ui.ClickScreen(new Vector2(330,1000-(375+section*45)));yield return null;
                    if(!Check(discovery.Section==section&&TextFits(ui),"Readable section "+definition.keys[0]+" / "+section,output))yield break;
                    if(definition.keys[0]=="church"&&section==3){ScreenCapture.CaptureScreenshot(Path.Combine(output,"13-object-detail.png"));yield return new WaitForSeconds(.5f);}
                    ui.ClickScreen(new Vector2(180,296));yield return null;
                }
            }
            // Full legend paging, including unmapped categories and abbreviations.
            ui.ClickScreen(new Vector2(330,625));yield return null;ui.ClickScreen(new Vector2(480,296));yield return null;
            for(int page=0;page<25;page++)
            {
                if(!Check(discovery.Page==2&&TextFits(ui),"Readable full legend page "+page,output))yield break;
                if(page<24){ui.ClickScreen(new Vector2(320,244));yield return null;}
            }
            discovery.Close(true);
            // Render and interact with the exact world-space canvas used in Quest.
            app.enabled=false;ui.UseWorldSpace(app.View);discovery.Show(new PickedObject{key="church",point=new Vector3(2,0,26),radius=10});yield return null;
            bool RayClick(float x,float y)
            {
                Vector3 target=ui.Root.TransformPoint(new Vector3(x-720,500-y,0));return ui.ClickRay(new Ray(app.View.transform.position,(target-app.View.transform.position).normalized),true,out _);
            }
            if(!Check(RayClick(180,694)&&discovery.Page==1,"World-space controller ray opens details",output))yield break;yield return null;
            if(!Check(RayClick(330,375)&&discovery.Section==0,"World-space ray selects a section",output))yield break;yield return null;
            if(!Check(TextFits(ui),"World-space text layout",output))yield break;
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"14-world-space-ui.png"));yield return new WaitForSeconds(.7f);
            if(!Check(RayClick(145,817)&&discovery.Page==0,"World-space ray returns to summary",output))yield break;yield return null;
            if(!Check(RayClick(720,217)&&!ui.PanelOpen&&!discovery.Active,"World-space ray closes inspection",output))yield break;
            InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(keyboard);
            File.WriteAllText(Path.Combine(output,"journey.json"),"{\"passed\":true,\"checks\":[\"map load\",\"toolbar click\",\"same-frame drag\",\"wheel zoom\",\"mouse raycast selection\",\"world spawn\",\"W key grounded movement\",\"Escape return\"],\"hardwareVRVerified\":false}");
            Debug.Log("FERRARIS_JOURNEY_PASS");Application.Quit(0);
        }
        static bool TextFits(VisitorUI ui)
        {
            Canvas.ForceUpdateCanvases();bool ok=true;
            foreach(var text in ui.Root.GetComponentsInChildren<UnityEngine.UI.Text>())
                if(text.preferredHeight>text.rectTransform.rect.height+2){Debug.LogError($"FERRARIS_TEXT_CLIPPED {text.text} needs {text.preferredHeight} has {text.rectTransform.rect.height}");ok=false;}
            return ok;
        }
        IEnumerator Record(string output)
        {
            string frames=Path.Combine(output,"journey-frames");Directory.CreateDirectory(frames);
            foreach(string old in Directory.GetFiles(frames,"frame-*.png"))File.Delete(old);
            for(int frame=0;frame<180;frame++){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(frames,$"frame-{frame:D4}.png"));yield return new WaitForSeconds(.5f);}
        }
        bool Check(bool ok,string check,string output)
        {
            Debug.Log($"FERRARIS_CHECK {check}: {ok}");
            if(!ok){File.WriteAllText(Path.Combine(output,"journey.json"),"{\"passed\":false,\"failed\":\""+check+"\"}");Application.Quit(1);}
            return ok;
        }
    }
}
