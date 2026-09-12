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
