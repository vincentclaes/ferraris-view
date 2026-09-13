using System.Collections.Generic;
using UnityEngine;

namespace Ferraris
{
    // Authored illustrative stand-in: one face and costume throughout the day.
    // Replace presentation with a reviewed rigged asset for production realism.
    public sealed class StoryPerson : MonoBehaviour
    {
        Transform head,mouth,leftArm,rightArm,leftLeg,rightLeg,basket;
        readonly List<Material> materials=new();
        Mesh skirtMesh,apronMesh;
        float stride;
        public void Build()
        {
            var skin=Surface("Huid",new Color(.64f,.43f,.32f));
            var jacket=Surface("Bruin wollen jak",new Color(.27f,.17f,.11f));
            var linen=Surface("Linnen muts en schort",new Color(.76f,.72f,.60f));
            var skirt=Surface("Blauwgrijze rok",new Color(.23f,.29f,.30f));
            var hair=Surface("Bruin haar",new Color(.15f,.095f,.055f));
            var dark=Surface("Schoenen en ogen",new Color(.065f,.045f,.03f));
            var wicker=Surface("Rieten mand",new Color(.44f,.29f,.13f));
            Part("Jak",transform,new Vector3(0,1.16f,0),new Vector3(.39f,.49f,.24f),jacket);
            skirtMesh=Drape("Rok",skirt,.95f,.15f,.22f,.40f,Mathf.PI*2,0);
            apronMesh=Drape("Schort",linen,.94f,.24f,.23f,.38f,1.45f,.018f);
            Part("Hals",transform,new Vector3(0,1.43f,0),new Vector3(.13f,.16f,.12f),skin);
            Part("Halsdoek",transform,new Vector3(0,1.36f,.09f),new Vector3(.33f,.12f,.10f),linen);
            head=new GameObject("Hoofd").transform;head.SetParent(transform,false);head.localPosition=new Vector3(0,1.59f,0);
            Part("Gezicht",head,Vector3.zero,new Vector3(.205f,.27f,.21f),skin);
            Part("Haar",head,new Vector3(0,.065f,-.045f),new Vector3(.22f,.21f,.19f),hair);
            Part("Muts",head,new Vector3(0,.096f,-.035f),new Vector3(.235f,.20f,.205f),linen);
            Part("Neus",head,new Vector3(0,-.01f,.105f),new Vector3(.043f,.067f,.046f),skin);
            foreach(float side in new[]{-1f,1f})
            {
                Part("Oog",head,new Vector3(side*.042f,.026f,.092f),new Vector3(.027f,.014f,.012f),dark);
                Part("Wenkbrauw",head,new Vector3(side*.043f,.049f,.091f),new Vector3(.04f,.009f,.012f),hair);
            }
            mouth=Part("Mond",head,new Vector3(0,-.068f,.089f),new Vector3(.058f,.009f,.015f),jacket);
            leftArm=Limb("Linkerarm",new Vector3(-.23f,1.35f,0),jacket,skin,false);
            rightArm=Limb("Rechterarm",new Vector3(.23f,1.35f,0),jacket,skin,false);
            leftLeg=Limb("Linkerbeen",new Vector3(-.12f,.57f,0),linen,dark,true);
            rightLeg=Limb("Rechterbeen",new Vector3(.12f,.57f,0),linen,dark,true);
            basket=new GameObject("Mand").transform;basket.SetParent(leftArm,false);basket.localPosition=new Vector3(-.05f,-.59f,0);
            Part("Mand",basket,Vector3.zero,new Vector3(.29f,.20f,.24f),wicker);
            for(int i=0;i<9;i++)
            {
                float a=i*Mathf.PI/8;
                Part("Hengsel",basket,new Vector3(Mathf.Cos(a)*.135f,.07f+Mathf.Sin(a)*.15f,0),new Vector3(.04f,.045f,.025f),wicker);
            }
            var collider=gameObject.AddComponent<CapsuleCollider>();collider.center=Vector3.up*.9f;collider.height=1.8f;collider.radius=.32f;collider.isTrigger=true;
        }
        Material Surface(string name,Color color)
        {
            var m=new Material(Shader.Find("Standard")){name=name,color=color};m.SetFloat("_Glossiness",.12f);materials.Add(m);return m;
        }
        Transform Part(string name,Transform parent,Vector3 position,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name=name;
            go.GetComponent<Collider>().enabled=false;Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;
            go.GetComponent<Renderer>().sharedMaterial=material;return go.transform;
        }
        Transform Limb(string name,Vector3 position,Material cloth,Material end,bool leg)
        {
            var pivot=new GameObject(name).transform;pivot.SetParent(transform,false);pivot.localPosition=position;
            Part(name,pivot,new Vector3(0,-.22f,0),new Vector3(leg?.11f:.115f,.48f,.12f),cloth);
            Part(leg?"Schoen":"Hand",pivot,new Vector3(0,-.48f,leg?.055f:0),new Vector3(.11f,leg?.11f:.15f,leg?.24f:.08f),end);
            return pivot;
        }
        Mesh Drape(string name,Material material,float top,float bottom,float topRadius,float bottomRadius,float arc,float offset)
        {
            const int segments=40,rows=5;
            var vertices=new List<Vector3>();var indices=new List<int>();
            for(int row=0;row<=rows;row++)for(int i=0;i<=segments;i++)
            {
                float t=row/(float)rows,a=-arc*.5f+arc*i/segments;
                float radius=Mathf.Lerp(topRadius,bottomRadius,t)*(1+.035f*Mathf.Sin(i*2.4f));
                vertices.Add(new Vector3(Mathf.Sin(a)*radius,Mathf.Lerp(top,bottom,t),Mathf.Cos(a)*radius*.75f+offset));
                if(row==rows||i==segments)continue;
                int v=row*(segments+1)+i;indices.AddRange(new[]{v,v+segments+1,v+1,v+1,v+segments+1,v+segments+2});
            }
            var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var go=new GameObject(name);go.transform.SetParent(transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;return mesh;
        }
        public void Animate(float speed,Vector3 visitor,bool gesture,float dt)
        {
            stride+=speed*dt*5;float swing=Mathf.Sin(stride)*Mathf.Min(speed,1.5f)*16;
            leftLeg.localRotation=Quaternion.Euler(swing,0,0);rightLeg.localRotation=Quaternion.Euler(-swing,0,0);
            leftArm.localRotation=Quaternion.Euler(-swing*.35f,0,7);
            rightArm.localRotation=Quaternion.Euler(gesture?-32:swing*.6f,0,gesture?-28:-7);
            Vector3 direction=visitor-transform.position;direction.y=0;
            if(speed<.05f&&direction.sqrMagnitude>.05f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(direction),100*dt);
            head.localRotation=Quaternion.Euler(Mathf.Sin(Time.time*1.1f)*1.5f,0,0);
        }
        public void SetSpeaking(bool speaking)
        {
            // Simple speaking cue; a production face needs phoneme-based animation.
            mouth.localScale=new Vector3(.058f,speaking?.012f+.025f*Mathf.Abs(Mathf.Sin(Time.time*13)):.009f,.015f);
        }
        void OnDestroy(){foreach(var material in materials)Destroy(material);if(skirtMesh!=null)Destroy(skirtMesh);if(apronMesh!=null)Destroy(apronMesh);}
    }
}
