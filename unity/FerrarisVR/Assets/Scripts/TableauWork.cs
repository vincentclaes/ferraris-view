using UnityEngine;

namespace Ferraris
{
    // Looping demonstrations, not a simulation of an identified person's working day.
    public class TableauWork : MonoBehaviour
    {
        public string Activity { get; private set; }
        public Transform Item { get; private set; }
        public Transform Basket { get; private set; }
        public Transform Handle { get; private set; }
        public Transform Beater { get; private set; }
        public Vector3 Pickup { get; private set; }
        public Vector3 Destination { get; private set; }
        public Vector3 BeaterTip=>Beater.TransformPoint(Vector3.forward*.65f);
        TableauResident resident;AreaData area;Vector3 pickup,drop;Transform binding;
        static readonly Color Wood=new(.35f,.23f,.12f),Straw=new(.66f,.48f,.22f);
        Transform Piece(string name,WorldMesh mesh,Material material,Vector3 position)
        {
            var go=mesh.Object(name,transform,material);go.transform.localPosition=position;return go.transform;
        }
        Vector3 Ground(float x,float z)
        {
            var p=transform.TransformPoint(new Vector3(x,0,z));return new Vector3(x,area.Height(p.x,p.z)-transform.position.y+.02f,z);
        }
        public void Build(TableauResident person,Material material,string activity,AreaData terrain)
        {
            resident=person;area=terrain;Activity=activity;
            if(activity=="dorsen")
            {
                var shaft=new WorldMesh();shaft.Beam(Vector3.down*.24f,Vector3.up*1.10f,.021f,Wood,12);
                Handle=Piece("Steel van de dorsvlegel",shaft,material,Vector3.zero);
                var head=new WorldMesh();head.Beam(Vector3.zero,Vector3.forward*.65f,.043f,Wood,12,.035f);
                // The leather connection turns independently of the long shaft.
                head.Beam(new Vector3(-.025f,0,0),new Vector3(.025f,0,0),.028f,new Color(.16f,.10f,.065f),8);
                Beater=Piece("Los verbonden slagknuppel",head,material,Vector3.zero);Beater.SetParent(Handle,false);Beater.localPosition=Vector3.up*1.10f;
                return;
            }
            var item=new WorldMesh();
            if(activity=="fruit rapen")
            {
                item.Ellipsoid(Vector3.zero,new Vector3(.065f,.06f,.061f),Quaternion.identity,new Color(.52f,.14f,.065f),14,9);
                item.Beam(Vector3.up*.05f,new Vector3(.012f,.085f,0),.005f,Wood,5);
                var basket=new WorldMesh();LivingTableaux.Basket(basket,Vector3.zero);
                for(int i=0;i<5;i++){float a=i*2.4f;basket.Ellipsoid(new Vector3(Mathf.Cos(a)*.12f,.075f,Mathf.Sin(a)*.12f),Vector3.one*.06f,Quaternion.identity,new Color(.47f,.40f,.12f),10,6);}
                Basket=Piece("Mand voor de geraapte vruchten",basket,material,Ground(-.35f,.55f));
                pickup=Ground(.24f,.48f)+Vector3.up*.065f;drop=Basket.localPosition+new Vector3(.09f,.09f,-.07f);
            }
            else
            {
                LivingTableaux.Sheaf(item,new Vector3(0,-.36f,0),.4f);
                pickup=Ground(.24f,.53f)+Vector3.up*.18f;drop=Ground(-.32f,.56f)+Vector3.up*.18f;
                if(activity=="schoven binden")
                {
                    var rope=new WorldMesh();
                    for(int i=0;i<24;i++){float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;rope.Beam(new Vector3(Mathf.Cos(a)*.15f,0,Mathf.Sin(a)*.15f),new Vector3(Mathf.Cos(b)*.15f,0,Mathf.Sin(b)*.15f),.011f,Wood,5);}
                    binding=Piece("Stroband rond de schoof",rope,material,new Vector3(0,.86f,.3f));
                }
            }
            Item=Piece(activity=="fruit rapen"?"Vrucht van grond naar mand":"Verzamelde graanhalmen",item,material,pickup);
            var collider=Item.gameObject.AddComponent<BoxCollider>();collider.size=activity=="fruit rapen"?Vector3.one*.13f:new Vector3(.35f,.85f,.35f);collider.isTrigger=true;
            Pickup=transform.TransformPoint(pickup);Destination=transform.TransformPoint(drop);
        }
        static float Ease(float t,float a,float b)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,t));
        public void Sample(float t)
        {
            if(Activity=="dorsen"){Thresh(t);return;}
            if(Activity=="schoven binden")
            {
                float tighten=Ease(t,.2f,.55f)*(1-Ease(t,.72f,.95f));resident.WorkPose(12,0,Mathf.Sin(t*Mathf.PI*2)*3);
                Item.localPosition=new Vector3(0,.88f,.30f);Item.localRotation=Quaternion.Euler(0,0,Mathf.Sin(t*Mathf.PI*2)*3);
                binding.localScale=Vector3.one*(1.15f-tighten*.26f);
                resident.Reach(true,transform.TransformPoint(new Vector3(.16f-tighten*.09f,.92f,.32f)));
                resident.Reach(false,transform.TransformPoint(new Vector3(-.16f+tighten*.09f,.92f,.32f)));return;
            }
            float reach=Ease(t,.08f,.32f),lift=Ease(t,.36f,.51f),transfer=Ease(t,.54f,.72f),reset=Ease(t,.85f,1);
            float crouch=Mathf.Lerp(reach*(1-lift*.65f),1,transfer)*(1-reset);
            resident.WorkPose(5+crouch*69,crouch,-20*transfer*(1-reset));
            var rest=new Vector3(.23f,.96f,.18f);var delivery=drop+Vector3.up*.29f;
            var target=Vector3.Lerp(rest,pickup,reach);target=Vector3.Lerp(target,new Vector3(.12f,.87f,.39f),lift);target=Vector3.Lerp(target,delivery,transfer);target=Vector3.Lerp(target,rest,reset);
            resident.Reach(true,transform.TransformPoint(target));
            resident.Reach(false,transform.TransformPoint(Vector3.Lerp(new Vector3(-.24f,.94f,.18f),new Vector3(-.24f,.41f,.30f),crouch)));
            if(t<.34f)Item.localPosition=pickup;
            else if(t<.75f)Item.position=resident.RightHand;
            else Item.localPosition=Vector3.Lerp(delivery,drop,Ease(t,.75f,.82f));
            if(Activity!="fruit rapen")Item.localRotation=Quaternion.Euler(Mathf.Lerp(90,0,lift)*(1-transfer)+90*transfer,0,0);
            // A short hidden reset keeps the finite illustration from filling the basket forever.
            Item.gameObject.SetActive(t<.94f);
        }
        void Thresh(float t)
        {
            float lift=Ease(t,.02f,.25f),strike=Ease(t,.42f,.60f),recover=Ease(t,.7f,1);
            float angle=Mathf.Lerp(112,-30,lift);angle=Mathf.Lerp(angle,116,strike);angle=Mathf.Lerp(angle,112,recover);
            float bend=Mathf.Lerp(23,0,lift);bend=Mathf.Lerp(bend,28,strike);bend=Mathf.Lerp(bend,23,recover);
            resident.WorkPose(bend,0,0);
            float height=Mathf.Lerp(1.06f,1.20f,lift);height=Mathf.Lerp(height,1.0f,strike);height=Mathf.Lerp(height,1.06f,recover);
            Handle.localPosition=new Vector3(0,height,.32f);
            Handle.localRotation=Quaternion.Euler(angle,0,0);
            var hinge=Beater.position;float floor=area.Height(hinge.x,hinge.z)+.18f;
            float maxDown=Mathf.Asin(Mathf.Clamp01((hinge.y-floor)/.65f))*Mathf.Rad2Deg;
            float lag=Mathf.Lerp(80,-45,Ease(t,.13f,.35f));lag=Mathf.Lerp(lag,90,strike);
            Beater.rotation=transform.rotation*Quaternion.Euler(Mathf.Min(lag,maxDown),0,0);
            resident.Reach(true,Handle.position);resident.Reach(false,Handle.TransformPoint(Vector3.up*.18f));
        }
    }
}
