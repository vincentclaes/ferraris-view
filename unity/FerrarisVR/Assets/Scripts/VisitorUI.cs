using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
namespace Ferraris
{
    // One native canvas renders the same Dutch text on screen and in the headset.
    public class VisitorUI : MonoBehaviour
    {
        public Canvas Canvas { get; private set; }
        public RectTransform Root { get; private set; }
        Font font;bool wasXR;
        public bool PanelOpen; public Action ClosePanel; public Transform PanelRoot;
        readonly List<(GameObject go,Rect rect,Action action)> buttons=new();
        readonly List<(GameObject go,Rect rect)> surfaces=new();
        GameObject focused;string focusName;Rect focusRect;bool keyboardMode;
        public bool HasKeyboardFocus=>keyboardMode&&focused!=null&&InScope(focused);
        public string FocusedLabel=>HasKeyboardFocus?focused.name:null;
        bool InScope(GameObject go)=>go.activeInHierarchy&&(!PanelOpen||(PanelRoot!=null?go.transform.IsChildOf(PanelRoot):go.transform.parent!=Root));
        void Awake()
        {
            Root=new GameObject("Bezoekersinformatie",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler)).GetComponent<RectTransform>();
            Canvas=Root.GetComponent<Canvas>();Canvas.renderMode=RenderMode.ScreenSpaceOverlay;Canvas.sortingOrder=20;
            var scaler=Root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1440,1000);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            font=Resources.Load<Font>("Fonts/LiberationSans-Regular");
        }
        void LateUpdate()
        {
            var app=FerrarisApp.Instance;if(app==null||wasXR==app.IsXR)return;
            wasXR=app.IsXR;
            if(wasXR)UseWorldSpace(app.View);
        }
        public void UseWorldSpace(Camera view)
        {
            Root.GetComponent<CanvasScaler>().enabled=false;Canvas.renderMode=RenderMode.WorldSpace;
            Root.SetParent(view.transform,false);Root.sizeDelta=new Vector2(1440,1000);
            Root.localPosition=new Vector3(0,0,2);Root.localRotation=Quaternion.identity;Root.localScale=Vector3.one*.0015f;
        }
        public void Button(Rect rect,string value,Action action,Transform parent=null,int size=23)
        {
            var go=Box(rect);go.name=value;go.GetComponent<Image>().color=new Color(.19f,.29f,.23f,.98f);
            Text(new Rect(rect.x+8,rect.y+5,rect.width-16,rect.height-7),value,size,go.transform);
            if(parent!=null)go.transform.SetParent(parent,true);buttons.Add((go,rect,action));
            if(keyboardMode&&value==focusName&&rect==focusRect)Focus(go,rect);
        }
        public void RemovePanel(GameObject panel)
        {
            if(panel==null)return;panel.SetActive(false);
            if(Application.isPlaying)Destroy(panel);else DestroyImmediate(panel);
        }
        public void ClearKeyboardFocus()
        {
            if(focused!=null&&focused.TryGetComponent<Outline>(out var outline))outline.enabled=false;
            focused=null;focusName=null;keyboardMode=false;
        }
        void Focus(GameObject go,Rect rect)
        {
            if(focused!=null&&focused.TryGetComponent<Outline>(out var old))old.enabled=false;
            focused=go;focusName=go.name;focusRect=rect;keyboardMode=true;
            var outline=go.GetComponent<Outline>()??go.AddComponent<Outline>();outline.effectColor=new Color(1,.76f,.25f);outline.effectDistance=new Vector2(3,-3);outline.enabled=true;
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }
        public void MoveFocus(int direction)
        {
            buttons.RemoveAll(b=>b.go==null);var active=buttons.FindAll(b=>InScope(b.go));
            if(active.Count==0){ClearKeyboardFocus();return;}
            int index=active.FindIndex(b=>b.go==focused);index=index<0?(direction>0?0:active.Count-1):(index+direction+active.Count)%active.Count;
            Focus(active[index].go,active[index].rect);
        }
        public bool ActivateFocused()
        {
            if(!HasKeyboardFocus)return false;
            var selected=buttons.Find(b=>b.go==focused);selected.action();return true;
        }
        public bool HandleKeyboard(Keyboard keyboard)
        {
            if(keyboard==null)return false;
            if(keyboard.tabKey.wasPressedThisFrame){MoveFocus(keyboard.shiftKey.isPressed?-1:1);return true;}
            if(!HasKeyboardFocus)return false;
            if(keyboard.enterKey.wasPressedThisFrame||keyboard.numpadEnterKey.wasPressedThisFrame||keyboard.spaceKey.wasPressedThisFrame)return ActivateFocused();
            if(keyboard.downArrowKey.wasPressedThisFrame||keyboard.rightArrowKey.wasPressedThisFrame){MoveFocus(1);return true;}
            if(keyboard.upArrowKey.wasPressedThisFrame||keyboard.leftArrowKey.wasPressedThisFrame){MoveFocus(-1);return true;}
            return false;
        }
        public bool ClickScreen(Vector2 point)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Root,point,null,out var p);
            return Click(new Vector2(p.x+Root.rect.width/2,Root.rect.height/2-p.y));
        }
        public bool ClickRay(Ray ray,bool pressed,out Vector3 end)
        {
            end=ray.origin+ray.direction*4;
            if(!new Plane(Root.forward,Root.position).Raycast(ray,out float distance)||distance<0)return false;
            end=ray.GetPoint(distance);Vector3 p=Root.InverseTransformPoint(end);var point=new Vector2(p.x+720,500-p.y);
            return pressed?Click(point):Hit(point);
        }
        public bool BlocksScreen(Vector2 point)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Root,point,null,out var p);
            return Hit(new Vector2(p.x+Root.rect.width/2,Root.rect.height/2-p.y));
        }
        bool Hit(Vector2 point)=>surfaces.Exists(b=>b.go!=null&&b.go.activeInHierarchy&&b.rect.Contains(point));
        bool Click(Vector2 point)
        {
            buttons.RemoveAll(b=>b.go==null);surfaces.RemoveAll(b=>b.go==null);
            for(int i=buttons.Count-1;i>=0;i--)if(InScope(buttons[i].go)&&buttons[i].rect.Contains(point)){var action=buttons[i].action;ClearKeyboardFocus();action();return true;}
            return Hit(point);
        }
        public GameObject Box(Rect rect)
        {
            var r=Rect("Informatiepaneel",rect,Root);var image=r.gameObject.AddComponent<Image>();image.color=new Color(.045f,.075f,.065f,.94f);surfaces.Add((r.gameObject,rect));return r.gameObject;
        }
        public Text Text(Rect rect,string value,int size=23,Transform parent=null)
        {
            // Coordinates always refer to the full canvas, even for a grouped card.
            var r=Rect(value,rect,Root);var t=r.gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.color=new Color(.97f,.94f,.83f);t.text=value;t.supportRichText=false;t.raycastTarget=false;
            t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;
            if(parent!=null)r.SetParent(parent,true);return t;
        }
        static RectTransform Rect(string name,Rect rect,Transform parent)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(rect.x,-rect.y);r.sizeDelta=rect.size;return r;
        }
        void OnDestroy(){if(Root!=null){if(Application.isPlaying)Destroy(Root.gameObject);else DestroyImmediate(Root.gameObject);}}
    }
}
