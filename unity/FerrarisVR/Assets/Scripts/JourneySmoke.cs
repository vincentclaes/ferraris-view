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
            home.Toggle();home.SetQuery("Dalenstraat");yield return null;
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
            var day=app.GetComponent<PersonsDay>();day.Open();day.Resume();yield return new WaitForSeconds(.3f);
            day.Pause();if(!Check(!ui.PanelOpen&&!day.Progress.Running,"Story pauses for free exploration",output))yield break;
            day.Open();day.Resume();
            for(int stop=0;stop<day.Content.stops.Length;stop++)
            {
                var target=day.Content.stops[stop];app.EnterWorld(target.x,target.z);day.Open();yield return new WaitForSeconds(.4f);
                if(stop==0){ScreenCapture.CaptureScreenshot(Path.Combine(output,"09-story.png"));yield return new WaitForSeconds(.5f);}
                ui.ClickScreen(new Vector2(420,1000-733));yield return null;
                if(!Check(day.Progress.Step==stop+1,"Story stop "+(stop+1),output))yield break;
            }
            if(!Check(day.Progress.Complete(day.Content.stops.Length),"Story reaches ending",output))yield break;
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"10-story-ending.png"));yield return new WaitForSeconds(.5f);day.Close();app.ReturnToMap();
            var sound=app.GetComponent<LandscapeSound>();sound.Open();yield return null;
            if(!Check(sound.Sites.Count==3,"Three contextual sound sources",output))yield break;
            sound.SetVolume(.4f);sound.SetMuted(true);
            if(!Check(sound.Sites.TrueForAll(s=>s.source.volume==0),"Mute reaches every ambient source",output))yield break;
            sound.SetMuted(false);
            if(!Check(sound.Sites.TrueForAll(s=>Mathf.Abs(s.source.volume-.4f)<.01f&&s.source.spatialBlend==1&&s.source.rolloffMode==AudioRolloffMode.Linear),"Volume and native positional falloff",output))yield break;
            var sheep=sound.Sites[1];sound.Close();app.EnterWorld(sheep.source.transform.position.x+6,sheep.source.transform.position.z);yield return new WaitForSeconds(.5f);
            sound.Open();ScreenCapture.CaptureScreenshot(Path.Combine(output,"11-sound.png"));yield return new WaitForSeconds(.7f);sound.Close();app.ReturnToMap();
            InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(keyboard);
            File.WriteAllText(Path.Combine(output,"journey.json"),"{\"passed\":true,\"checks\":[\"map load\",\"toolbar click\",\"same-frame drag\",\"wheel zoom\",\"mouse raycast selection\",\"world spawn\",\"W key grounded movement\",\"Escape return\"],\"hardwareVRVerified\":false}");
            Debug.Log("FERRARIS_JOURNEY_PASS");Application.Quit(0);
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
