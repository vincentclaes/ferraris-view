#if UNITY_STANDALONE || UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ferraris
{
    // Replay controller bindings in a rendered desktop player. No XR compositor,
    // stereo, device tracking or hardware performance is simulated or certified.
    public static class XRInputStudy
    {
        static void Set<T>(InputDevice device,string control,T value) where T:struct =>
            InputSystem.QueueDeltaStateEvent((InputControl<T>)device[control],value);
        static IEnumerator Frames(int count){for(int i=0;i<count;i++)yield return null;}
        static bool ControllerText(VisitorUI ui)
        {
            Canvas.ForceUpdateCanvases();bool instructions=false;
            foreach(var text in ui.Root.GetComponentsInChildren<UnityEngine.UI.Text>())
            {
                instructions|=text.text.Contains("joystick");
                if(text.text.Contains("F1")||text.text.Contains(" · M")||text.text.Contains("muis")||text.preferredHeight>text.rectTransform.rect.height+2)return false;
            }
            return instructions;
        }
        public static IEnumerator Run(FerrarisApp app,string output)
        {
            var checks=new List<string>();
            bool Check(bool passed,string name)
            {
                Debug.Log($"FERRARIS_XR_INPUT_CHECK {name}: {passed}");
                if(passed){checks.Add(name);return true;}
                File.WriteAllText(Path.Combine(output,"xr-input.json"),"{\"passed\":false,\"hardwareVerified\":false,\"failed\":\""+name+"\"}");
                Application.Quit(1);return false;
            }
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.RegisterLayout(@"{""name"":""FerrarisReplayController"",""extend"":""XRController"",""controls"":[
                {""name"":""primary2DAxis"",""layout"":""Vector2""},
                {""name"":""triggerPressed"",""layout"":""Axis""},
                {""name"":""secondaryButton"",""layout"":""Axis""},
                {""name"":""primaryButton"",""layout"":""Axis""}]}");
            var head=InputSystem.AddDevice("XRHMD");
            var left=InputSystem.AddDevice("FerrarisReplayController");InputSystem.SetDeviceUsage(left,"LeftHand");
            var right=InputSystem.AddDevice("FerrarisReplayController");InputSystem.SetDeviceUsage(right,"RightHand");
            Set(head,"centerEyePosition",Vector3.up*1.65f);Set(head,"centerEyeRotation",Quaternion.identity);
            var hand=new Vector3(.25f,1.2f,.1f);Set(right,"devicePosition",hand);Set(right,"deviceRotation",Quaternion.identity);
            // Only device-presence detection is bypassed; UpdateXR and its actions run unchanged.
            typeof(FerrarisApp).GetField("xr",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(app,true);
            app.ReturnToMap();yield return Frames(3);
            var ui=app.GetComponent<VisitorUI>();
            if(!Check(ControllerText(ui),"Map instructions describe controllers and fit"))yield break;
            if(!Check(app.IsXR&&!app.View.orthographic&&Vector3.Distance(app.View.transform.localPosition,Vector3.up*1.65f)<.001f,"Head pose binding and perspective map"))yield break;
            Set(right,"primary2DAxis",Vector2.up);yield return Frames(45);Set(right,"primary2DAxis",Vector2.zero);yield return Frames(2);
            if(!Check(app.MapZoom>1.3f,"Right joystick zoom"))yield break;
            Set(left,"primary2DAxis",new Vector2(.7f,.4f));yield return Frames(30);Set(left,"primary2DAxis",Vector2.zero);yield return Frames(2);
            if(!Check(app.MapPan.x>10&&app.MapPan.y>5,"Left joystick pan"))yield break;
            var expected=new Vector2(app.MapPan.x+app.Area.size*.1f/app.MapZoom,app.MapPan.y-app.Area.size*.1f/app.MapZoom);
            var target=app.MapSurface.TransformPoint(new Vector3(.1f,-.1f,0));
            Set(right,"deviceRotation",Quaternion.LookRotation(target-hand));yield return Frames(2);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"xr-map.png"));yield return Frames(2);
            Set(right,"triggerPressed",1f);yield return Frames(2);Set(right,"triggerPressed",0f);yield return Frames(2);
            if(!Check(app.InWorld&&Vector2.Distance(new Vector2(app.Selected.x,app.Selected.z),expected)<.02f,"Controller ray selects the zoomed and panned map coordinate"))yield break;
            if(!Check(Vector2.Distance(new Vector2(app.Player.position.x,app.Player.position.z),expected)<=25.1f,"Selected coordinate enters its historical world location"))yield break;
            Set(right,"secondaryButton",1f);yield return Frames(2);Set(right,"secondaryButton",0f);yield return Frames(2);
            if(!Check(!app.InWorld&&Vector2.Distance(new Vector2(app.Selected.x,app.Selected.z),expected)<.02f,"B returns to map and preserves selection"))yield break;
            // Keep the pointer below the visitor panels while replaying movement.
            Set(right,"deviceRotation",Quaternion.LookRotation(Vector3.down));
            float previous=0;
            foreach(float pressure in new[]{.25f,.5f,1f})
            {
                app.EnterWorld(-300,-100);yield return Frames(3);var start=app.Player.position;float time=Time.time;
                Set(left,"primary2DAxis",Vector2.up*pressure);yield return Frames(45);
                Set(left,"primary2DAxis",Vector2.zero);yield return Frames(1);
                var delta=app.Player.position-start;float speed=new Vector2(delta.x,delta.z).magnitude/(Time.time-time);
                Debug.Log($"FERRARIS_XR_SPEED pressure={pressure} metresPerSecond={speed:F3}");
                if(!Check(speed>previous&&Mathf.Abs(speed-app.VrMaxSpeed*Mathf.Pow(pressure,1.5f))<.2f,"Progressive controller speed "+pressure))yield break;
                previous=speed;
                if(!Check(app.Player.position.y>=app.Area.Height(app.Player.position.x,app.Player.position.z)-.1f,"Terrain support at joystick pressure "+pressure))yield break;
            }
            Set(head,"centerEyeRotation",Quaternion.Euler(0,90,0));yield return Frames(2);
            var before=app.Player.position;Set(left,"primary2DAxis",Vector2.up);yield return Frames(20);Set(left,"primary2DAxis",Vector2.zero);yield return Frames(1);
            var travel=app.Player.position-before;
            if(!Check(travel.x>.5f&&Mathf.Abs(travel.z)<.1f,"Walking follows headset yaw"))yield break;
            var eye=app.View.transform.position;var rotation=app.Player.rotation;
            Set(right,"primary2DAxis",Vector2.right);yield return Frames(2);Set(right,"primary2DAxis",Vector2.zero);yield return Frames(2);
            if(!Check(Mathf.Abs(Quaternion.Angle(rotation,app.Player.rotation)-30)<.1f&&Vector2.Distance(new Vector2(eye.x,eye.z),new Vector2(app.View.transform.position.x,app.View.transform.position.z))<.02f,"Snap turn rotates thirty degrees around the eye"))yield break;
            if(!Check(ControllerText(ui),"World instructions describe controllers and fit"))yield break;
            var mapButton=ui.Root.TransformPoint(new Vector3(130-720,500-111,0));
            Set(right,"deviceRotation",Quaternion.Inverse(app.Player.rotation)*Quaternion.LookRotation(mapButton-app.Player.TransformPoint(hand)));yield return Frames(2);
            var position=app.Player.position;
            Set(right,"triggerPressed",1f);yield return Frames(2);Set(right,"triggerPressed",0f);yield return Frames(2);
            if(!Check(app.GetComponent<ExplorationUI>().MapOpen&&app.InWorld&&Vector3.Distance(position,app.Player.position)<.02f,"Controller opens location map without moving the visitor"))yield break;
            Set(right,"secondaryButton",1f);yield return Frames(2);Set(right,"secondaryButton",0f);yield return Frames(2);
            if(!Check(!ui.PanelOpen&&app.InWorld,"B closes the map panel before leaving the world"))yield break;
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"xr-world.png"));yield return Frames(2);
            Set(right,"secondaryButton",1f);yield return Frames(2);
            if(!Check(!app.InWorld,"B returns after movement and snap turning"))yield break;
            var centre=app.View.WorldToViewportPoint(app.MapSurface.position);
            if(!Check(centre.z>0&&Vector2.Distance(new Vector2(centre.x,centre.y),Vector2.one*.5f)<.01f,"Returned map is centred after headset yaw and snap turn"))yield break;
            Set(right,"secondaryButton",0f);yield return Frames(2);
            var anchoredPosition=app.MapSurface.position;var anchoredRotation=app.MapSurface.rotation;
            var movedHead=new Vector3(.35f,1.1f,-.4f);var tiltedHead=Quaternion.Euler(20,-135,5);
            Set(head,"centerEyePosition",movedHead);Set(head,"centerEyeRotation",tiltedHead);yield return Frames(2);
            app.ZoomMap(1.1f);
            if(!Check(Vector3.Distance(app.MapSurface.position,anchoredPosition)<.001f&&Quaternion.Angle(app.MapSurface.rotation,anchoredRotation)<.01f,"Map stays anchored while looking around and zooming"))yield break;
            Set(right,"secondaryButton",1f);yield return Frames(1);
            centre=app.View.WorldToViewportPoint(app.MapSurface.position);
            if(!Check(centre.z>0&&Vector2.Distance(new Vector2(centre.x,centre.y),Vector2.one*.5f)<.01f&&Vector3.Distance(app.View.transform.localPosition,movedHead)<.001f&&Quaternion.Angle(app.View.transform.localRotation,tiltedHead)<.01f,"B recentres for seated translated tilted head without resetting tracked pose"))yield break;
            Set(right,"secondaryButton",0f);yield return Frames(2);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"xr-return-map.png"));yield return Frames(2);
            expected=new Vector2(app.MapPan.x,app.MapPan.y-app.Area.size*.1f/app.MapZoom);
            target=app.MapSurface.TransformPoint(new Vector3(0,-.1f,0));
            Set(right,"deviceRotation",Quaternion.LookRotation(target-hand));yield return Frames(2);
            Set(right,"triggerPressed",1f);yield return Frames(1);
            if(!Check(app.InWorld&&Vector2.Distance(new Vector2(app.Selected.x,app.Selected.z),expected)<.02f&&Quaternion.Angle(app.View.transform.localRotation,tiltedHead)<.01f,"Reoriented map ray preserves selected coordinates and first world-frame head pose"))yield break;
            Set(right,"triggerPressed",0f);yield return Frames(2);
            Set(right,"secondaryButton",1f);yield return Frames(2);Set(right,"secondaryButton",0f);yield return Frames(2);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UnityEngine.UI.Text diagnostic=null;
            foreach(var text in ui.Root.GetComponentsInChildren<UnityEngine.UI.Text>(true))if(text.name=="Ontwikkeldiagnostiek")diagnostic=text;
            Set(left,"secondaryButton",1f);yield return Frames(2);Set(left,"secondaryButton",0f);yield return new WaitForSeconds(1.2f);
            if(!Check(diagnostic!=null&&diagnostic.gameObject.activeSelf&&diagnostic.text.Contains("Y: sluiten")&&ControllerText(ui),"Y enables readable world-space diagnostics"))yield break;
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"xr-diagnostics.png"));yield return Frames(2);
            Set(left,"secondaryButton",1f);yield return Frames(2);Set(left,"secondaryButton",0f);yield return Frames(2);
            if(!Check(!diagnostic.gameObject.activeSelf&&!app.InWorld,"Y hides diagnostics without changing map mode"))yield break;
#endif
            foreach(var device in new[]{head,left,right})InputSystem.RemoveDevice(device);
            File.WriteAllText(Path.Combine(output,"xr-input.json"),"{\"passed\":true,\"hardwareVerified\":false,\"checks\":"+checks.Count+"}");
            Debug.Log("FERRARIS_XR_INPUT_PASS");Application.Quit(0);
        }
    }
}
#endif
