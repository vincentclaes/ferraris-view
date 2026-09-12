using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ferraris
{
    public class HistoricalWorld : MonoBehaviour
    {
        public AreaData Area { get; private set; }
        public GameObject TerrainObject { get; private set; }
        public Transform RoadRoot, BuildingRoot;
        readonly List<Vector3> vertices=new();
        readonly List<int> triangles=new();
        readonly List<Color> colors=new();
        Material groundMaterial,objectMaterial;
        Mesh treeMesh;
        readonly List<Matrix4x4[]> treeBatches=new();
        public bool ShowTrees=true;
        public int TreeCount { get; private set; }
        static readonly Color Grass=new(.43f,.48f,.27f),Brick=new(.59f,.32f,.22f),Roof=new(.32f,.22f,.16f);

        public void Build(AreaData area,WorldData data)
        {
            Area=area;
            objectMaterial=new Material(Shader.Find("Ferraris/MobileLandscape")){enableInstancing=true};
            groundMaterial=new Material(objectMaterial);
            groundMaterial.mainTexture=Resources.Load<Texture2D>("Winksele/landcover");
            const int n=257;
            var uv=new List<Vector2>();
            for(int z=0;z<n;z++) for(int x=0;x<n;x++)
            {
                float px=((float)x/(n-1)-.5f)*area.size,pz=((float)z/(n-1)-.5f)*area.size;
                vertices.Add(new Vector3(px,area.Height(px,pz),pz));colors.Add(Color.white);uv.Add(new Vector2((float)x/(n-1),(float)z/(n-1)));
                if(x<n-1 && z<n-1){int a=z*n+x;triangles.AddRange(new[]{a,a+n,a+1,a+1,a+n,a+n+1});}
            }
            TerrainObject=Flush("Terrain",groundMaterial,uv);
            TerrainObject.AddComponent<MeshCollider>().sharedMesh=TerrainObject.GetComponent<MeshFilter>().sharedMesh;
            foreach(Road r in data.roads)
            {
                int start=vertices.Count;
                for(int i=0;i<r.points.Length;i++)
                {
                    MapPoint p=r.points[i],before=r.points[Mathf.Max(0,i-1)],after=r.points[Mathf.Min(i+1,r.points.Length-1)];
                    Vector2 d=new Vector2(after.x-before.x,after.z-before.z).normalized;
                    foreach(int sign in new[]{-1,1})
                    {
                        float x=p.x+d.y*r.width*.5f*sign,z=p.z-d.x*r.width*.5f*sign;
                        vertices.Add(new Vector3(x,area.Height(x,z)+.08f,z));colors.Add(new Color(.65f,.53f,.36f));
                    }
                    if(i>0){int a=start+(i-1)*2;triangles.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});}
                }
            }
            RoadRoot=Flush("Ferraris dirt roads",objectMaterial).transform;
            var colliders=new GameObject("Building collisions");colliders.transform.SetParent(transform);
            foreach(Building b in data.buildings)
            {
                float h=b.kind=="barn"?4:5;
                float ground=area.Height(b.x,b.z);
                Quaternion rotation=Quaternion.Euler(0,b.yaw,0);
                Vector3 origin=new(b.x,ground,b.z);
                Box(origin+Vector3.up*h/2,new Vector3(b.width,h,b.depth),rotation,b.kind=="farmhouse"?new Color(.74f,.69f,.53f):Brick);
                Gable(origin+Vector3.up*h,b.width,b.depth,3,rotation,Roof);
                Box(origin+rotation*new Vector3(0,1.2f,-b.depth/2-.05f),new Vector3(1.1f,2.4f,.12f),rotation,new Color(.22f,.17f,.11f));
                foreach(int side in new[]{-1,1})
                    Box(origin+rotation*new Vector3(side*b.width*.28f,2.5f,-b.depth/2-.08f),new Vector3(.9f,1.1f,.15f),rotation,new Color(.19f,.20f,.16f));
                var collision=new GameObject(b.kind);collision.transform.SetParent(colliders.transform);collision.transform.SetPositionAndRotation(origin+Vector3.up*h/2,rotation);
                collision.AddComponent<BoxCollider>().size=new Vector3(b.width,h,b.depth);
                if(b.kind=="church")
                {
                    Vector3 tower=origin+rotation*new Vector3(-b.width*.36f,0,0);
                    Box(tower+Vector3.up*7,new Vector3(7,14,7),rotation,new Color(.64f,.60f,.48f));
                    Cone(tower+Vector3.up*14,5,9,4,new Color(.23f,.25f,.25f));
                }
            }
            BuildingRoot=Flush("Historical buildings",objectMaterial).transform;
            // One shared mesh and instanced batches; no tree GameObjects or colliders.
            Box(Vector3.up*2,new Vector3(.5f,4,.5f),Quaternion.identity,new Color(.30f,.23f,.14f));
            Cone(Vector3.up*2,2.8f,4.2f,7,new Color(.31f,.40f,.20f));
            Cone(Vector3.up*4.1f,2.1f,3.2f,7,new Color(.38f,.46f,.24f));
            treeMesh=MakeMesh();Clear();
            var batch=new List<Matrix4x4>();
            foreach(Tree t in data.trees)
            {
                batch.Add(Matrix4x4.TRS(new Vector3(t.x,area.Height(t.x,t.z),t.z),Quaternion.Euler(0,t.x*13,0),Vector3.one*t.scale));
                if(batch.Count==128){treeBatches.Add(batch.ToArray());batch.Clear();}
            }
            if(batch.Count>0)treeBatches.Add(batch.ToArray());TreeCount=data.trees.Length;
        }
        void Update()
        {
            if(!ShowTrees || treeMesh==null)return;
            foreach(var batch in treeBatches) Graphics.DrawMeshInstanced(treeMesh,0,objectMaterial,batch,batch.Length,null,ShadowCastingMode.Off,false,0,null,LightProbeUsage.Off);
        }
        void Box(Vector3 center,Vector3 size,Quaternion rotation,Color color)
        {
            int s=vertices.Count;
            foreach(var p in new[]{new Vector3(-1,-1,-1),new Vector3(1,-1,-1),new Vector3(1,1,-1),new Vector3(-1,1,-1),new Vector3(-1,-1,1),new Vector3(1,-1,1),new Vector3(1,1,1),new Vector3(-1,1,1)})
            {vertices.Add(center+rotation*Vector3.Scale(p,size*.5f));colors.Add(color);}
            foreach(int i in new[]{0,2,1,0,3,2,4,5,6,4,6,7,0,4,7,0,7,3,1,2,6,1,6,5,3,7,6,3,6,2,0,1,5,0,5,4})triangles.Add(s+i);
        }
        void Gable(Vector3 origin,float w,float d,float h,Quaternion q,Color c)
        {
            int s=vertices.Count;
            foreach(Vector3 p in new[]{new Vector3(-w/2,0,-d/2),new Vector3(w/2,0,-d/2),new Vector3(0,h,-d/2),new Vector3(-w/2,0,d/2),new Vector3(w/2,0,d/2),new Vector3(0,h,d/2)}){vertices.Add(origin+q*p);colors.Add(c);}
            foreach(int i in new[]{0,2,1,3,4,5,0,3,5,0,5,2,1,2,5,1,5,4})triangles.Add(s+i);
        }
        void Cone(Vector3 bottom,float radius,float height,int sides,Color c)
        {
            for(int i=0;i<sides;i++)
            {
                int s=vertices.Count;float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;
                vertices.Add(bottom+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius));vertices.Add(bottom+Vector3.up*height);vertices.Add(bottom+new Vector3(Mathf.Cos(b)*radius,0,Mathf.Sin(b)*radius));
                colors.Add(c);colors.Add(c);colors.Add(c);triangles.AddRange(new[]{s,s+1,s+2});
            }
        }
        Mesh MakeMesh(List<Vector2> uv=null)
        {
            Mesh mesh=new(){indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetColors(colors);
            if(uv!=null)mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        GameObject Flush(string title,Material material,List<Vector2> uv=null)
        {
            var go=new GameObject(title);go.transform.SetParent(transform);go.AddComponent<MeshFilter>().sharedMesh=MakeMesh(uv);go.AddComponent<MeshRenderer>().sharedMaterial=material;Clear();return go;
        }
        void Clear(){vertices.Clear();triangles.Clear();colors.Clear();}
    }
}
