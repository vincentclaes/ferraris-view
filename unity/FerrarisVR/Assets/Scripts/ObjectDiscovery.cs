using System;
using System.Linq;
using UnityEngine;
namespace Ferraris
{
    public class ObjectDiscovery : MonoBehaviour
    {
        public bool Active { get; private set; }
        public ObjectContent Content { get; private set; }
        public ObjectPicker Picker { get; private set; }
        public PickedObject Selection { get; private set; }
        public int Page { get; private set; }
        public int Section { get; private set; }=-1;
        FerrarisApp app;VisitorUI ui;GameObject panel,hint;LineRenderer ring;LegendContent legend;int inventoryPage;
        public static string TypeLabel(string key)=>key switch
        {
            "house"=>"huis", "barn"=>"schuur", "farmhouse"=>"hoeve", "church"=>"kerk", "road"=>"weg", "soil"=>"kale akker", "crop"=>"akker met gewas", "grass"=>"grasland", "orchard"=>"boomgaard", "tree"=>"boom", "cow"=>"rund", "sheep"=>"schaap", "barrel"=>"ton", "woodpile"=>"houtstapel", "fence"=>"hek", "terrain"=>"terrein", "sky"=>"lucht", _=>"onbekend"
        };
        void Start()
        {
            app=GetComponent<FerrarisApp>();ui=GetComponent<VisitorUI>();
            Content=JsonUtility.FromJson<ObjectContent>(Resources.Load<TextAsset>("Discovery/objects").text);legend=JsonUtility.FromJson<LegendContent>(Resources.Load<TextAsset>("Discovery/legend").text);Picker=new ObjectPicker(app.Area,app.Data,app.World);
            ui.Button(new Rect(835,275,580,45),"Onderzoek objecten • I",Toggle);
            hint=ui.Box(new Rect(835,330,580,92));ui.Text(new Rect(852,340,545,76),"Onderzoeken: wijs een object of stuk grond aan en klik.\nQuest: rechter richtstraal + trekker. I: verder wandelen.",21,hint.transform);hint.SetActive(false);
            ring=new GameObject("Geselecteerd element").AddComponent<LineRenderer>();ring.loop=true;ring.positionCount=48;ring.material=new Material(Shader.Find("Unlit/Color")){color=new Color(1,.75f,.19f)};ring.enabled=false;
        }
        public void Toggle()
        {
            if(Active){Close(true);return;}ui.ClosePanel?.Invoke();Active=true;hint.SetActive(true);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }
        public void SelectMap(MapPoint point)=>Show(Picker.Map(point.x,point.z));
        public void SelectRay(Ray ray)=>Show(Picker.Ray(ray));
        public void Show(PickedObject selected)
        {
            ui.ClosePanel?.Invoke();Active=true;hint.SetActive(true);Selection=selected;Page=0;Section=-1;Draw();
        }
        public void Close(bool stop=false)
        {
            if(panel!=null)Destroy(panel);panel=null;ui.PanelOpen=false;ui.ClosePanel=null;
            if(stop){Active=false;hint.SetActive(false);ring.enabled=false;Cursor.lockState=app.InWorld&&!app.IsXR?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!app.InWorld||app.IsXR;}
        }
        void LateUpdate()
        {
            if(ring==null)return;ring.enabled=Active&&Selection!=null&&Selection.key!="sky";
            if(!ring.enabled)return;
            Vector3 centre=Selection.volume?.centre??Selection.point;float radius=Selection.radius;
            ring.useWorldSpace=app.InWorld;ring.startWidth=ring.endWidth=app.InWorld?.08f:.0018f;
            ring.transform.SetParent(app.InWorld?app.World.transform:app.MapSurface,false);
            for(int i=0;i<48;i++)
            {
                float a=i*Mathf.PI*2/48,x=centre.x+Mathf.Cos(a)*radius,z=centre.z+Mathf.Sin(a)*radius;
                ring.SetPosition(i,app.InWorld?new Vector3(x,app.Area.Height(x,z)+.22f,z):app.MapLocal(x,z,-.025f));
            }
        }
        void Draw()
        {
            if(panel!=null)Destroy(panel);panel=ui.Box(new Rect(28,180,760,680));ui.PanelOpen=true;ui.ClosePanel=()=>Close(true);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            var history=Content.For(Selection.key);
            ui.Text(new Rect(48,197,610,43),history?.name??"Onbekend element",28,panel.transform);ui.Button(new Rect(690,197,76,40),"Sluiten",()=>Close(true),panel.transform,19);
            if(history==null){ui.Text(new Rect(48,275,700,200),"Voor dit element is nog geen gecontroleerde uitleg beschikbaar. We voegen geen historische betekenis toe zonder onderbouwing.",25,panel.transform);return;}
            if(Page==0)
            {
                ui.Text(new Rect(48,265,700,35),"Kort uitgelegd",26,panel.transform);ui.Text(new Rect(48,315,706,175),history.summary,26,panel.transform);
                ui.Text(new Rect(48,520,706,120),"Legende: "+history.legend,23,panel.transform);
                ui.Button(new Rect(48,670,280,48),"Meer weten",()=>{Page=1;Section=-1;Draw();},panel.transform);
            }
            else if(Page==2)
            {
                ui.Text(new Rect(48,255,700,55),$"Volledige legende • pagina {inventoryPage+1}/{(legend.entries.Length+5)/6}",24,panel.transform);
                for(int i=0;i<6&&inventoryPage*6+i<legend.entries.Length;i++)
                {
                    var entry=legend.entries[inventoryPage*6+i];ui.Text(new Rect(48,321+i*66,704,62),entry.label+" — "+(entry.types.Length>0?string.Join(", ",entry.types.Select(TypeLabel))+": ":"")+entry.status,19,panel.transform);
                }
                if(inventoryPage>0)ui.Button(new Rect(48,735,180,42),"Vorige",()=>{inventoryPage--;Draw();},panel.transform);
                if((inventoryPage+1)*6<legend.entries.Length)ui.Button(new Rect(242,735,180,42),"Volgende",()=>{inventoryPage++;Draw();},panel.transform);
            }
            else if(Section<0)
            {
                ui.Text(new Rect(48,262,704,70),"Kies een onderdeel. Algemene uitleg is geen bewezen geschiedenis van dit individuele exemplaar.",24,panel.transform);
                for(int i=0;i<history.sections.Length;i++){int index=i;ui.Button(new Rect(48,355+i*45,714,40),history.sections[i].title,()=>{Section=index;Draw();},panel.transform,23);}
            }
            else
            {
                ui.Text(new Rect(48,265,704,68),history.sections[Section].title,26,panel.transform);ui.Text(new Rect(48,350,704,300),history.sections[Section].text,25,panel.transform);
                ui.Button(new Rect(48,682,270,43),"Andere onderdelen",()=>{Section=-1;Draw();},panel.transform,23);
                ui.Button(new Rect(337,682,270,43),"Volledige inventaris",()=>{Page=2;inventoryPage=0;Draw();},panel.transform,23);
            }
            ui.Button(new Rect(48,795,213,44),"Samenvatting",()=>{Page=0;Section=-1;Draw();},panel.transform,22);
            ui.Button(new Rect(275,795,245,44),"Kies ander object",()=>Close(),panel.transform,22);
            ui.Button(new Rect(534,795,229,44),"Plaatsnamen",()=>{Close(true);app.GetComponent<PlaceNames>().Open();},panel.transform,22);
        }
        void OnDestroy(){if(ring!=null)Destroy(ring.gameObject);}
    }
}
