using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ferraris
{
 public class HistoricalWorld:MonoBehaviour
 {
  public AreaData Area{get;private set;}
  public GameObject TerrainObject{get;private set;}
  public Transform RoadRoot,BuildingRoot;
  public bool ShowTrees=true;
  public int TreeCount{get;private set;}
  public int AnimalCount{get;private set;}
  readonly Dictionary<string,WorldMesh> parts=new();
  readonly Dictionary<string,Material> materials=new();
  public Material Sky{get;private set;}
  WorldVegetation vegetation;
  public static Texture2D Texture(string name)=>Resources.Load<Texture2D>("Visuals/Textures/"+name);
  public static Material Surface(string texture,Color? tint=null,float scale=.5f)
  {
   var m=new Material(Shader.Find("Ferraris/WorldSurface")){enableInstancing=true};m.SetColor("_Color",tint??Color.white);m.SetFloat("_Scale",scale);
   if(texture!=null){m.SetTexture("_MainTex",Texture(texture+"_diff"));m.SetTexture("_BumpMap",Texture(texture+"_normal"));m.SetTexture("_RoughnessTex",Texture(texture+"_rough"));}return m;
  }
  WorldMesh Part(string key){if(!parts.TryGetValue(key,out var m)){m=new WorldMesh();parts[key]=m;}return m;}
  public void Build(AreaData area,WorldData data)
  {
   Area=area;TreeCount=data.trees.Length;
   foreach(string name in new[]{"brick","plaster","roof","wood","soil","bark"})materials[name]=Surface(name);
   materials["stone"]=Surface("plaster",new Color(.55f,.53f,.46f));materials["glass"]=Surface(null,new Color(.055f,.085f,.075f));materials["wool"]=Surface(null);materials["iron"]=Surface(null,new Color(.12f,.13f,.12f));
   ConfigureLight();BuildTerrain();BuildRoads(data);
   var buildings=new GameObject("Detailed Ferraris buildings");buildings.transform.SetParent(transform,false);BuildingRoot=buildings.transform;
   foreach(var b in data.buildings)BuildBuilding(b,buildings.transform);
   foreach(var pair in parts)if(pair.Value.VertexCount>0)pair.Value.Object(pair.Key+" architectural details",buildings.transform,materials[pair.Key]);
   vegetation=gameObject.AddComponent<WorldVegetation>();vegetation.Build(area,data);
   BuildAnimals(data);
  }
  void ConfigureLight()
  {
   Sky=new Material(Shader.Find("Skybox/Panoramic"));Sky.SetTexture("_MainTex",Texture("sky"));Sky.SetFloat("_Exposure",.55f);Sky.SetFloat("_Rotation",110);RenderSettings.skybox=Sky;
   RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.40f,.47f,.55f);RenderSettings.ambientEquatorColor=new Color(.35f,.35f,.30f);RenderSettings.ambientGroundColor=new Color(.24f,.22f,.17f);
   RenderSettings.fogColor=new Color(.68f,.74f,.78f);RenderSettings.fogStartDistance=220;RenderSettings.fogEndDistance=1700;
   QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowResolution=ShadowResolution.High;QualitySettings.shadowDistance=65;QualitySettings.shadowCascades=2;QualitySettings.pixelLightCount=1;
   foreach(var light in FindObjectsByType<Light>(FindObjectsSortMode.None))if(light.type==LightType.Directional){light.color=new Color(1,.92f,.78f);light.intensity=1.05f;light.shadows=LightShadows.Soft;light.shadowStrength=.68f;light.shadowBias=.035f;light.shadowNormalBias=.25f;light.transform.rotation=Quaternion.Euler(28,-55,0);RenderSettings.sun=light;break;}
  }
  void BuildTerrain()
  {
   var material=new Material(Shader.Find("Ferraris/TerrainSurface"));material.SetTexture("_MainTex",Resources.Load<Texture2D>("Winksele/landcover"));material.SetTexture("_Grass",Texture("grass_diff"));material.SetTexture("_Soil",Texture("soil_diff"));material.SetTexture("_BumpMap",Texture("grass_normal"));material.SetTexture("_SoilNormal",Texture("soil_normal"));material.SetFloat("_AreaSize",Area.size);
   const int n=257;var vertices=new Vector3[n*n];var uv=new Vector2[n*n];var triangles=new int[(n-1)*(n-1)*6];int k=0;
   for(int z=0;z<n;z++)for(int x=0;x<n;x++)
   {
    float px=((float)x/(n-1)-.5f)*Area.size,pz=((float)z/(n-1)-.5f)*Area.size;int i=z*n+x;vertices[i]=new Vector3(px,Area.Height(px,pz),pz);uv[i]=new Vector2((float)x/(n-1),(float)z/(n-1));
    if(x<n-1&&z<n-1){triangles[k++]=i;triangles[k++]=i+n;triangles[k++]=i+1;triangles[k++]=i+1;triangles[k++]=i+n;triangles[k++]=i+n+1;}
   }
   var mesh=new Mesh{name="DHMV terrain",indexFormat=IndexFormat.UInt32,vertices=vertices,uv=uv,triangles=triangles};mesh.RecalculateNormals();mesh.RecalculateTangents();
   TerrainObject=new GameObject("DHMV terrain");TerrainObject.transform.SetParent(transform,false);TerrainObject.AddComponent<MeshFilter>().sharedMesh=mesh;TerrainObject.AddComponent<MeshRenderer>().sharedMaterial=material;TerrainObject.AddComponent<MeshCollider>().sharedMesh=mesh;
   // Extend the view beyond the playable crop, without extending navigation.
   var horizon=new WorldMesh();float step=100;
   for(float z=-2000;z<2000;z+=step)for(float x=-2000;x<2000;x+=step)
   {
    if(x>=-500&&x<500&&z>=-500&&z<500)continue;
    Vector3 P(float px,float pz)=>new Vector3(px,Area.Height(px,pz)-Mathf.Max(0,Mathf.Max(Mathf.Abs(px),Mathf.Abs(pz))-500)*.006f,pz);
    horizon.Quad(P(x,z),P(x,z+step),P(x+step,z+step),P(x+step,z),Color.white);
   }
   var distant=horizon.Object("Distant countryside",transform,material);distant.AddComponent<MeshCollider>().sharedMesh=distant.GetComponent<MeshFilter>().sharedMesh;
  }
  void BuildRoads(WorldData data)
  {
   var road=new WorldMesh();
   foreach(var r in data.roads)for(int i=0;i<r.points.Length-1;i++)
   {
    var p=r.points[i];var next=r.points[i+1];Vector2 d=new Vector2(next.x-p.x,next.z-p.z).normalized;Vector2 side=new(d.y,-d.x);
    // Short strips follow uneven terrain and form shallow worn wheel tracks.
    float[] edges={-r.width*.5f,-r.width*.27f,-r.width*.13f,r.width*.13f,r.width*.27f,r.width*.5f};
    for(int j=0;j<edges.Length-1;j++)
    {
     Vector3 P(MapPoint point,float offset){float x=point.x+side.x*offset,z=point.z+side.y*offset;return new Vector3(x,Area.Height(x,z)+.13f,z);}
     Color c=j==1||j==3?new Color(.66f,.59f,.48f):j==2?new Color(.83f,.80f,.65f):new Color(.89f,.83f,.73f);
     road.Quad(P(p,edges[j]),P(next,edges[j]),P(next,edges[j+1]),P(p,edges[j+1]),c);
    }
   }
   RoadRoot=road.Object("Rutted earth roads",transform,materials["soil"]).transform;
  }
  void BuildBuilding(Building b,Transform parent)
  {
   float y=Area.Height(b.x,b.z);Vector3 origin=new(b.x,y-.18f,b.z);Quaternion q=Quaternion.Euler(0,b.yaw,0);
   if(b.kind=="church")
   {
    var prefab=Resources.Load<GameObject>("Visuals/Church");
    if(prefab!=null)
    {
     var church=Instantiate(prefab,parent);church.name="Maria-Hemelvaartkerk — interpreted period exterior";church.transform.SetPositionAndRotation(origin,q);church.transform.localScale=Vector3.one*(b.width/43.72f);
     foreach(var t in church.GetComponentsInChildren<Transform>(true))if(t.name.IndexOf("Sacristy",StringComparison.OrdinalIgnoreCase)>=0)t.gameObject.SetActive(false);
     return;
    }
   }
   bool barn=b.kind=="barn";float h=barn?4.0f:b.width>15?4.7f:3.3f,rise=barn?2.5f:3.2f;string wall=b.kind=="farmhouse"?"plaster":"brick";
   Vector3 P(float x,float py,float z)=>origin+q*new Vector3(x,py,z);
   void Box(string m,float x,float py,float z,float sx,float sy,float sz,Color? color=null)=>Part(m).Box(P(x,py,z),new Vector3(sx,sy,sz),q,color??Color.white);
   Box("stone",0,.35f,0,b.width+.22f,.7f,b.depth+.22f);
   Box(wall,0,h/2+.35f,0,b.width,h,b.depth);
   // Full gables, projecting eaves, ridge capping and visible oak fascia.
   float top=h+.35f,w=b.width/2,d=b.depth/2;
   Part(wall).Triangle(P(-w,top,-d),P(0,top+rise,-d),P(w,top,-d),Color.white);
   Part(wall).Triangle(P(w,top,d),P(0,top+rise,d),P(-w,top,d),Color.white);
   Part("roof").Quad(P(-w-.4f,top,-d-.4f),P(-w-.4f,top,d+.4f),P(0,top+rise,d+.4f),P(0,top+rise,-d-.4f),Color.white);
   Part("roof").Quad(P(0,top+rise,-d-.4f),P(0,top+rise,d+.4f),P(w+.4f,top,d+.4f),P(w+.4f,top,-d-.4f),Color.white);
   Box("roof",0,top+rise+.035f,0,.2f,.13f,b.depth+.9f);
   foreach(float side in new[]{-1f,1f}){Box("wood",side*(w+.3f),top-.08f,0,.13f,.24f,b.depth+.85f);Part("wood").Beam(P(-w-.4f,top,side*(d+.42f)),P(0,top+rise,side*(d+.42f)),.065f,Color.white,4);Part("wood").Beam(P(0,top+rise,side*(d+.42f)),P(w+.4f,top,side*(d+.42f)),.065f,Color.white,4);}
   float doorW=barn?2.7f:1.2f,doorH=barn?3.0f:2.1f;
   Box("wood",0,doorH/2+.35f,-d-.05f,doorW,doorH,.18f,new Color(.62f,.57f,.47f));Box("stone",0,.18f,-d-.45f,doorW+.5f,.35f,.8f);
   Box("wood",0,doorH+.45f,-d-.17f,doorW+.4f,.2f,.2f);Box("iron",doorW*.29f,1.25f,-d-.17f,.08f,.18f,.05f);
   for(int side=-1;side<=1;side+=2)
   {
    int count=Mathf.Clamp(Mathf.FloorToInt(b.width/3),2,6);
    for(int i=0;i<count;i++)
    {
     float x=-w+(i+1)*b.width/(count+1);if(side==-1&&Mathf.Abs(x)<doorW/2+1)continue;
     if(barn&&i%2==0)continue;
     float z=side*(d+.065f),wy=2.05f;Box("glass",x,wy,z,.83f,1.05f,.08f);
     foreach(float edge in new[]{-.5f,.5f}){Box("wood",x+edge*.95f,wy,z+side*.035f,.075f,1.2f,.1f);Box("wood",x,wy+edge*1.16f,z+side*.035f,1,.08f,.1f);}
     Box("wood",x,wy,z+side*.055f,.045f,1.07f,.1f);Box("wood",x,wy,z+side*.055f,.85f,.045f,.1f);Box("stone",x,wy-.64f,z+side*.08f,1.16f,.14f,.28f);
     foreach(float shutter in new[]{-.73f,.73f})Box("wood",x+shutter,wy,z,.36f,1.15f,.12f,new Color(.43f,.49f,.38f));
    }
   }
   if(!barn){Box("brick",w*.46f,top+rise*.75f,d*.25f,.85f,2.4f,.8f);Box("stone",w*.46f,top+rise*.75f+1.18f,d*.25f,1.02f,.18f,.97f);Box("iron",w*.46f,top+rise*.75f+1.28f,d*.25f,.55f,.04f,.5f);}
   var col=new GameObject(b.kind+" collision");col.transform.SetParent(parent,false);col.transform.SetPositionAndRotation(P(0,h/2,0),q);col.AddComponent<BoxCollider>().size=new Vector3(b.width,h,b.depth);
   // Small farmyard furnishings add readable scale near the doors.
   Part("wood").Beam(P(w-.65f,.1f,-d-1.2f),P(w-.65f,1,-d-1.2f),.38f,new Color(.7f,.66f,.54f),12);
   for(int i=0;i<3;i++)Box("wood",-w+.8f,.18f+i*.2f,-d-1.15f,1.7f,.17f,.4f);
   if(barn)
   {
    for(int i=0;i<6;i++){float x=-w+i*b.width/5;Box("wood",x,.7f,d+2.0f,.12f,1.4f,.12f);}
    Box("wood",0,.55f,d+2,b.width,.12f,.12f);Box("wood",0,1.1f,d+2,b.width,.12f,.12f);
   }
  }
  void BuildAnimals(WorldData data)
  {
   var root=new GameObject("Grazing sheep and cattle");root.transform.SetParent(transform,false);var random=new System.Random(1775);
   foreach(var patch in data.patches)
   {
    if(patch.kind!="grass")continue;
    // One small herd per pasture, kept inside the mapped land-use polygon.
    for(int n=0;n<4;n++)
    {
     int tri=random.Next(patch.points.Length/3)*3;float a=.2f+(float)random.NextDouble()*.3f,b=.15f+(float)random.NextDouble()*.3f;
     float x=patch.points[tri].x*a+patch.points[tri+1].x*b+patch.points[tri+2].x*(1-a-b),z=patch.points[tri].z*a+patch.points[tri+1].z*b+patch.points[tri+2].z*(1-a-b);
     if(WorldVegetation.NearRoad(data,x,z,6))continue;
     var animal=new GameObject(n==0?"Grazing cattle":"Grazing sheep");animal.transform.SetParent(root.transform,false);animal.transform.SetPositionAndRotation(new Vector3(x,Area.Height(x,z),z),Quaternion.Euler(0,random.Next(360),0));BuildAnimal(animal.transform,n==0);AnimalCount++;
    }
   }
  }
  void BuildAnimal(Transform parent,bool cow)
  {
   var body=new WorldMesh();Color coat=cow?new Color(.38f,.19f,.095f):new Color(.85f,.81f,.68f),dark=new Color(.18f,.14f,.1f);float s=cow?1.5f:1;
   Vector3 V(float x,float y,float z)=>new Vector3(x,y,z)*s;
   body.Ellipsoid(V(0,.85f,0),V(.38f,.40f,.7f),Quaternion.identity,coat,16,9);
   for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2){body.Beam(V(x*.22f,.68f,z*.43f),V(x*.25f,.12f,z*.48f),.075f*s,dark,8,.045f*s);body.Ellipsoid(V(x*.25f,.08f,z*.48f),V(.07f,.08f,.12f),Quaternion.identity,dark,8,5);}
   if(!cow)for(int i=0;i<160;i++){float a=i*2.39996f,t=1-2*(i+.5f)/160,r=Mathf.Sqrt(1-t*t);Vector3 p=V(Mathf.Cos(a)*r*.375f,.85f+t*.39f,Mathf.Sin(a)*r*.69f);body.Ellipsoid(p,V(.035f,.028f,.039f),Quaternion.identity,coat,8,6);}
   body.Beam(V(0,.96f,-.6f),V(.1f,.55f,-.8f),.035f*s,dark,6);body.Object("Coat, legs and hooves",parent,materials["wool"]);
   var head=new WorldMesh();head.Ellipsoid(Vector3.zero,V(.21f,.29f,.33f),Quaternion.Euler(24,0,0),cow?coat:dark);head.Ellipsoid(V(0,-.15f,.22f),V(.18f,.14f,.18f),Quaternion.identity,new Color(.36f,.27f,.23f));
   for(int side=-1;side<=1;side+=2){head.Ellipsoid(V(side*.25f,.08f,0),V(.17f,.04f,.08f),Quaternion.Euler(0,0,side*25),coat,8,5);head.Ellipsoid(V(side*.17f,.07f,.18f),V(.028f,.033f,.03f),Quaternion.identity,Color.black,8,5);if(cow)head.Beam(V(side*.14f,.2f,-.08f),V(side*.29f,.4f,-.11f),.055f*s,new Color(.7f,.65f,.48f),7,.01f);}
   var h=head.Object("Grazing head",parent,materials["wool"]);h.transform.localPosition=V(0,.7f,.7f);h.AddComponent<GrazingAnimal>();
  }
  void Update(){if(vegetation!=null)vegetation.ShowTrees=ShowTrees;}
 }
 public class GrazingAnimal:MonoBehaviour
 {
  void Update(){transform.localRotation=Quaternion.Euler(18+Mathf.Sin(Time.time*.55f+transform.position.x)*14,Mathf.Sin(Time.time*.3f+transform.position.z)*8,0);}
 }
}
