using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
#if UNITY_WEBGL && !UNITY_EDITOR
using UnityEngine.Networking;
#endif

namespace Ferraris
{
    public class FerrarisApp : MonoBehaviour
    {
        public static FerrarisApp Instance { get; private set; }
        public AreaData Area { get; private set; }
        public HistoricalWorld World { get; private set; }
        public bool InWorld { get; private set; }
        public bool IsXR => xr;
        public WorldData Data => data;
        public Transform MapSurface => mapPlane.transform;
        public Vector3 MapLocal(float x,float z,float depth)=>new Vector3((x/Area.size-MapPan.x/Area.size*(xr?1:0))*(xr?MapZoom:1),(z/Area.size-MapPan.y/Area.size*(xr?1:0))*(xr?MapZoom:1),depth);
        public bool Ready { get; private set; }
        public Transform Player { get; private set; }
        public Camera View { get; private set; }
        public MapPoint Selected { get; private set; } = new(0,0);
        public float MapZoom { get; private set; } = 1;
        public Vector2 MapPan { get; private set; }
        public string Error { get; private set; }
        WorldData data;
        GameObject mapRoot,mapPlane,marker,overlay;
        CharacterController character;
        InputAction headPosition,headRotation,rightPosition,rightRotation,leftStick,rightStick,trigger,back,menu;
        LineRenderer rayLine;
        Vector2 pressPosition,previousPointer,pendingPan,releasedPosition;
        bool pointerReleased;
        Mouse pointerDevice;
        public bool TracePointer;
        public float VrMaxSpeed=6f;
        int ignoreLookUntilFrame;
        float yaw,pitch,verticalSpeed,turnCooldown,fps;
        bool pressed,dragged,triggerHeld,backHeld,menuHeld,xr,overlayShown;
        Material mapMaterial;
        string webMapStatus;
        GUIStyle title,body;
        readonly List<InputAction> actions=new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if(FindAnyObjectByType<FerrarisApp>()==null)new GameObject("Ferraris experience").AddComponent<FerrarisApp>();
        }
        void Awake()
        {
            Instance=this;
            try
            {
                var areaFile=Resources.Load<TextAsset>("Winksele/area");
                var worldFile=Resources.Load<TextAsset>("Winksele/world");
                if(areaFile==null || worldFile==null)throw new InvalidOperationException("Gebiedsgegevens ontbreken. Bereid de lokale kaartgegevens opnieuw voor.");
                Area=JsonUtility.FromJson<AreaData>(areaFile.text);data=JsonUtility.FromJson<WorldData>(worldFile.text);
                // Unity's mouse-event merging can move a button-down event to
                // the drag's final position, erasing the distinction from a click.
                InputSystem.settings.disableRedundantEventsMerging=true;
#if UNITY_WEBGL && !UNITY_EDITOR
                var texture=Texture2D.grayTexture;
#else
                var texture=Resources.Load<Texture2D>("Winksele/ferraris");
                if(texture==null)throw new InvalidOperationException("De Ferrariskaart ontbreekt. Bereid de lokale kaartgegevens opnieuw voor.");
#endif
                Application.targetFrameRate=72;QualitySettings.vSyncCount=0;QualitySettings.antiAliasing=4;
                RenderSettings.ambientLight=new Color(.7f,.72f,.62f);RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=160;RenderSettings.fogEndDistance=650;RenderSettings.fogColor=new Color(.71f,.77f,.75f);
                var sun=new GameObject("Late afternoon light").AddComponent<Light>();sun.type=LightType.Directional;sun.transform.rotation=Quaternion.Euler(40,-30,0);sun.shadows=LightShadows.None;
                Player=new GameObject("Player").transform;Player.SetParent(transform);
                character=Player.gameObject.AddComponent<CharacterController>();character.height=1.8f;character.radius=.25f;character.center=new Vector3(0,.9f,0);character.stepOffset=.35f;character.slopeLimit=55;
                View=new GameObject("Main Camera").AddComponent<Camera>();View.tag="MainCamera";View.transform.SetParent(Player);View.nearClipPlane=.05f;View.farClipPlane=1800;View.backgroundColor=RenderSettings.fogColor;View.clearFlags=CameraClearFlags.SolidColor;
                World=new GameObject("HistoricalWorld").AddComponent<HistoricalWorld>();World.transform.SetParent(transform);World.Build(Area,data);World.gameObject.SetActive(false);
                mapRoot=new GameObject("FerrarisMapScene");mapRoot.transform.SetParent(transform);
                mapPlane=GameObject.CreatePrimitive(PrimitiveType.Quad);mapPlane.name="Ferraris map — raycast surface";mapPlane.transform.SetParent(mapRoot.transform,false);
                mapMaterial=new Material(Shader.Find("Unlit/Texture")){mainTexture=texture};mapPlane.GetComponent<Renderer>().sharedMaterial=mapMaterial;
                marker=GameObject.CreatePrimitive(PrimitiveType.Sphere);Destroy(marker.GetComponent<SphereCollider>());marker.name="Selected location";marker.transform.SetParent(mapPlane.transform,false);marker.transform.localScale=Vector3.one*.014f;marker.GetComponent<Renderer>().material.color=new Color(.9f,.3f,.12f);
                overlay=new GameObject("Ferraris vector overlay");overlay.transform.SetParent(mapPlane.transform,false);BuildOverlay();overlay.SetActive(false);
                headPosition=Action("<XRHMD>/centerEyePosition");headRotation=Action("<XRHMD>/centerEyeRotation");
                rightPosition=Action("<XRController>{RightHand}/devicePosition");rightRotation=Action("<XRController>{RightHand}/deviceRotation");
                leftStick=Action("<XRController>{LeftHand}/primary2DAxis");rightStick=Action("<XRController>{RightHand}/primary2DAxis");
                trigger=Action("<XRController>{RightHand}/triggerPressed");back=Action("<XRController>{RightHand}/secondaryButton");
                menu=Action("<XRController>{LeftHand}/primaryButton");
                var pointerClick=new InputAction(type:InputActionType.Button,binding:"<Mouse>/leftButton");
                pointerClick.started+=c=>{pointerDevice=(Mouse)c.control.device;PointerDown(pointerDevice.position.ReadValue());};
                pointerClick.canceled+=c=>PointerUp(((Mouse)c.control.device).position.ReadValue());
                actions.Add(pointerClick);pointerClick.Enable();
                var pointerPosition=new InputAction(type:InputActionType.PassThrough,binding:"<Mouse>/position");
                pointerPosition.performed+=c=>{if(c.control.device==pointerDevice)PointerMove(c.ReadValue<Vector2>());};
                actions.Add(pointerPosition);pointerPosition.Enable();
                rayLine=new GameObject("Controller map ray").AddComponent<LineRenderer>();rayLine.positionCount=2;rayLine.startWidth=rayLine.endWidth=.004f;rayLine.material=new Material(Shader.Find("Unlit/Color"));rayLine.material.color=new Color(1,.72f,.24f);
                Ready=true;ReturnToMap();gameObject.AddComponent<AddressDisplay>();gameObject.AddComponent<HomeSearch>();gameObject.AddComponent<PersonsDay>();gameObject.AddComponent<LandscapeSound>();gameObject.AddComponent<PlaceNames>();gameObject.AddComponent<ObjectDiscovery>();StartCoroutine(DetectXR());
#if UNITY_WEBGL && !UNITY_EDITOR
                WebGLInput.captureAllKeyboardInput=false;
                StartCoroutine(LoadWebMap());
#endif
                if(Array.Exists(Environment.GetCommandLineArgs(),s=>s=="-ferraris-smoke"))gameObject.AddComponent<JourneySmoke>();
            }
            catch(Exception e){Error="De ervaring kon niet worden geladen. Controleer de lokale gebieds- en beeldbestanden.";Debug.LogException(e);}
        }
        InputAction Action(string binding){var a=new InputAction(binding:binding);a.Enable();actions.Add(a);return a;}
#if UNITY_WEBGL && !UNITY_EDITOR
        IEnumerator LoadWebMap()
        {
            webMapStatus="Ferrariskaart laden via Digitaal Vlaanderen…";
            var culture=System.Globalization.CultureInfo.InvariantCulture;
            string bbox=string.Join(",",Array.ConvertAll(new[]{Area.originE-Area.size/2,Area.originN-Area.size/2,Area.originE+Area.size/2,Area.originN+Area.size/2},value=>value.ToString("R",culture)));
            string url="https://geo.api.vlaanderen.be/histcart/wms?SERVICE=WMS&VERSION=1.1.1&REQUEST=GetMap&LAYERS=ferraris&STYLES=&SRS=EPSG:31370&BBOX="+bbox+"&WIDTH=2048&HEIGHT=2048&FORMAT=image/png";
            using(var request=UnityWebRequestTexture.GetTexture(url))
            {
                request.timeout=45;yield return request.SendWebRequest();
                if(request.result!=UnityWebRequest.Result.Success){webMapStatus="De kaartdienst is even niet bereikbaar. Herlaad de pagina om opnieuw te proberen.";yield break;}
                var texture=DownloadHandlerTexture.GetContent(request);texture.wrapMode=TextureWrapMode.Clamp;
                mapMaterial.mainTexture=texture;webMapStatus=null;Debug.Log("TOENLAND_MAP_READY");
            }
        }
#endif
        IEnumerator DetectXR()
        {
            // Android OpenXR loader may complete after Awake.
            for(int i=0;i<300;i++)
            {
                if(XRSettings.isDeviceActive)
                {
                    xr=true;var inputs=new List<XRInputSubsystem>();SubsystemManager.GetSubsystems(inputs);
                    foreach(var input in inputs)input.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
                    ReturnToMap();yield break;
                }
                yield return null;
            }
        }
        void Update()
        {
            if(!Ready)return;
            fps=Mathf.Lerp(fps,1/Mathf.Max(Time.unscaledDeltaTime,.001f),.04f);
            if(xr){UpdateXR();return;}
            var mouse=Mouse.current;var keyboard=Keyboard.current;
            var ui=GetComponent<VisitorUI>();
            if(keyboard!=null && keyboard.escapeKey.wasPressedThisFrame){if(ui?.PanelOpen==true)ui.ClosePanel?.Invoke();else ReturnToMap();return;}
            if(ui?.HandleKeyboard(keyboard)==true)return;
            // Text entry owns letter keys: typing Winksele must not open inspection.
            if(GetComponent<HomeSearch>()?.IsOpen!=true&&keyboard!=null)
            {
                if(keyboard.hKey.wasPressedThisFrame){GetComponent<HomeSearch>().Toggle();return;}
                if(keyboard.jKey.wasPressedThisFrame){GetComponent<PersonsDay>().Open();return;}
                if(keyboard.lKey.wasPressedThisFrame){GetComponent<LandscapeSound>().Open();return;}
                if(keyboard.nKey.wasPressedThisFrame){GetComponent<PlaceNames>().Open();return;}
                if(keyboard.iKey.wasPressedThisFrame){ui?.ClearKeyboardFocus();GetComponent<ObjectDiscovery>().Toggle();return;}
                if(keyboard.mKey.wasPressedThisFrame){ui?.ClosePanel?.Invoke();ReturnToMap();return;}
            }
            if(ui?.PanelOpen==true||ui?.HasKeyboardFocus==true)return;
            if(InWorld)
            {
                if(mouse!=null && Cursor.lockState==CursorLockMode.Locked && Time.frameCount>ignoreLookUntilFrame)
                {
                    Vector2 delta=mouse.delta.ReadValue();yaw+=delta.x*.12f;pitch=Mathf.Clamp(pitch-delta.y*.12f,-85,85);
                    Player.rotation=Quaternion.Euler(0,yaw,0);View.transform.localRotation=Quaternion.Euler(pitch,0,0);
                }
                Vector2 move=Vector2.zero;
                if(keyboard!=null){move.x=(keyboard.dKey.isPressed?1:0)-(keyboard.aKey.isPressed?1:0);move.y=(keyboard.wKey.isPressed?1:0)-(keyboard.sKey.isPressed?1:0);}
                Move(move,keyboard!=null && keyboard.leftShiftKey.isPressed?6:2.6f,Time.deltaTime);
            }
            else if(mouse!=null)
            {
                if(keyboard!=null)
                {
                    Vector2 pan=new((keyboard.rightArrowKey.isPressed?1:0)-(keyboard.leftArrowKey.isPressed?1:0),(keyboard.upArrowKey.isPressed?1:0)-(keyboard.downArrowKey.isPressed?1:0));
                    if(pan!=Vector2.zero)PanMap(pan.normalized*Area.size*.3f*Time.deltaTime/MapZoom);
                    if(keyboard.equalsKey.wasPressedThisFrame||keyboard.numpadPlusKey.wasPressedThisFrame)ZoomMap(1.4f);
                    if(keyboard.minusKey.wasPressedThisFrame||keyboard.numpadMinusKey.wasPressedThisFrame)ZoomMap(1/1.4f);
                    if(keyboard.rKey.wasPressedThisFrame){MapZoom=1;MapPan=Vector2.zero;ApplyMapView();}
                    if(keyboard.vKey.wasPressedThisFrame){overlayShown=!overlayShown;overlay.SetActive(overlayShown);}
                    if(keyboard.enterKey.wasPressedThisFrame||keyboard.numpadEnterKey.wasPressedThisFrame){SelectUV(MapPan.x/Area.size+.5f,MapPan.y/Area.size+.5f);return;}
                }
                Vector2 pos=mouse.position.ReadValue();
                if(pos.y>110 && pos.y<Screen.height-120)
                {
                    float scroll=mouse.scroll.ReadValue().y;
                    if(Mathf.Abs(scroll)>0)ZoomMap(Mathf.Exp(Mathf.Clamp(scroll,-120,120)*.003f));
                }
                if(dragged && pendingPan!=Vector2.zero)
                {
                    PanMap(-pendingPan*View.orthographicSize*2/Screen.height);pendingPan=Vector2.zero;
                }
                if(pointerReleased)
                {
                    pointerReleased=false;
                    if(!dragged && Physics.Raycast(View.ScreenPointToRay(releasedPosition),out RaycastHit hit,3000) && hit.collider.gameObject==mapPlane)SelectUV(hit.textureCoord.x,hit.textureCoord.y);
                }
            }
        }
        // Input callbacks preserve down/move/up positions even when a quick drag
        // is delivered entirely in one Update. Frame polling loses that history.
        void PointerDown(Vector2 p)
        {
            if(TracePointer)Debug.Log($"POINTER down {p}");
            if(!Ready||xr)return;
            var ui=GetComponent<VisitorUI>();
            if(ui?.ClickScreen(p)==true||ui?.PanelOpen==true)return;
            if(HandleToolbar(p)){ui?.ClearKeyboardFocus();return;}
            if(InWorld&&GetComponent<ObjectDiscovery>()?.Active==true){GetComponent<ObjectDiscovery>().SelectRay(View.ScreenPointToRay(p));return;}
            if(InWorld)
            {
                GetComponent<VisitorUI>()?.ClearKeyboardFocus();Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;ignoreLookUntilFrame=Time.frameCount+1;return;
            }
            if(p.y<=110||p.y>=Screen.height-120)return;
            GetComponent<VisitorUI>()?.ClearKeyboardFocus();
            pressed=true;dragged=false;pointerReleased=false;pendingPan=Vector2.zero;pressPosition=previousPointer=p;
        }
        void PointerMove(Vector2 p)
        {
            if(TracePointer)Debug.Log($"POINTER move {p} pressed={pressed}");
            if(!pressed)return;
            pendingPan+=p-previousPointer;previousPointer=p;
            if(Vector2.Distance(p,pressPosition)>5)dragged=true;
        }
        void PointerUp(Vector2 p)
        {
            if(TracePointer)Debug.Log($"POINTER up {p} pressed={pressed} dragged={dragged}");
            if(!pressed)return;
            PointerMove(p);pressed=false;releasedPosition=p;pointerReleased=true;
        }
        static Rect ToolbarRect(float x,float width)=>new Rect(x,Screen.height-57,width,32);
        bool HandleToolbar(Vector2 pointer)
        {
            pointer.y=Screen.height-pointer.y;
            if(InWorld)
            {
                if(ToolbarRect(28,155).Contains(pointer)){ReturnToMap();return true;}
                return false;
            }
            if(ToolbarRect(28,110).Contains(pointer))ZoomMap(1/1.4f);
            else if(ToolbarRect(148,110).Contains(pointer))ZoomMap(1.4f);
            else if(ToolbarRect(268,140).Contains(pointer)){MapPan=Vector2.zero;MapZoom=1;ApplyMapView();}
            else if(ToolbarRect(418,165).Contains(pointer)){overlayShown=!overlayShown;overlay.SetActive(overlayShown);}
            else return false;
            return true;
        }
        void UpdateXR()
        {
            View.transform.localPosition=headPosition.ReadValue<Vector3>();View.transform.localRotation=headRotation.ReadValue<Quaternion>();
            bool down=trigger.ReadValue<float>()>.5f,b=back.ReadValue<float>()>.5f;
            var ui=GetComponent<VisitorUI>();
            if(b&&!backHeld){if(ui?.PanelOpen==true)ui.ClosePanel?.Invoke();else ReturnToMap();}backHeld=b;
            bool m=menu.ReadValue<float>()>.5f;if(m&&!menuHeld)GetComponent<HomeSearch>().Toggle();menuHeld=m;
            Vector3 uiOrigin=Player.TransformPoint(rightPosition.ReadValue<Vector3>());
            Vector3 uiDirection=Player.rotation*rightRotation.ReadValue<Quaternion>()*Vector3.forward;
            bool uiHit=ui!=null&&ui.ClickRay(new Ray(uiOrigin,uiDirection),down&&!triggerHeld,out _);
            var discovery=GetComponent<ObjectDiscovery>();
            if(!uiHit&&InWorld&&discovery?.Active==true&&down&&!triggerHeld)discovery.SelectRay(new Ray(uiOrigin,uiDirection));
            if(uiHit||ui?.PanelOpen==true){rayLine.enabled=true;rayLine.SetPosition(0,uiOrigin);rayLine.SetPosition(1,uiOrigin+uiDirection*2);triggerHeld=down;return;}
            Vector2 right=rightStick.ReadValue<Vector2>(),left=leftStick.ReadValue<Vector2>();
            if(InWorld)
            {
                turnCooldown-=Time.deltaTime;
                if(Mathf.Abs(right.x)>.7f && turnCooldown<=0){Player.RotateAround(View.transform.position,Vector3.up,Mathf.Sign(right.x)*30);turnCooldown=.3f;}
                Move(LocomotionInput(left),VrMaxSpeed,Time.deltaTime);
            }
            else
            {
                if(left.sqrMagnitude>.04f)PanMap(left*Area.size*.25f*Time.deltaTime/MapZoom);
                if(Mathf.Abs(right.y)>.15f)ZoomMap(Mathf.Exp(right.y*Time.deltaTime));
                Vector3 origin=Player.TransformPoint(rightPosition.ReadValue<Vector3>());
                Vector3 direction=Player.rotation*rightRotation.ReadValue<Quaternion>()*Vector3.forward;
                var ray=new Ray(origin,direction);Vector3 end=origin+direction*4;
                if(Physics.Raycast(ray,out RaycastHit hit,10)&&hit.collider.gameObject==mapPlane)
                {
                    end=hit.point;
                    if(down&&!triggerHeld)SelectUV(hit.textureCoord.x,hit.textureCoord.y);
                }
                rayLine.SetPosition(0,origin);rayLine.SetPosition(1,end);
            }
            rayLine.enabled=!InWorld||discovery?.Active==true;if(InWorld){rayLine.SetPosition(0,uiOrigin);rayLine.SetPosition(1,uiOrigin+uiDirection*12);}triggerHeld=down;
        }
        public void ZoomMap(float factor){MapZoom=Mathf.Clamp(MapZoom*factor,1,8);ApplyMapView();}
        public void PanMap(Vector2 delta){MapPan+=delta;float limit=Area.size*.5f*(1-1/MapZoom);MapPan=new Vector2(Mathf.Clamp(MapPan.x,-limit,limit),Mathf.Clamp(MapPan.y,-limit,limit));ApplyMapView();}
        void ApplyMapView()
        {
            float limit=Area.size*.5f*(1-1/MapZoom);MapPan=new Vector2(Mathf.Clamp(MapPan.x,-limit,limit),Mathf.Clamp(MapPan.y,-limit,limit));
            if(xr)
            {
                mapPlane.transform.localScale=Vector3.one*2.4f;
                mapPlane.transform.localPosition=new Vector3(0,1.6f,2.4f);
                mapMaterial.mainTextureScale=Vector2.one/MapZoom;mapMaterial.mainTextureOffset=MapPan/Area.size+Vector2.one*(.5f-.5f/MapZoom);
                // UV overlays need the same affine mapping as the zoomed raster.
                overlay.transform.localScale=Vector3.one*MapZoom;overlay.transform.localPosition=new Vector3(-MapPan.x/Area.size*MapZoom,-MapPan.y/Area.size*MapZoom,0);
            }
            else
            {
                mapPlane.transform.localScale=new Vector3(Area.size,Area.size,1);
                mapPlane.transform.localPosition=Vector3.zero;mapMaterial.mainTextureScale=Vector2.one;mapMaterial.mainTextureOffset=Vector2.zero;
                View.orthographicSize=Area.size*.66f/MapZoom/Mathf.Min(1,View.aspect);
                Player.position=new Vector3(MapPan.x,MapPan.y,-1100);
            }
            marker.transform.localPosition=new Vector3((Selected.x/Area.size-MapPan.x/Area.size*(xr?1:0))*(xr?MapZoom:1),(Selected.z/Area.size-MapPan.y/Area.size*(xr?1:0))*(xr?MapZoom:1),-.02f);
        }
        public void Locate(float x,float z){Selected=new MapPoint(x,z);ReturnToMap();MapZoom=2;MapPan=new Vector2(x,z);ApplyMapView();}
        public void SelectUV(float u,float v)
        {
            if(xr){u=(u-.5f)/MapZoom+.5f+MapPan.x/Area.size;v=(v-.5f)/MapZoom+.5f+MapPan.y/Area.size;}
            Selected=Area.MapUVToUnity(Mathf.Clamp01(u),Mathf.Clamp01(v));
            if(GetComponent<ObjectDiscovery>()?.Active==true){GetComponent<ObjectDiscovery>().SelectMap(Selected);return;}
            EnterWorld(Selected.x,Selected.z);
        }
        public void EnterWorld(float x,float z)
        {
            ignoreLookUntilFrame=Time.frameCount+1;
            InWorld=true;View.clearFlags=CameraClearFlags.Skybox;mapRoot.SetActive(false);World.gameObject.SetActive(true);rayLine.enabled=false;
            character.enabled=false;Player.SetPositionAndRotation(new Vector3(x,Area.Height(x,z)+.08f,z),Quaternion.identity);
            yaw=pitch=verticalSpeed=0;View.orthographic=false;if(!xr)View.fieldOfView=75;
            View.transform.localPosition=xr?headPosition.ReadValue<Vector3>():Vector3.up*1.65f;View.transform.localRotation=Quaternion.identity;
            Physics.SyncTransforms();character.enabled=true;
            // Map symbols can be clicked inside buildings. Find the nearest free
            // spawn within 25m; keep Selected unchanged for alignment diagnostics.
            if(Occupied(Player.position))
            {
                bool found=false;
                for(float r=1;r<=25&&!found;r+=1)for(int i=0;i<16&&!found;i++)
                {
                    Vector3 p=new(x+Mathf.Cos(i*Mathf.PI/8)*r,0,z+Mathf.Sin(i*Mathf.PI/8)*r);p.y=Area.Height(p.x,p.z)+.08f;
                    if(Mathf.Abs(p.x)<Area.size/2-1&&Mathf.Abs(p.z)<Area.size/2-1&&!Occupied(p)){character.enabled=false;Player.position=p;character.enabled=true;found=true;}
                }
            }
            Cursor.lockState=xr?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=xr;
        }

        bool Occupied(Vector3 p)
        {
            foreach(var building in data.buildings)if(building.Contains(p.x,p.z))return true;
            foreach(Collider c in Physics.OverlapCapsule(p+Vector3.up*.4f,p+Vector3.up*1.5f,.3f))if(c!=character&&c.gameObject!=World.TerrainObject)return true;
            return false;
        }
        // Preserve stick magnitude: a gentle push creeps, full tilt runs.
        public static Vector2 LocomotionInput(Vector2 stick)
        {
            stick=Vector2.ClampMagnitude(stick,1);
            return stick*Mathf.Sqrt(stick.magnitude);
        }
        public void Move(Vector2 input,float speed,float dt)
        {
            if(!InWorld)return;
            Vector3 forward=View.transform.forward;forward.y=0;forward.Normalize();Vector3 right=Vector3.Cross(Vector3.up,forward);
            input=Vector2.ClampMagnitude(input,1);Vector3 movement=(right*input.x+forward*input.y)*speed;
            if(character.isGrounded&&verticalSpeed<0)verticalSpeed=-2;verticalSpeed=Mathf.Max(-30,verticalSpeed-20*dt);
            character.Move((movement+Vector3.up*verticalSpeed)*dt);
            Vector3 p=Player.position;float edge=Area.size*.5f-.7f;p.x=Mathf.Clamp(p.x,-edge,edge);p.z=Mathf.Clamp(p.z,-edge,edge);p.y=Mathf.Max(p.y,Area.Height(p.x,p.z)+.03f);
            Player.position=p;
        }
        public void ReturnToMap()
        {
            if(!Ready)return;
            GetComponent<VisitorUI>()?.ClearKeyboardFocus();
            InWorld=false;View.clearFlags=CameraClearFlags.SolidColor;character.enabled=false;World.gameObject.SetActive(false);mapRoot.SetActive(true);
            pressed=pointerReleased=false;pendingPan=Vector2.zero;
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            Player.SetPositionAndRotation(Vector3.zero,Quaternion.identity);View.transform.SetLocalPositionAndRotation(Vector3.zero,Quaternion.identity);View.orthographic=!xr;
            rayLine.enabled=xr;ApplyMapView();
        }
        void BuildOverlay()
        {
            foreach(Road road in data.roads)Line(road.points,new Color(0,.8f,1),false);
            foreach(Building b in data.buildings)
            {
                if(b.footprint!=null){Line(b.footprint,Color.yellow,true);continue;}
                Quaternion q=Quaternion.Euler(0,b.yaw,0);var points=new List<MapPoint>();
                foreach(Vector3 p in new[]{new Vector3(-b.width/2,0,-b.depth/2),new Vector3(b.width/2,0,-b.depth/2),new Vector3(b.width/2,0,b.depth/2),new Vector3(-b.width/2,0,b.depth/2)}){Vector3 v=q*p;points.Add(new MapPoint(b.x+v.x,b.z+v.z));}Line(points.ToArray(),Color.yellow,true);
            }
            foreach(Patch patch in data.patches)if(patch.kind=="orchard")for(int i=0;i<patch.points.Length;i+=3)Line(new[]{patch.points[i],patch.points[i+1],patch.points[i+2]},Color.green,true);
        }
        void Line(MapPoint[] points,Color color,bool loop)
        {
            var line=new GameObject("Extracted feature").AddComponent<LineRenderer>();line.transform.SetParent(overlay.transform,false);line.useWorldSpace=false;line.loop=loop;line.positionCount=points.Length;line.startWidth=line.endWidth=xr?.0036f:Area.size*.0015f;line.material=new Material(Shader.Find("Unlit/Color"));line.material.color=color;
            for(int i=0;i<points.Length;i++)line.SetPosition(i,new Vector3(points[i].x/Area.size,points[i].z/Area.size,-.005f));
        }
        void OnGUI()
        {
            if(xr)return;
            title??=new GUIStyle(GUI.skin.label){fontSize=26,fontStyle=FontStyle.Bold};body??=new GUIStyle(GUI.skin.label){fontSize=15};
            GUI.Box(new Rect(12,12,InWorld?Screen.width*.56f-12:Screen.width-24,98),GUIContent.none);
            GUI.Label(new Rect(28,20,Screen.width-40,38),"WINKSELE  /  1775",title);
            if(!Ready){GUI.Label(new Rect(28,64,Screen.width-50,80),Error??"Ferrariskaart laden…",body);return;}
            GeoPoint geo=Area.UnityToGeoCoordinate(InWorld?Player.position.x:Selected.x,InWorld?Player.position.z:Selected.z);
            GUI.Label(new Rect(28,61,Screen.width-50,25),$"{(InWorld?"HISTORISCH LANDSCHAP":"FERRARISKAART")}   •   {geo.lat:F6}, {geo.lon:F6}   •   {(InWorld?$"X {Player.position.x:F1}  Z {Player.position.z:F1}  terrein {Area.Height(Player.position.x,Player.position.z)+Area.heightBase:F1}m TAW":$"Zoom {MapZoom:F1}×")}   •   {fps:F0} FPS",body);
            if(!InWorld&&webMapStatus!=null)GUI.Label(new Rect(28,Screen.height/2,Screen.width-56,60),webMapStatus,body);
            GUI.Box(new Rect(12,Screen.height-100,Screen.width-24,88),GUIContent.none);
            GUI.Label(new Rect(28,Screen.height-92,Screen.width-50,25),InWorld?"WASD lopen · Muis kijken · Shift sneller · Tab: muis vrij / knoppen · H adres · J verhaal · L geluid · N namen · I onderzoek · M kaart":"Sleep / pijltjes: verschuiven · Scrol / + −: zoom · Klik / Enter: betreed plek · Tab: knoppen · H adres · J verhaal · L geluid · N namen",body);
            if(InWorld)
            {
                GUI.Box(ToolbarRect(28,155),"Terug naar kaart",GUI.skin.button);
            }
            else
            {
                GUI.Box(ToolbarRect(28,110),"−  Zoom",GUI.skin.button);
                GUI.Box(ToolbarRect(148,110),"+  Zoom",GUI.skin.button);
                GUI.Box(ToolbarRect(268,140),"Herstel kaart",GUI.skin.button);
                GUI.Box(ToolbarRect(418,165),overlayShown?"Verberg lijnen":"Toon lijnen",GUI.skin.button);
            }
            GUI.Label(new Rect(Screen.width-370,Screen.height-52,345, 32),"KBR · Digitaal Vlaanderen | blad 93",body);
        }

        void OnDestroy(){foreach(var a in actions)a.Dispose();if(Instance==this)Instance=null;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
    }
}
