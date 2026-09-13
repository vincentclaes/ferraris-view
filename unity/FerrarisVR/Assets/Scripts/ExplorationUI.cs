using UnityEngine;
using UnityEngine.UI;

namespace Ferraris
{
    // Browser-first navigation. Consulting the map never moves the player.
    public class ExplorationUI : MonoBehaviour
    {
        FerrarisApp app; VisitorUI ui;
        GameObject toolbar,mini,panel,zoomControls;
        Text instruction,bearing,address,mapStatus;
        RawImage miniImage,largeImage;
        RectTransform miniHeading,largeHeading;
        Vector2 lastSize; bool lastWorld;
        public bool MapOpen { get; private set; }

        void Start()
        {
            app=GetComponent<FerrarisApp>();ui=GetComponent<VisitorUI>();
            DrawNavigation();
        }
        void DrawNavigation()
        {
            ui.RemovePanel(toolbar);ui.RemovePanel(mini);ui.RemovePanel(zoomControls);
            lastSize=ui.Root.rect.size;lastWorld=app.InWorld;
            toolbar=ui.Box(new Rect(24,24,650,214));
            ui.Text(new Rect(42,35,610,34),"Winksele · circa 1775",26,toolbar.transform);
            if(app.InWorld)ui.Button(new Rect(40,88,186,46),"Kaart · M",ToggleMap,toolbar.transform);
            ui.Button(new Rect(app.InWorld?240:40,88,180,46),"Ontdek",ShowDiscover,toolbar.transform);
            ui.Button(new Rect(app.InWorld?434:234,88,164,46),"Hulp · F1",ShowHelp,toolbar.transform);
            instruction=ui.Text(new Rect(40,159,610,72),"",23);
            instruction.transform.SetParent(toolbar.transform,true);
            if(!app.InWorld)
            {
                zoomControls=ui.Box(new Rect(24,ui.Root.rect.height-88,276,64));
                ui.Button(new Rect(36,ui.Root.rect.height-78,54,44),"-",()=>app.ZoomMap(1/1.4f),zoomControls.transform,28);
                ui.Button(new Rect(102,ui.Root.rect.height-78,54,44),"+",()=>app.ZoomMap(1.4f),zoomControls.transform,28);
                ui.Button(new Rect(168,ui.Root.rect.height-78,118,44),"Overzicht",()=>{app.ZoomMap(1/app.MapZoom);},zoomControls.transform,20);
                return;
            }
            float x=ui.Root.rect.width-292;
            mini=ui.Box(new Rect(x,24,268,314));
            ui.Button(new Rect(x+8,32,252,298),"Vergroot kaart · M",ToggleMap,mini.transform,21);
            ui.Text(new Rect(x+20,292,235,32),"Vergroot kaart · M",21,mini.transform);
            miniImage=MapImage(new Rect(x+12,36,244,244),mini.transform,out miniHeading);
            ui.Text(new Rect(x+18,42,34,34),"N",23,mini.transform).color=new Color(.08f,.15f,.12f);
        }
        void LateUpdate()
        {
            if(ui==null||!app.Ready)return;
            if(lastWorld!=app.InWorld||lastSize!=ui.Root.rect.size)DrawNavigation();
            // Only the active panel participates in focus and click navigation.
            toolbar.SetActive(!ui.PanelOpen);
            if(zoomControls!=null)zoomControls.SetActive(!ui.PanelOpen);
            if(mini!=null)mini.SetActive(app.InWorld&&!ui.PanelOpen);
            instruction.text=app.InWorld
                ? Cursor.lockState==CursorLockMode.Locked?"WASD: wandelen   ·   Muis: kijken   ·   M: kaart":"Klik in het landschap om verder te wandelen.\nEscape maakt de muis vrij; je blijft op je plek."
                : app.MapStatus??"Klik een plek op de kaart om daar te wandelen.\nSleep om te verschuiven. Scrol om te zoomen.";
            UpdateMap(miniImage,miniHeading);
            if(MapOpen)
            {
                UpdateMap(largeImage,largeHeading);
                bearing.text="Je bent hier · je kijkt naar het "+Compass(app.View.transform.forward)+".";
                address.text=GetComponent<AddressDisplay>().Caption+"\nHuidig adres, geen bewezen historische perceelkoppeling.";
                mapStatus.text=app.MapStatus??"Noorden boven · De pijl toont je kijkrichting.";
            }
        }
        void UpdateMap(RawImage image,RectTransform heading)
        {
            if(image==null)return;
            image.texture=app.MapTexture;
            var uv=MapUV(app.Player.position,app.Area.size);
            heading.anchorMin=heading.anchorMax=uv;heading.anchoredPosition=Vector2.zero;
            heading.localRotation=Quaternion.Euler(0,0,-Mathf.Atan2(app.View.transform.forward.x,app.View.transform.forward.z)*Mathf.Rad2Deg);
        }
        public static Vector2 MapUV(Vector3 position,float size)=>new Vector2(Mathf.Clamp01(position.x/size+.5f),Mathf.Clamp01(position.z/size+.5f));
        public static string Compass(Vector3 forward)
        {
            string[] names={"noorden","noordoosten","oosten","zuidoosten","zuiden","zuidwesten","westen","noordwesten"};
            int index=Mathf.RoundToInt(Mathf.Atan2(forward.x,forward.z)*Mathf.Rad2Deg/45);
            return names[(index+8)%8];
        }
        RawImage MapImage(Rect rect,Transform parent,out RectTransform heading)
        {
            var go=new GameObject("Ferrariskaart",typeof(RectTransform),typeof(RawImage));
            var r=go.GetComponent<RectTransform>();r.SetParent(ui.Root,false);
            r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(rect.x,-rect.y);r.sizeDelta=rect.size;
            r.SetParent(parent,true);
            var image=go.GetComponent<RawImage>();image.texture=app.MapTexture;image.raycastTarget=false;
            heading=new GameObject("Je locatie en kijkrichting",typeof(RectTransform)).GetComponent<RectTransform>();
            heading.SetParent(go.transform,false);heading.sizeDelta=new Vector2(32,40);
            heading.gameObject.AddComponent<MapHeading>();
            return image;
        }
        void BeginPanel(string title)
        {
            ui.ClosePanel?.Invoke();
            if(GetComponent<ObjectDiscovery>().Active)GetComponent<ObjectDiscovery>().Close(true);
            ui.ClearKeyboardFocus();
            panel=ui.Box(new Rect(28,180,760,680));ui.PanelRoot=panel.transform;ui.PanelOpen=true;ui.ClosePanel=Close;
            ui.Text(new Rect(48,197,610,44),title,28,panel.transform);
            ui.Button(new Rect(670,197,98,42),"Sluiten",Close,panel.transform,21);
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }
        public void Close()
        {
            if(panel==null)return;
            ui.RemovePanel(panel);panel=null;MapOpen=false;
            ui.PanelOpen=false;ui.ClosePanel=null;ui.ClearKeyboardFocus();
            // Pointer lock needs a new click in browsers after Escape.
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }
        public void ToggleMap()
        {
            if(MapOpen){Close();return;}
            if(!app.InWorld){ui.ClosePanel?.Invoke();return;}
            BeginPanel("Je plek op de Ferrariskaart");MapOpen=true;
            largeImage=MapImage(new Rect(48,258,480,480),panel.transform,out largeHeading);
            ui.Text(new Rect(60,266,38,38),"N",26,panel.transform).color=new Color(.08f,.15f,.12f);
            bearing=ui.Text(new Rect(550,263,210,100),"",24,panel.transform);
            address=ui.Text(new Rect(550,384,210,268),"",20,panel.transform);
            mapStatus=ui.Text(new Rect(48,749,705,42),"",19,panel.transform);
            ui.Button(new Rect(48,800,284,44),"Verder wandelen · M",Close,panel.transform,22);
            ui.Button(new Rect(352,800,250,44),"Andere startplek",()=>{Close();app.ReturnToMap();},panel.transform,22);
        }
        public void ShowDiscover()
        {
            BeginPanel("Wat wil je ontdekken?");
            ui.Text(new Rect(48,259,700,65),"Kies iets dat je nieuwsgierig maakt. Je kunt ook gewoon blijven wandelen.",24,panel.transform);
            ui.Button(new Rect(48,353,710,62),"Onderzoek wat je ziet",()=>{Close();GetComponent<ObjectDiscovery>().Toggle();},panel.transform);
            ui.Button(new Rect(48,432,710,62),"Zoek een huidig adres",()=>{Close();GetComponent<HomeSearch>().Toggle();},panel.transform);
            ui.Button(new Rect(48,511,710,62),"Volg een dag in 1775",()=>{Close();GetComponent<PersonsDay>().Open();},panel.transform);
            ui.Button(new Rect(48,590,710,62),"Ontdek plaatsnamen",()=>{Close();GetComponent<PlaceNames>().Open();},panel.transform);
            ui.Button(new Rect(48,669,710,62),"Geluid en ondertitels",()=>{Close();GetComponent<LandscapeSound>().Open();},panel.transform);
            ui.Text(new Rect(48,773,700,65),"Bij elk verhaal lees je wat bekend is en wat een interpretatie of illustratie is.",21,panel.transform);
        }
        public void ShowHelp()
        {
            BeginPanel("Zo wandel je door Toenland");
            ui.Text(new Rect(48,270,700,480),app.IsXR
                ?"Bewegen: linker stick.\nDraaien: rechter stick.\nKiezen: rechter richtstraal en trekker.\n\nGebruik Kaart om je locatie en kijkrichting te bekijken.\nMet Sluiten blijf je op dezelfde plek.\n\nOnder Ontdek vind je adressen, verhalen, plaatsnamen en geluiden."
                :"Wandel met W A S D of de pijltjestoetsen.\nKijk rond met de muis. Shift laat je sneller gaan.\n\nM opent en sluit je kaart. De pijl toont waar je bent en waar je naar kijkt. Je wandeling blijft op dezelfde plek.\n\nEscape maakt de muis vrij of sluit een paneel.\nKlik in het landschap om weer rond te kijken.\n\nTab kiest een knop. Shift + Tab gaat terug.\nEnter of spatie bevestigt. F2 gaat naar de website.\n\nOnder Ontdek vind je adressen, verhalen en geluiden.",25,panel.transform);
            ui.Text(new Rect(48,769,704,70),"Kaart: KBR · Digitaal Vlaanderen, blad 93.\nHet landschap is deels een illustratieve reconstructie.",21,panel.transform);
        }
    }

    // A high-contrast arrow: its centre is the exact location, its tip faces ahead.
    public class MapHeading : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Add(vh,new Vector2(-16,-16),new Vector2(0,24),new Vector2(16,-16),Color.white);
            Add(vh,new Vector2(-11,-12),new Vector2(0,17),new Vector2(11,-12),new Color(.05f,.24f,.72f));
        }
        static void Add(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Color color)
        {
            int start=vh.currentVertCount;
            vh.AddVert(a,color,Vector2.zero);vh.AddVert(b,color,Vector2.zero);vh.AddVert(c,color,Vector2.zero);vh.AddTriangle(start,start+1,start+2);
        }
    }
}
