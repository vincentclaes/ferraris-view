using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ferraris
{
    // These are narrative additions, never evidence of a building's historical use.
    public class LivingTableaux : MonoBehaviour
    {
        public StoryContent Content { get; private set; }
        public readonly List<Transform> Sites=new();
        static readonly Color Linen=new(.76f,.70f,.55f),Oak=new(.31f,.20f,.10f),Straw=new(.64f,.47f,.20f);
        Material material;
        public void Build(AreaData area,WorldData data)
        {
            Content=JsonUtility.FromJson<StoryContent>(Resources.Load<TextAsset>("Discovery/day").text);
            material=HistoricalWorld.Surface(null);
            for(int i=0;i<Content.stops.Length;i++)
            {
                var stop=Content.stops[i];var p=FindSpace(area,data,stop);
                stop.x=p.x;stop.z=p.z;
                var site=new GameObject("Tafereel "+(i+1)+" — "+stop.title);site.transform.SetParent(transform,false);site.transform.position=p;site.AddComponent<TableauSite>();Sites.Add(site.transform);
                var props=new WorldMesh();
                Vector3 Ground(float x,float z)=>new(x,area.Height(p.x+x,p.z+z)-p.y+.02f,z);
                void Person(float x,float z,bool skirt,string action,float yaw,Color coat)
                {
                    var actor=new GameObject("Bewoner — "+action);actor.transform.SetParent(site.transform,false);actor.transform.localPosition=Ground(x,z);actor.transform.localRotation=Quaternion.Euler(0,yaw,0);
                    actor.AddComponent<TableauResident>().Build(material,skirt,action,coat,i*.8f+x);
                }
                if(i==0)
                {
                    Person(-.65f,0,true,"mand dragen",155,new Color(.25f,.31f,.32f));Person(.8f,.4f,false,"praten",225,new Color(.40f,.29f,.20f));
                    Basket(props,Ground(1.35f,-.75f));Bench(props,Ground(.9f,.9f));
                }
                else if(i==1)
                {
                    Person(-.7f,0,true,"halmen verzamelen",180,new Color(.39f,.24f,.20f));Person(.95f,.45f,false,"schoven binden",160,new Color(.30f,.32f,.22f));
                    for(int s=0;s<4;s++)Sheaf(props,Ground(-1.35f+s*.75f,-.9f),s*.7f);
                }
                else if(i==2)
                {
                    Person(-.55f,0,true,"fruit rapen",160,new Color(.31f,.35f,.23f));Basket(props,Ground(.5f,-.5f));
                    for(int f=0;f<12;f++){var fruit=Ground(Mathf.Sin(f*2.4f)*1.3f,Mathf.Cos(f*2.4f)*1.2f);props.Ellipsoid(fruit+Vector3.up*.065f,new Vector3(.068f,.063f,.065f),Quaternion.identity,f%3==0?new Color(.43f,.13f,.06f):new Color(.49f,.47f,.14f),8,5);}
                }
                else if(i==3)
                {
                    Person(-.65f,0,true,"praten",125,new Color(.25f,.29f,.39f));Person(.65f,.2f,false,"praten",235,new Color(.43f,.31f,.22f));
                    Basket(props,Ground(-1.15f,-.5f));
                }
                else
                {
                    Person(-.35f,.35f,false,"dorsen",180,new Color(.34f,.28f,.20f));Person(1.25f,.25f,true,"mand dragen",215,new Color(.36f,.24f,.22f));
                    for(int b=0;b<6;b++)props.Box(Ground(-.45f,-.8f)+new Vector3((b-2.5f)*.25f,.045f,0),new Vector3(.24f,.07f,1.9f),Quaternion.identity,Oak);
                    for(int s=0;s<3;s++)Sheaf(props,Ground(-1.25f+s*.7f,-.7f),s*.5f);
                    Basket(props,Ground(1.45f,-.9f));
                }
                var objects=props.Object("Manden en oogstgerei",site.transform,material);objects.AddComponent<MeshCollider>().sharedMesh=objects.GetComponent<MeshFilter>().sharedMesh;
                Debug.Log($"FERRARIS_TABLEAU stop={i+1} x={p.x:F2} z={p.z:F2} residents={site.GetComponentsInChildren<TableauResident>().Length}");
            }
        }
        public bool WorkingSpace(float x,float z)
        {
            foreach(var site in Sites)if(new Vector2(x-site.position.x,z-site.position.z).sqrMagnitude<9)return true;return false;
        }
        public static Vector3 FindSpace(AreaData area,WorldData data,StoryStop stop)
        {
            for(int r=0;r<=28;r++)for(int j=0;j<(r==0?1:32);j++)
            {
                float angle=j*Mathf.PI/16,x=stop.x+r*Mathf.Cos(angle),z=stop.z+r*Mathf.Sin(angle);
                if(!Clear(data,x,z,3)||!Clear(data,x,z-5.5f,.8f))continue;
                if(stop.kind is "crop" or "orchard")
                {
                    bool same=false;foreach(var patch in data.patches)if(patch.kind==stop.kind&&WorldVegetation.Contains(patch,x,z)){same=true;break;}if(!same)continue;
                }
                float h=area.Height(x,z);if(Mathf.Abs(area.Height(x,z-5.5f)-h)>1.2f)continue;
                return new Vector3(x,h,z);
            }
            throw new InvalidOperationException("Geen vrije plek voor tafereel: "+stop.title);
        }
        public static bool Clear(WorldData data,float x,float z,float radius)
        {
            foreach(var b in data.buildings)
            {
                if(b.Contains(x,z))return false;
                var points=b.footprint;
                for(int i=0;i<points.Length;i++)if(HomeSearch.SegmentDistance(new Vector2(x,z),points[i],points[(i+1)%points.Length])<radius)return false;
            }
            foreach(var road in data.roads)for(int i=1;i<road.points.Length;i++)if(HomeSearch.SegmentDistance(new Vector2(x,z),road.points[i-1],road.points[i])<radius+road.width/2)return false;
            foreach(var tree in data.trees)if(Vector2.Distance(new Vector2(x,z),new Vector2(tree.x,tree.z))<radius+.3f*tree.scale)return false;
            return true;
        }
        static void Bench(WorldMesh mesh,Vector3 p)
        {
            mesh.Box(p+Vector3.up*.46f,new Vector3(1.1f,.085f,.37f),Quaternion.identity,Oak);
            foreach(float x in new[]{-.43f,.43f})foreach(float z in new[]{-.12f,.12f})mesh.Beam(p+new Vector3(x,0,z),p+new Vector3(x,.46f,z),.04f,Oak);
        }
        public static void Basket(WorldMesh mesh,Vector3 p)
        {
            // Open woven bowl with a raised handle, not a solid barrel.
            const int sides=24;
            for(int ring=0;ring<8;ring++)for(int n=0;n<sides;n++)
            {
                float a=n*Mathf.PI*2/sides,b=(n+1)*Mathf.PI*2/sides,r=.18f+ring*.01f,y=.025f+ring*.03f;
                mesh.Beam(p+new Vector3(Mathf.Cos(a)*r,y,Mathf.Sin(a)*r),p+new Vector3(Mathf.Cos(b)*r,y,Mathf.Sin(b)*r),.012f,Straw,4);
            }
            for(int n=0;n<sides;n++){float a=n*Mathf.PI*2/sides;mesh.Beam(p+new Vector3(Mathf.Cos(a)*.18f,.02f,Mathf.Sin(a)*.18f),p+new Vector3(Mathf.Cos(a)*.25f,.25f,Mathf.Sin(a)*.25f),.009f,Oak,4);}
            mesh.Ellipsoid(p+Vector3.up*.018f,new Vector3(.18f,.018f,.18f),Quaternion.identity,Straw,16,3);
            for(int n=0;n<16;n++){float a=n*Mathf.PI/16,b=(n+1)*Mathf.PI/16;mesh.Beam(p+new Vector3(Mathf.Cos(a)*.25f,.25f+Mathf.Sin(a)*.28f,0),p+new Vector3(Mathf.Cos(b)*.25f,.25f+Mathf.Sin(b)*.28f,0),.017f,Oak,5);}
        }
        static void Sheaf(WorldMesh mesh,Vector3 p,float phase)
        {
            for(int k=0;k<28;k++)
            {
                float a=k*2.4f+phase,r=.06f+.12f*(k%5)/4;var low=p+new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);var high=p+new Vector3(Mathf.Cos(a)*r*.8f,.7f+(k%4)*.055f,Mathf.Sin(a)*r*.8f);
                mesh.Beam(low,high,.009f,Straw,3);mesh.Ellipsoid(high,new Vector3(.018f,.075f,.019f),Quaternion.identity,Linen,5,4);
            }
            for(int k=0;k<12;k++){float a=k*Mathf.PI/6,b=(k+1)*Mathf.PI/6;mesh.Beam(p+new Vector3(Mathf.Cos(a)*.13f,.34f,Mathf.Sin(a)*.13f),p+new Vector3(Mathf.Cos(b)*.13f,.34f,Mathf.Sin(b)*.13f),.013f,Oak,4);}
        }
    }
    public class TableauSite : MonoBehaviour { }
    public class TableauResident : MonoBehaviour
    {
        public Transform Torso { get; private set; }
        Transform left,right,leftForearm,rightForearm,head;
        string action;float phase;
        static readonly Color Skin=new(.57f,.37f,.25f),Linen=new(.76f,.71f,.59f),Leather=new(.16f,.11f,.08f);
        public void Build(Material material,bool skirt,string activity,Color coat,float offset)
        {
            action=activity;phase=offset;
            Transform Piece(string name,WorldMesh mesh,Transform parent,Vector3 position){var part=mesh.Object(name,parent,material);part.transform.localPosition=position;return part.transform;}
            var lower=new WorldMesh();
            for(int side=-1;side<=1;side+=2)
            {
                lower.Ellipsoid(new Vector3(side*.12f,.085f,.055f),new Vector3(.085f,.075f,.17f),Quaternion.identity,Leather);
                lower.Beam(new Vector3(side*.12f,.12f,0),new Vector3(side*.12f,.48f,0),.053f,Linen,10,.065f);
                if(!skirt)lower.Beam(new Vector3(side*.12f,.44f,0),new Vector3(side*.13f,.98f,0),.095f,coat,12,.115f);
            }
            if(skirt)
            {
                for(int n=0;n<48;n++)
                {
                    float a=n*Mathf.PI/24,b=(n+1)*Mathf.PI/24;
                    Vector3 P(float angle,float y,float radius)=>new(Mathf.Cos(angle)*radius,y,Mathf.Sin(angle)*radius*.8f);
                    var tint=Color.Lerp(coat,Color.black,n%2==0?.05f:.17f);
                    lower.Quad(P(a,.17f,.37f),P(a,.92f,.17f),P(b,.92f,.17f),P(b,.17f,.37f),tint);
                    if(n>=4&&n<20)lower.Quad(P(a,.2f,.38f),P(a,.92f,.18f),P(b,.92f,.18f),P(b,.2f,.38f),Linen);
                }
            }
            Piece("Rok, kousen en schoenen",lower,transform,Vector3.zero);
            var body=new WorldMesh();body.Ellipsoid(new Vector3(0,.23f,0),new Vector3(.20f,.30f,.115f),Quaternion.identity,coat,18,12);
            body.Beam(new Vector3(0,.45f,0),new Vector3(0,.56f,0),.055f,Skin,10);
            body.Triangle(new Vector3(-.15f,.45f,.095f),new Vector3(0,.16f,.13f),new Vector3(.15f,.45f,.095f),Linen);
            for(int b=0;b<5;b++)body.Ellipsoid(new Vector3(.025f,.09f+b*.055f,.117f),Vector3.one*.011f,Quaternion.identity,Leather,6,4);
            Torso=Piece("Bovenlichaam",body,transform,Vector3.up*.93f);
            var face=new WorldMesh();face.Ellipsoid(Vector3.zero,new Vector3(.106f,.148f,.107f),Quaternion.identity,Skin,20,14);
            face.Ellipsoid(new Vector3(0,-.015f,.10f),new Vector3(.023f,.034f,.027f),Quaternion.identity,Skin,10,7);
            for(int side=-1;side<=1;side+=2)
            {
                face.Ellipsoid(new Vector3(side*.042f,.024f,.094f),new Vector3(.017f,.008f,.009f),Quaternion.identity,Linen,8,5);
                face.Ellipsoid(new Vector3(side*.042f,.024f,.102f),new Vector3(.006f,.006f,.003f),Quaternion.identity,Leather,8,5);
                face.Beam(new Vector3(side*.025f,.045f,.097f),new Vector3(side*.06f,.046f,.087f),.006f,Leather,5);
                face.Ellipsoid(new Vector3(side*.104f,-.006f,0),new Vector3(.021f,.032f,.017f),Quaternion.identity,Skin,8,6);
            }
            face.Beam(new Vector3(-.022f,-.062f,.095f),new Vector3(.022f,-.062f,.095f),.005f,new Color(.30f,.15f,.12f),5);
            if(skirt)
            {
                face.Ellipsoid(new Vector3(0,.094f,-.027f),new Vector3(.116f,.09f,.115f),Quaternion.identity,Linen,20,10);
                face.Ellipsoid(new Vector3(0,-.025f,-.082f),new Vector3(.105f,.1f,.06f),Quaternion.identity,Linen,16,8);
            }
            else
            {
                face.Ellipsoid(new Vector3(0,.122f,0),new Vector3(.22f,.018f,.19f),Quaternion.identity,Leather,24,5);
                face.Beam(new Vector3(0,.12f,0),new Vector3(0,.24f,0),.108f,coat,20,.085f);
                face.Ellipsoid(new Vector3(0,.24f,0),new Vector3(.085f,.013f,.085f),Quaternion.identity,coat,20,4);
            }
            head=Piece("Gezicht en hoofddeksel",face,Torso,new Vector3(0,.65f,0));
            for(int side=-1;side<=1;side+=2)
            {
                var upper=new WorldMesh();upper.Beam(Vector3.zero,new Vector3(0,-.27f,0),.079f,coat,12,.06f);upper.Ellipsoid(Vector3.zero,Vector3.one*.082f,Quaternion.identity,coat);
                var arm=Piece(side<0?"Linker bovenarm":"Rechter bovenarm",upper,Torso,new Vector3(side*.22f,.42f,0));
                var fore=new WorldMesh();fore.Beam(Vector3.zero,new Vector3(0,-.23f,0),.058f,Linen,12,.038f);fore.Ellipsoid(new Vector3(0,-.26f,.005f),new Vector3(.036f,.07f,.025f),Quaternion.identity,Skin,10,7);
                var elbow=Piece("Onderarm en hand",fore,arm,new Vector3(0,-.27f,0));
                if(side<0){left=arm;leftForearm=elbow;}else{right=arm;rightForearm=elbow;}
            }
            if(action=="mand dragen")
            {
                var basket=new WorldMesh();LivingTableaux.Basket(basket,new Vector3(0,-.78f,0));Piece("Gedragen mand",basket,leftForearm,Vector3.zero);
            }
            if(action=="dorsen")
            {
                var tool=new WorldMesh();tool.Beam(new Vector3(0,-.27f,0),new Vector3(0,.85f,0),.022f,Leather,9);tool.Beam(new Vector3(0,.85f,0),new Vector3(0,.86f,.56f),.035f,coat,9);Piece("Illustratieve dorsvlegel",tool,rightForearm,Vector3.zero);
            }
            var collider=gameObject.AddComponent<CapsuleCollider>();collider.center=Vector3.up*.85f;collider.height=1.7f;collider.radius=.34f;
            Pose(0);
        }
        public void Pose(float time)
        {
            float wave=Mathf.Sin(time*1.4f+phase),slow=Mathf.Sin(time*.65f+phase);
            bool bending=action is "halmen verzamelen" or "fruit rapen" or "schoven binden";
            Torso.localRotation=Quaternion.Euler(bending?22+15*wave:action=="dorsen"?10+9*wave:2*slow,0,0);
            head.localRotation=Quaternion.Euler(bending?8:0,slow*9,0);
            left.localRotation=Quaternion.Euler(bending?-35:action=="praten"?-18-12*wave:-8,0,-7);
            right.localRotation=Quaternion.Euler(bending?-30:action=="dorsen"?-65-45*wave:-15-10*wave,0,7);
            leftForearm.localRotation=Quaternion.Euler(action=="mand dragen"?-12:bending?-18:-35-10*wave,0,0);
            rightForearm.localRotation=Quaternion.Euler(action=="dorsen"?-45:bending?-20:-35+10*wave,0,0);
        }
        void Update()
        {
            var camera=Camera.main;if(camera!=null&&(camera.transform.position-transform.position).sqrMagnitude<80*80)Pose(Time.time);
        }
    }
}
