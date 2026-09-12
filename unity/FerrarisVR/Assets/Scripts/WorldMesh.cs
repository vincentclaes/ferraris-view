using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ferraris
{
    // Metric UVs and separate face vertices keep masonry scale and hard edges.
    public sealed class WorldMesh
    {
        readonly List<Vector3> vertices=new();
        readonly List<Vector2> uv=new();
        readonly List<Color> colors=new();
        readonly List<int> triangles=new();
        public int VertexCount=>vertices.Count;
        public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color color,float repeat=1)
        {
            int n=vertices.Count;vertices.AddRange(new[]{a,b,c,d});
            float w=Vector3.Distance(a,b)*repeat,h=Vector3.Distance(b,c)*repeat;
            uv.AddRange(new[]{Vector2.zero,new Vector2(w,0),new Vector2(w,h),new Vector2(0,h)});
            // Front/right box faces wind upward first; keep brick courses horizontal.
            if(Mathf.Abs((b-a).normalized.y)>.9f)
            {
                uv[n]=Vector2.zero;uv[n+1]=new Vector2(0,w);uv[n+2]=new Vector2(h,w);uv[n+3]=new Vector2(h,0);
            }
            for(int i=0;i<4;i++)colors.Add(color);
            triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
        }
        public void Card(Vector3 centre,Vector2 size,Quaternion rotation,Color color,float atlasMaxU=1)
        {
            int n=uv.Count;
            Quad(centre+rotation*new Vector3(-size.x/2,-size.y/2,0),centre+rotation*new Vector3(-size.x/2,size.y/2,0),centre+rotation*new Vector3(size.x/2,size.y/2,0),centre+rotation*new Vector3(size.x/2,-size.y/2,0),color);
            uv[n]=new Vector2(0,0);uv[n+1]=new Vector2(0,1);uv[n+2]=new Vector2(atlasMaxU,1);uv[n+3]=new Vector2(atlasMaxU,0);
        }
        public void Triangle(Vector3 a,Vector3 b,Vector3 c,Color color)
        {
            int n=vertices.Count;vertices.AddRange(new[]{a,b,c});colors.AddRange(new[]{color,color,color});
            Vector3 normal=Vector3.Cross(b-a,c-a);
            Vector2 Project(Vector3 p)=>Mathf.Abs(normal.y)>Mathf.Max(Mathf.Abs(normal.x),Mathf.Abs(normal.z))?new Vector2(p.x,p.z):Mathf.Abs(normal.x)>Mathf.Abs(normal.z)?new Vector2(p.z,p.y):new Vector2(p.x,p.y);
            uv.AddRange(new[]{Project(a),Project(b),Project(c)});triangles.AddRange(new[]{n,n+1,n+2});
        }
        public void Box(Vector3 centre,Vector3 size,Quaternion q,Color color)
        {
            var p=new Vector3[8];int i=0;
            foreach(var v in new[]{new Vector3(-1,-1,-1),new Vector3(1,-1,-1),new Vector3(1,1,-1),new Vector3(-1,1,-1),new Vector3(-1,-1,1),new Vector3(1,-1,1),new Vector3(1,1,1),new Vector3(-1,1,1)})p[i++]=centre+q*Vector3.Scale(v,size/2);
            foreach(var f in new[]{new[]{0,3,2,1},new[]{4,5,6,7},new[]{0,4,7,3},new[]{1,2,6,5},new[]{3,7,6,2},new[]{0,1,5,4}})Quad(p[f[0]],p[f[1]],p[f[2]],p[f[3]],color);
        }
        public void Beam(Vector3 a,Vector3 b,float radius,Color color,int sides=7,float tip=-1)
        {
            Vector3 direction=(b-a).normalized;Quaternion q=Quaternion.FromToRotation(Vector3.up,direction);if(tip<0)tip=radius;
            for(int i=0;i<sides;i++)
            {
                float t=i*Mathf.PI*2/sides,u=(i+1)*Mathf.PI*2/sides;
                Vector3 r=q*new Vector3(Mathf.Cos(t),0,Mathf.Sin(t)),s=q*new Vector3(Mathf.Cos(u),0,Mathf.Sin(u));
                Quad(a+r*radius,b+r*tip,b+s*tip,a+s*radius,color);
            }
        }
        public void Ellipsoid(Vector3 centre,Vector3 radii,Quaternion rotation,Color color,int sides=12,int rings=7)
        {
            int start=vertices.Count;
            for(int y=0;y<=rings;y++)for(int x=0;x<=sides;x++)
            {
                float a=x*Mathf.PI*2/sides,b=y*Mathf.PI/rings;
                Vector3 p=new(Mathf.Sin(b)*Mathf.Cos(a),Mathf.Cos(b),Mathf.Sin(b)*Mathf.Sin(a));
                vertices.Add(centre+rotation*Vector3.Scale(p,radii));uv.Add(new Vector2((float)x/sides,(float)y/rings));colors.Add(color);
                if(x<sides&&y<rings){int n=start+y*(sides+1)+x;triangles.AddRange(new[]{n,n+1,n+sides+1,n+1,n+sides+2,n+sides+1});}
            }
        }
        public Mesh Mesh(string name)
        {
            var m=new Mesh{name=name,indexFormat=IndexFormat.UInt32};m.SetVertices(vertices);m.SetColors(colors);m.SetUVs(0,uv);m.SetTriangles(triangles,0);m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();return m;
        }
        public GameObject Object(string name,Transform parent,Material material)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=Mesh(name);go.AddComponent<MeshRenderer>().sharedMaterial=material;return go;
        }
    }
}
