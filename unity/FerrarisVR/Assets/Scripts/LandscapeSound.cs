using System.Collections.Generic;
using UnityEngine;
namespace Ferraris
{
    public class SoundSite
    {
        public string name,description;public AudioSource source;public float range,next;
    }
    public class LandscapeSound : MonoBehaviour
    {
        public readonly List<SoundSite> Sites=new();
        public float Volume { get; private set; }=.6f;
        public bool Muted { get; private set; }
        FerrarisApp app;VisitorUI ui;GameObject panel,hud;UnityEngine.UI.Text cue;bool evidence;float nextCaption;
        public static float Gain(float distance,float range)=>Mathf.Clamp01(1-(distance-4)/Mathf.Max(1,range-4));
        public static string Bearing(Vector3 relative)
        {
            if(Mathf.Abs(relative.x)>Mathf.Abs(relative.z))return relative.x<0?"links":"rechts";
            return relative.z<0?"achter je":"voor je";
        }
        void Start()
        {
            app=GetComponent<FerrarisApp>();ui=GetComponent<VisitorUI>();app.View.gameObject.AddComponent<AudioListener>();
            Volume=Mathf.Clamp01(PlayerPrefs.GetFloat("landscape-volume",.6f));Muted=PlayerPrefs.GetInt("landscape-muted",0)==1;
            var church=System.Array.Find(app.Data.buildings,b=>b.kind=="church");
            Add("Kerkklok","Een klokslag bij de kerk. Illustratieve opname; de klank en het uurrooster van Winksele in 1775 zijn niet bekend.",new Vector3(church.x,app.Area.Height(church.x,church.z)+18,church.z),"bell",180);
            foreach(var animal in app.World.GetComponentsInChildren<GrazingAnimal>(true))if(animal.transform.parent.name=="Grazing sheep"){Add("Blatend schaap","Een schaap roept bij de grazende dieren. Dit maakt de illustratieve veestapel ook zonder geluid vindbaar.",animal.transform.position,"sheep",65);break;}
            var work=app.World.Tableaux.Sites[4].position;
            Add("Houtwerk op het erf","Kloppend hout bij het illustratieve dorstafereel. De opname is algemene klankillustratie, geen reconstructie van dit werktuig of bewijs van een historische werkplaats.",work+Vector3.up,"wood",80);
            hud=ui.Box(new Rect(835,650,580,102));cue=ui.Text(new Rect(852,660,545,86),"",21,hud.transform);ApplyVolume();
        }
        void Add(string name,string description,Vector3 position,string clip,float range)
        {
            var go=new GameObject(name);go.transform.SetParent(app.World.transform,false);go.transform.position=position;
            var source=go.AddComponent<AudioSource>();source.clip=Resources.Load<AudioClip>("Discovery/Audio/"+clip);source.playOnAwake=false;source.loop=false;source.spatialBlend=1;source.rolloffMode=AudioRolloffMode.Linear;source.minDistance=4;source.maxDistance=range;source.dopplerLevel=0;
            Sites.Add(new SoundSite{name=name,description=description,source=source,range=range,next=Time.time+Sites.Count*2});
        }
        public void SetVolume(float value){Volume=Mathf.Clamp01(value);ApplyVolume();if(panel!=null)Draw();}
        public void SetMuted(bool value){Muted=value;ApplyVolume();if(panel!=null)Draw();}
        void ApplyVolume(){foreach(var site in Sites)site.source.volume=Muted?0:Volume;PlayerPrefs.SetFloat("landscape-volume",Volume);PlayerPrefs.SetInt("landscape-muted",Muted?1:0);PlayerPrefs.Save();}
        void Update()
        {
            if(hud==null)return;hud.SetActive(app.InWorld&&!ui.PanelOpen);if(!app.InWorld)return;
            foreach(var site in Sites)if(Time.time>=site.next&&!site.source.isPlaying){site.source.Play();site.next=Time.time+(site.source.clip?.length??1)+12;}
            if(Time.unscaledTime<nextCaption)return;nextCaption=Time.unscaledTime+.25f;
            SoundSite nearest=null;float best=0;
            foreach(var site in Sites){float gain=Gain(Vector3.Distance(app.View.transform.position,site.source.transform.position),site.range);if(gain>best){best=gain;nearest=site;}}
            hud.SetActive(nearest!=null&&nearest.source.isPlaying&&!ui.PanelOpen);
            cue.text=nearest==null?"Geluidsplekken: geen bron dichtbij.\nKies Ontdek voor de geluidsplekken.":$"{(Muted||Volume==0?"Geluid gedempt • ":"")}{nearest.name}\n{Vector3.Distance(app.View.transform.position,nearest.source.transform.position):F0} m • {Bearing(app.View.transform.InverseTransformPoint(nearest.source.transform.position))}\nIllustratief geluid • Ontdek: geluid en ondertitels";
        }
        public void Open(){ui.ClosePanel?.Invoke();evidence=false;Draw();}
        public void Close(){ui.RemovePanel(panel);panel=null;ui.PanelOpen=false;ui.ClosePanel=null;ui.ClearKeyboardFocus();}
        void Draw()
        {
            ui.RemovePanel(panel);panel=ui.Box(new Rect(28,180,760,680));ui.PanelRoot=panel.transform;ui.PanelOpen=true;ui.ClosePanel=Close;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            ui.Text(new Rect(48,197,610,42),"Luister naar het landschap",28,panel.transform);ui.Button(new Rect(690,197,76,40),"Sluiten",Close,panel.transform,19);
            ui.Text(new Rect(48,258,710,45),$"Omgevingsgeluid: {Volume*100:F0}% {(Muted?"(gedempt)":"")}",25,panel.transform);
            ui.Button(new Rect(48,310,160,48),"Zachter",()=>SetVolume(Volume-.2f),panel.transform);ui.Button(new Rect(224,310,160,48),"Luider",()=>SetVolume(Volume+.2f),panel.transform);ui.Button(new Rect(400,310,210,48),Muted?"Geluid aan":"Dempen",()=>SetMuted(!Muted),panel.transform);
            if(evidence)ui.Text(new Rect(48,385,706,365),"Hoe weten we dit?\nDe kerkpositie volgt de Ferrariskaart (KBR/NGI, blad 93). Dieren en de werkplek zijn illustratief geplaatst, geen gedocumenteerde geluidsbronnen uit 1775.\n\nHedendaagse opnamen, CC0:\n• Kerkklok: dsp9000, Freesound 76405.\n• Schaap: mikewest / AntumDeluge, OpenGameArt ‘Sheep Baa’.\n• Hout: StarNinjas, OpenGameArt ‘Saw or Wood Impact’.\n\nDe tekst toont dezelfde plek, richting en activiteit, ook met geluid uit. Er is geen gesproken tekst.",23,panel.transform);
            else for(int i=0;i<Sites.Count;i++)
            {
                var site=Sites[i];ui.Text(new Rect(48,385+i*120,700,76),site.name+" — "+site.description,22,panel.transform);
                ui.Button(new Rect(48,463+i*120,250,34),"Toon geluidsplek",()=>{var p=site.source.transform.position;app.Locate(p.x,p.z);Close();},panel.transform,20);
            }
            ui.Button(new Rect(48,795,310,46),"Hoe weten we dit?",()=>{evidence=!evidence;Draw();},panel.transform);
        }
    }
}
