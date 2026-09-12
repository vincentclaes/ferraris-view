using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Ferraris
{
    // One native canvas renders the same Dutch text on screen and in the headset.
    public class VisitorUI : MonoBehaviour
    {
        public Canvas Canvas { get; private set; }
        public RectTransform Root { get; private set; }
        Font font;bool wasXR;
        public bool PanelOpen; public Action ClosePanel;
        readonly List<(GameObject go,Rect rect,Action action)> buttons=new();
        void Awake()
        {
            Root=new GameObject("Bezoekersinformatie",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler)).GetComponent<RectTransform>();
            Canvas=Root.GetComponent<Canvas>();Canvas.renderMode=RenderMode.ScreenSpaceOverlay;Canvas.sortingOrder=20;
            var scaler=Root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1440,1000);scaler.matchWidthOrHeight=.5f;
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        void LateUpdate()
        {
            var app=FerrarisApp.Instance;if(app==null||wasXR==app.IsXR)return;
            wasXR=app.IsXR;
            if(wasXR)
            {
                Root.GetComponent<CanvasScaler>().enabled=false;Canvas.renderMode=RenderMode.WorldSpace;
                Root.SetParent(app.View.transform,false);Root.sizeDelta=new Vector2(1440,1000);
                Root.localPosition=new Vector3(0,0,2);Root.localRotation=Quaternion.identity;Root.localScale=Vector3.one*.0015f;
            }
        }
        public void Button(Rect rect,string value,Action action,Transform parent=null,int size=23)
        {
            var go=Box(rect);go.name=value;go.GetComponent<Image>().color=new Color(.19f,.29f,.23f,.98f);
            Text(new Rect(rect.x+8,rect.y+5,rect.width-16,rect.height-7),value,size,go.transform);
            if(parent!=null)go.transform.SetParent(parent,true);buttons.Add((go,rect,action));
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
        bool Hit(Vector2 point)=>buttons.Exists(b=>b.go!=null&&b.go.activeInHierarchy&&b.rect.Contains(point))||(PanelOpen&&new Rect(28,180,760,680).Contains(point));
        bool Click(Vector2 point)
        {
            buttons.RemoveAll(b=>b.go==null);
            for(int i=buttons.Count-1;i>=0;i--)if(buttons[i].go.activeInHierarchy&&buttons[i].rect.Contains(point)){var action=buttons[i].action;action();return true;}
            return Hit(point);
        }
        public GameObject Box(Rect rect)
        {
            var r=Rect("Informatiepaneel",rect,Root);var image=r.gameObject.AddComponent<Image>();image.color=new Color(.045f,.075f,.065f,.94f);return r.gameObject;
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
        void OnDestroy(){if(Root!=null)Destroy(Root.gameObject);}
    }
}
