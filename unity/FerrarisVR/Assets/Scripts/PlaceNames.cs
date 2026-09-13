using System;
using UnityEngine;
namespace Ferraris
{
    [Serializable] public class PlaceNameEntry { public string name,earlier,certainty,explanation,evidence;public float x,z; }
    [Serializable] public class PlaceNameContent { public PlaceNameEntry[] entries; }
    public class PlaceNames : MonoBehaviour
    {
        public PlaceNameContent Content { get; private set; }
        public int Selected { get; private set; }=-1;
        FerrarisApp app;VisitorUI ui;GameObject panel;bool evidence;
        void Start()
        {
            app=GetComponent<FerrarisApp>();ui=GetComponent<VisitorUI>();Content=JsonUtility.FromJson<PlaceNameContent>(Resources.Load<TextAsset>("Discovery/names").text);
        }
        public void Open(){ui.ClosePanel?.Invoke();Selected=-1;evidence=false;Draw();}
        public void Choose(int index){Selected=index;evidence=false;var n=Content.entries[index];app.Locate(n.x,n.z);Draw();}
        public void Close(){ui.RemovePanel(panel);panel=null;ui.PanelOpen=false;ui.ClosePanel=null;ui.ClearKeyboardFocus();}
        void Draw()
        {
            ui.RemovePanel(panel);panel=ui.Box(new Rect(28,180,760,680));ui.PanelRoot=panel.transform;ui.PanelOpen=true;ui.ClosePanel=Close;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            ui.Text(new Rect(48,197,610,42),"Wat vertelt een plaatsnaam?",28,panel.transform);ui.Button(new Rect(690,197,76,40),"Sluiten",Close,panel.transform,19);
            if(Selected<0)
            {
                ui.Text(new Rect(48,260,700,125),"Kies een naam om de plek op de kaart te markeren. Een naam kan iets over het verleden vertellen, maar bewijst op zichzelf geen vroegere bewoner, activiteit of landgebruik.",25,panel.transform);
                for(int i=0;i<Content.entries.Length;i++){int index=i;var n=Content.entries[i];ui.Button(new Rect(48,420+i*100,715,74),n.name+"\n"+n.certainty,()=>Choose(index),panel.transform,23);}
                return;
            }
            var entry=Content.entries[Selected];
            ui.Text(new Rect(48,265,700,105),entry.name+"\n"+entry.earlier+"\n"+entry.certainty,25,panel.transform);
            ui.Text(new Rect(48,390,704,350),evidence?"Hoe weten we dit?\n"+entry.evidence:entry.explanation,25,panel.transform);
            ui.Button(new Rect(48,780,250,46),"Hoe weten we dit?",()=>{evidence=!evidence;Draw();},panel.transform);
            ui.Button(new Rect(310,780,200,46),"Verken de plek",()=>{app.EnterWorld(entry.x,entry.z);Close();},panel.transform,23);
            ui.Button(new Rect(526,780,237,46),"Andere namen",()=>{Selected=-1;Draw();},panel.transform);
        }
    }
}
