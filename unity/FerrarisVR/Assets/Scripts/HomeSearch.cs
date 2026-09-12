using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Ferraris
{
    public class HomeSearch : MonoBehaviour
    {
        public CurrentAddress Selected { get; private set; }
        public string Query { get; private set; }="";
        VisitorUI ui;FerrarisApp app;AddressBook book;GameObject panel;bool evidence;
        Keyboard keyboard;
        public bool IsOpen=>panel!=null&&panel.activeSelf;
        public bool AcceptsText=>IsOpen&&Selected==null&&!ui.HasKeyboardFocus;
        public static CurrentAddress[] Search(AddressBook book,string query)
        {
            var tokens=(query??"").Trim().Split(' ',StringSplitOptions.RemoveEmptyEntries);
            if(tokens.Length==0||book?.addresses==null)return Array.Empty<CurrentAddress>();
            return book.addresses.Where(a=>tokens.All(t=>a.Label.IndexOf(t,StringComparison.OrdinalIgnoreCase)>=0)).ToArray();
        }
        public static bool Covered(CurrentAddress a,float size)=>a!=null&&Mathf.Abs(a.x)<=size/2-.7f&&Mathf.Abs(a.z)<=size/2-.7f;
        public static string LandUse(WorldData data,float x,float z)
        {
            foreach(var b in data.buildings)
            {
                Vector3 p=Quaternion.Euler(0,-b.yaw,0)*new Vector3(x-b.x,0,z-b.z);
                if(Mathf.Abs(p.x)<=b.width/2&&Mathf.Abs(p.z)<=b.depth/2)return b.kind=="church"?"kerk":"bebouwing (functie niet zeker)";
            }
            foreach(var road in data.roads)
                for(int i=1;i<road.points.Length;i++)if(SegmentDistance(new Vector2(x,z),road.points[i-1],road.points[i])<=road.width/2)return "weg";
            foreach(var p in data.patches)if(WorldVegetation.Contains(p,x,z))return p.kind switch{"orchard"=>"boomgaard","grass"=>"grasland","soil"=>"akker","crop"=>"akker",_=>"onbekend / niet geclassificeerd"};
            return "onbekend / niet geclassificeerd";
        }
        public static float SegmentDistance(Vector2 p,MapPoint a,MapPoint b)
        {
            Vector2 start=new(a.x,a.z),v=new(b.x-a.x,b.z-a.z);
            return Vector2.Distance(p,start+v*Mathf.Clamp01(Vector2.Dot(p-start,v)/Mathf.Max(v.sqrMagnitude,.0001f)));
        }
        void Start()
        {
            app=GetComponent<FerrarisApp>();ui=GetComponent<VisitorUI>();book=GetComponent<AddressDisplay>().Book;
            ui.Button(new Rect(28,125,290,45),"Waar stond mijn huis?",Toggle);
            keyboard=Keyboard.current;if(keyboard!=null)keyboard.onTextInput+=Typed;
        }
        void OnDestroy(){if(keyboard!=null)keyboard.onTextInput-=Typed;}
        void Typed(char c){if(!AcceptsText||char.IsControl(c))return;SetQuery(Query+c);}
        void Update()
        {
            if(keyboard!=Keyboard.current){if(keyboard!=null)keyboard.onTextInput-=Typed;keyboard=Keyboard.current;if(keyboard!=null)keyboard.onTextInput+=Typed;}
            if(AcceptsText&&keyboard?.backspaceKey.wasPressedThisFrame==true)SetQuery(Query.Length>0?Query[..^1]:"");
        }
        public void Toggle(){if(IsOpen){Close();return;}ui.ClosePanel?.Invoke();ui.ClearKeyboardFocus();Draw();}
        public void Close(){ui.RemovePanel(panel);panel=null;ui.ClosePanel=null;ui.PanelOpen=false;ui.ClearKeyboardFocus();}
        public void SetQuery(string query){Query=query.Length>80?query[..80]:query;Selected=null;evidence=false;Draw();}
        public void Choose(CurrentAddress address)
        {
            Selected=address;evidence=false;
            if(Covered(address,app.Area.size)){app.Locate(address.x,address.z);}
            Draw();
        }
        void Draw()
        {
            ui.RemovePanel(panel);panel=ui.Box(new Rect(28,180,760,680));ui.PanelOpen=true;ui.ClosePanel=Close;
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            ui.Text(new Rect(48,195,675,35),"Wat stond waar mijn huis nu staat?",27,panel.transform);
            ui.Button(new Rect(690,195,76,40),"Sluiten",Close,panel.transform,19);
            ui.Button(new Rect(48,240,705,36),"Adres: "+Query+" ▏",()=>{Selected=null;ui.ClearKeyboardFocus();Draw();},panel.transform,25);
            if(Selected==null)
            {
                var matches=Search(book,Query);
                ui.Text(new Rect(48,282,710,62),Query.Length<2?"Typ een straat en huisnummer, of gebruik de toetsen. Offline zoeken in Winksele en de directe omgeving.":matches.Length==0?"Geen lokaal adres gevonden. Het adres is onbekend in deze momentopname of ligt buiten de beschikbare omgeving.":$"{matches.Length} resultaten. Kies je adres; typ verder om te verfijnen.",21,panel.transform);
                for(int i=0;i<Mathf.Min(matches.Length,5);i++){var a=matches[i];ui.Button(new Rect(48,353+i*46,718,42),a.Label,()=>Choose(a),panel.transform,21);}
            }
            else
            {
                bool covered=Covered(Selected,app.Area.size);
                string text=Selected.Label+"\n\n"+(covered?"Kaartinterpretatie rond 1775: "+LandUse(app.Data,Selected.x,Selected.z)+".\nDe marker bewaart je exacte gekozen punt. De historische kaart ligt niet overal precies op de huidige situatie.":"Dit adres ligt buiten het beschikbare kaart- en wandelgebied van 1 × 1 km rond Winksele. Je kunt hier nog niet de wereld binnengaan.");
                if(evidence)text="Hoe weten we dit?\nHuidig adrespunt: Digitaal Vlaanderen, "+book.date+".\nHistorische interpretatie: handmatig nagekeken overtrek van de Ferrariskaart, KBR/NGI blad 93 (1771–1778), https://uurl.kbr.be/1028485.\nBebouwing, wegen en percelen zijn kaartinterpretaties. Gewassen, gebouwen en dieren in 3D zijn illustratief. Niet ingetekende plekken blijven ongeclassificeerd; een huisfunctie is niet bewezen.";
                ui.Text(new Rect(48,287,704,230),text,22,panel.transform);
                ui.Button(new Rect(48,525,235,44),"Hoe weten we dit?",()=>{evidence=!evidence;Draw();},panel.transform);
                if(covered)ui.Button(new Rect(300,525,260,44),"Ga naar deze plek",()=>{app.EnterWorld(Selected.x,Selected.z);Close();},panel.transform);
                ui.Button(new Rect(580,525,180,44),"Verder zoeken",()=>{Selected=null;Draw();},panel.transform,21);
            }
            string[] rows={"QWERTYUIOP","ASDFGHJKL","ZXCVBNM","1234567890"};
            for(int y=0;y<rows.Length;y++)for(int x=0;x<rows[y].Length;x++){string c=rows[y][x].ToString();ui.Button(new Rect(48+x*54,590+y*48,49,43),c,()=>SetQuery(Query+c),panel.transform);}
            ui.Button(new Rect(603,590,158,43),"Wis letter",()=>SetQuery(Query.Length>0?Query[..^1]:""),panel.transform,20);
            ui.Button(new Rect(603,638,158,43),"Spatie",()=>SetQuery(Query+" "),panel.transform,20);
            ui.Button(new Rect(603,686,158,43),"Wis alles",()=>SetQuery(""),panel.transform,20);
            ui.Text(new Rect(48,793,700,55),app.IsXR?"Linker X: menu • Rechter richtstraal + trekker: kies\nOffline adresgegevens: Digitaal Vlaanderen":"Typ je adres • Tab: kies knop • Enter: bevestig • Escape: sluiten\nKlik op het adresveld om verder te typen. Bron: Digitaal Vlaanderen",19,panel.transform);
        }
    }
}
