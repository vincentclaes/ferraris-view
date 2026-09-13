#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Ferraris
{
    // Application frame cadence, not compositor FPS or CPU/GPU execution time.
    public class DevelopmentDiagnostics : MonoBehaviour
    {
        FerrarisApp app;Text label;InputAction toggle;bool visible;int frames;double started;
        void Start()
        {
            app=GetComponent<FerrarisApp>();
            label=GetComponent<VisitorUI>().Text(new Rect(24,660,1050,220),"",23);
            label.name="Ontwikkeldiagnostiek";
            var outline=label.gameObject.AddComponent<Outline>();outline.effectColor=Color.black;outline.effectDistance=new Vector2(2,-2);
            toggle=new InputAction(type:InputActionType.Button,binding:"<XRController>{LeftHand}/secondaryButton");toggle.Enable();
            SetVisible(Array.Exists(Environment.GetCommandLineArgs(),s=>s=="-ferraris-diagnostics"));
        }
        void SetVisible(bool value)
        {
            visible=value;label.gameObject.SetActive(value);ResetSample();
        }
        void ResetSample(){frames=0;started=Time.realtimeSinceStartupAsDouble;if(label!=null)label.text="Ontwikkeldiagnostiek · eerste meting volgt…";}
        void OnApplicationFocus(bool focused){ResetSample();}
        void OnApplicationPause(bool paused){ResetSample();}
        void Update()
        {
            if(Keyboard.current?.f3Key.wasPressedThisFrame==true||app.IsXR&&toggle.WasPressedThisFrame())SetVisible(!visible);
            if(!visible||!app.Ready)return;
            frames++;double elapsed=Time.realtimeSinceStartupAsDouble-started;if(elapsed<1)return;
            double fps=frames/elapsed,ms=elapsed*1000/frames;
            var p=app.InWorld?app.View.transform.position:new Vector3(app.MapPan.x,0,app.MapPan.y);
            var geo=app.Area.UnityToGeoCoordinate(p.x,p.z);var selected=app.Area.UnityToGeoCoordinate(app.Selected.x,app.Selected.z);
            float terrain=app.Area.Height(p.x,p.z);string mode=app.InWorld?"wereld":"kaart";
            label.text=FormattableString.Invariant($"Ontwikkeldiagnostiek · {(app.IsXR?"Y":"F3")}: sluiten\nApp: {fps:F1} frames/s · {ms:F2} ms/frame (geen GPU-meting) · {mode}\n{(app.InWorld?"Oog":"Kaartmidden")}: {geo.lat:F6}, {geo.lon:F6} · X/Z: {p.x:F2} / {p.z:F2} m\nGekozen: {selected.lat:F6}, {selected.lon:F6} · X/Z: {app.Selected.x:F2} / {app.Selected.z:F2} m\nTerrein: {terrain:F2} m lokaal · {terrain+app.Area.heightBase:F2} m TAW");
            Debug.Log(FormattableString.Invariant($"FERRARIS_DIAGNOSTICS mode={mode} xr={app.IsXR} frames={frames} seconds={elapsed:F3} fps={fps:F2} meanFrameMs={ms:F3} x={p.x:F3} z={p.z:F3} lat={geo.lat:F7} lon={geo.lon:F7} selectedX={app.Selected.x:F3} selectedZ={app.Selected.z:F3} terrainLocal={terrain:F3} terrainTAW={terrain+app.Area.heightBase:F3}"));
            frames=0;started=Time.realtimeSinceStartupAsDouble;
        }
        void OnDestroy(){toggle?.Dispose();if(label!=null)Destroy(label.gameObject);}
    }
}
#endif
