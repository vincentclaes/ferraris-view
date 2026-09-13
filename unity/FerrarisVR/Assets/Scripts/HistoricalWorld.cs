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
  public Transform BuildingRoot;
  public bool ShowTrees=true;
  public int TreeCount{get;private set;}
  public int AnimalCount{get;private set;}
  public LivingTableaux Tableaux{get;private set;}
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
  public void Build(AreaData area,WorldData data,bool includeTableaux=true)
  {
   Area=area;TreeCount=data.trees.Length;
   foreach(string name in new[]{"brick","plaster","roof","wood","soil","bark"})materials[name]=Surface(name);
   materials["stone"]=Surface("plaster",new Color(.55f,.53f,.46f));materials["glass"]=Surface(null,new Color(.055f,.085f,.075f));materials["wool"]=Surface(null);materials["iron"]=Surface(null,new Color(.12f,.13f,.12f));
   ConfigureLight();BuildTerrain(data.roads.Length>0);
   var buildings=new GameObject("Detailed Ferraris buildings");buildings.transform.SetParent(transform,false);BuildingRoot=buildings.transform;
   foreach(var b in data.buildings)BuildBuilding(b,buildings.transform);
   foreach(var pair in parts)if(pair.Value.VertexCount>0)pair.Value.Object(pair.Key+" architectural details",buildings.transform,materials[pair.Key]);
   if(includeTableaux){Tableaux=gameObject.AddComponent<LivingTableaux>();Tableaux.Build(area,data);}
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
  void BuildTerrain(bool hasRoads)
  {
   var material=new Material(Shader.Find("Ferraris/TerrainSurface"));material.SetTexture("_MainTex",Resources.Load<Texture2D>("Winksele/landcover"));material.SetTexture("_Grass",Texture("grass_diff"));material.SetTexture("_Soil",Texture("soil_diff"));material.SetTexture("_BumpMap",Texture("grass_normal"));material.SetTexture("_SoilNormal",Texture("soil_normal"));material.SetFloat("_AreaSize",Area.size);
   var roadMask=hasRoads?Resources.Load<Texture2D>("Winksele/roads"):Texture2D.blackTexture;
   if(roadMask==null)throw new InvalidOperationException("Missing road mask; run pipeline/export_world.py");
   material.SetTexture("_RoadMask",roadMask);
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
  public static (float length,float span,float height,float rise,Quaternion rotation) RuralForm(Building b)
  {
   float length=Mathf.Max(b.width,b.depth),span=Mathf.Min(b.width,b.depth);
   bool barn=b.kind=="barn";float h=barn?3.3f:b.kind=="farmhouse"?4.1f:2.7f;
   return (length,span,h,span*.5f*Mathf.Tan((barn?47:43)*Mathf.Deg2Rad),Quaternion.Euler(0,b.yaw+(b.depth>b.width?90:0),0));
  }
  void BuildMappedBuilding(Building b,Transform parent)
  {
   // The plan follows the reviewed symbol; elevations and openings are illustrative.
   float ground=Area.Height(b.x,b.z)-.3f,top=ground+3.05f;
   var inverse=Quaternion.Euler(0,-b.yaw,0);var collision=new WorldMesh();
   Vector3 Bottom(MapPoint p)=>new(p.x,Mathf.Min(ground,Area.Height(p.x,p.z)-.3f),p.z);
   Vector3 Roof(MapPoint p)
   {
    float across=(inverse*new Vector3(p.x-b.x,0,p.z-b.z)).z;
    return new Vector3(p.x,top+Mathf.Max(0,b.depth/2-Mathf.Abs(across))*.93f,p.z);
   }
   float longest=0;int doorEdge=0;
   for(int i=0;i<b.footprint.Length;i++)
   {
    var a=b.footprint[i];var c=b.footprint[(i+1)%b.footprint.Length];
    float length=Vector2.Distance(new Vector2(a.x,a.z),new Vector2(c.x,c.z));
    if(length>longest){longest=length;doorEdge=i;}
   }
   Color oak=new(.56f,.47f,.35f),clay=new(.91f,.66f,.45f);
   for(int i=0;i<b.footprint.Length;i++)
   {
    var a=b.footprint[i];var c=b.footprint[(i+1)%b.footprint.Length];
    var lowA=Bottom(a);var lowC=Bottom(c);var highA=Roof(a);var highC=Roof(c);
    collision.Quad(lowA,highA,highC,lowC,Color.white);
    var along=new Vector3(c.x-a.x,0,c.z-a.z);float length=along.magnitude;
    if(length<.001f)continue; // Rounded ridge intersections can coincide with a contour vertex.
    along/=length;
    var outward=Vector3.Cross(Vector3.up,along);var rotation=Quaternion.LookRotation(-outward);
    Vector3 P(float x,float y,float depth=0)=>new Vector3(a.x,y,a.z)+along*x+outward*depth;
    float Base(float x)=>Mathf.Lerp(lowA.y,lowC.y,x/length);
    float Cap(float x)=>Mathf.Lerp(highA.y,highC.y,x/length);
    void Wall(float x0,float x1,float y0,float y1,float y2,float y3)
    {
     if(x1-x0<.001f)return;
     // Shared wall coordinates keep horizontal brick courses across apertures/gables.
     Part("brick").Quad(P(x0,y0),P(x0,y1),P(x1,y2),P(x1,y3),Color.white,
      new Vector2(x0,y0-ground),new Vector2(x0,y1-ground),new Vector2(x1,y2-ground),new Vector2(x1,y3-ground));
    }
    void Box(string material,float x,float y,float depth,Vector3 size,Color color)=>Part(material).Box(P(x,y,depth),size,rotation,color);
    var openings=new List<(float x,float width,float bottom,float height,bool door)>();
    float doorWidth=Mathf.Min(1.1f,longest*.5f);
    if(i==doorEdge)openings.Add((length/2,doorWidth,ground+.3f,2f,true));
    int count=Mathf.FloorToInt(length/4);
    for(int j=0;j<count;j++)
    {
     float x=(j+.5f)*length/count;
     if(i==doorEdge&&Mathf.Abs(x-length/2)<doorWidth/2+1.1f)continue;
     openings.Add((x,.8f,top-1.72f,1.1f,false));
    }
    openings.Sort((a,c)=>a.x.CompareTo(c.x));float cursor=0;
    foreach(var o in openings)
    {
     float left=o.x-o.width/2,right=o.x+o.width/2,upper=o.bottom+o.height;
     Wall(cursor,left,Base(cursor),Cap(cursor),Cap(left),Base(left));
     Wall(left,right,Base(left),o.bottom,o.bottom,Base(right));
     Wall(left,right,upper,Cap(left),Cap(right),upper);
     // Real aperture with a dark recessed back and masonry reveals, not a panel on brick.
     float recess=o.door?.12f:.18f;
     Part("brick").Quad(P(left,o.bottom),P(left,upper),P(left,upper,-recess),P(left,o.bottom,-recess),Color.white);
     Part("brick").Quad(P(right,o.bottom,-recess),P(right,upper,-recess),P(right,upper),P(right,o.bottom),Color.white);
     Part("brick").Quad(P(left,upper),P(right,upper),P(right,upper,-recess),P(left,upper,-recess),Color.white);
     Box(o.door?"wood":"glass",o.x,o.bottom+o.height/2,-recess,new Vector3(o.width,o.height,.035f),o.door?oak:Color.white);
     foreach(float edge in new[]{left,right})Box("wood",edge,o.bottom+o.height/2,-.045f,new Vector3(.075f,o.height+.09f,.09f),oak);
     Box("wood",o.x,upper+.055f,-.025f,new Vector3(o.width+.22f,.14f,.15f),oak);
     Box("stone",o.x,o.bottom-.055f,.04f,new Vector3(o.width+.2f,.11f,.28f),Color.white);
     if(o.door)
     {
      for(int board=1;board<6;board++)Box("wood",left+o.width*board/6,o.bottom+o.height/2,-recess+.022f,new Vector3(.014f,o.height,.012f),oak*.65f);
      foreach(float y in new[]{o.bottom+.45f,upper-.4f})Box("iron",left+.2f,y,-recess+.03f,new Vector3(.36f,.04f,.025f),Color.white);
      Box("iron",right-.16f,o.bottom+1,-recess+.05f,new Vector3(.06f,.14f,.065f),Color.white);
     }
     else
     {
      Box("wood",o.x,o.bottom+o.height/2,-.12f,new Vector3(.045f,o.height,.065f),oak);
      Box("wood",o.x,o.bottom+o.height*.58f,-.12f,new Vector3(o.width,.04f,.065f),oak);
      foreach(float side in new[]{-1f,1f})
      {
       float x=o.x+side*(o.width/2+.25f);
       Box("wood",x,o.bottom+o.height/2,.07f,new Vector3(.37f,o.height,.065f),oak);
       foreach(float y in new[]{o.bottom+.2f,upper-.2f})Box("iron",x,y,.11f,new Vector3(.32f,.035f,.02f),Color.white);
      }
     }
     cursor=right;
    }
    Wall(cursor,length,Base(cursor),Cap(cursor),Cap(length),Base(length));
    // A low masonry footing and thin timber roof edge clarify the building's silhouette.
    Part("stone").Quad(P(0,Base(0),.015f),P(0,ground+.27f,.015f),P(length,ground+.27f,.015f),P(length,Base(length),.015f),new Color(.82f,.79f,.72f));
    Part("wood").Beam(highA+Vector3.down*.04f,highC+Vector3.down*.04f,.055f,oak,4);
   }
   var ridges=new HashSet<string>();
   Vector2 RoofUV(Vector3 p)
   {
    var local=inverse*(p-new Vector3(b.x,0,b.z));
    return new Vector2(local.x,local.z*Mathf.Sqrt(1+.93f*.93f));
   }
   for(int i=0;i<b.roof.Length;i+=3)
   {
    var a=Roof(b.roof[i]);var c=Roof(b.roof[i+1]);var d=Roof(b.roof[i+2]);
    if(Vector3.Cross(c-a,d-a).y<0)(c,d)=(d,c);
    Part("roof").Triangle(a,c,d,clay,RoofUV(a),RoofUV(c),RoofUV(d));collision.Triangle(a,c,d,Color.white);
    var points=new[]{a,c,d};
    for(int edge=0;edge<3;edge++)
    {
     var p=points[edge];var q=points[(edge+1)%3];var u=RoofUV(p);var v=RoofUV(q);
     if(Mathf.Abs(u.y)>.002f||Mathf.Abs(v.y)>.002f)continue;
     string key=Mathf.RoundToInt(Mathf.Min(u.x,v.x)*1000)+":"+Mathf.RoundToInt(Mathf.Max(u.x,v.x)*1000);
     if(ridges.Add(key))Part("roof").Beam(p+Vector3.up*.035f,q+Vector3.up*.035f,.09f,clay,8);
    }
   }
   var go=new GameObject("Mapped building: "+b.id);go.transform.SetParent(parent,false);
   go.AddComponent<MeshCollider>().sharedMesh=collision.Mesh(b.id+" footprint collision");
  }
  void BuildBuilding(Building b,Transform parent)
  {
   if(b.kind=="building"){BuildMappedBuilding(b,parent);return;}
   float y=Area.Height(b.x,b.z);Vector3 origin=new(b.x,y-.18f,b.z);Quaternion q=Quaternion.Euler(0,b.yaw,0);
   if(b.kind=="church")
   {
    var prefab=Resources.Load<GameObject>("Visuals/Church");
    if(prefab!=null)
    {
     var church=Instantiate(prefab,parent);church.name="Maria-Hemelvaartkerk — interpreted period exterior";
     foreach(var t in church.GetComponentsInChildren<Transform>(true))if(t.name.IndexOf("Sacristy",StringComparison.OrdinalIgnoreCase)>=0)t.gameObject.SetActive(false);
     // Fit the visible plan envelope to the reviewed nave symbol; the icon does
     // not supply a measured floor plan or the historic tower height.
     Bounds bounds=default;bool first=true;
     foreach(var renderer in church.GetComponentsInChildren<MeshRenderer>())
     {
      if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds);
     }
     float sx=b.width/bounds.size.x,sz=b.depth/bounds.size.z;
     church.transform.localScale=new Vector3(sx,sx,sz);
     church.transform.SetPositionAndRotation(origin+q*new Vector3(-bounds.center.x*sx,-bounds.min.y*sx,-bounds.center.z*sz),q);
     return;
    }
   }
   // Ferraris supplies the footprint, not a measured facade. Regional analogues:
   // Winksele Dalenstraat 2 (1762), erfgoedobject 41898; see docs/period-buildings.md.
   // Keep the traced rectangle unchanged while putting the ridge along its long axis.
   var form=RuralForm(b);float length=form.length,span=form.span,h=form.height,rise=form.rise;q=form.rotation;
   bool barn=b.kind=="barn",farm=b.kind=="farmhouse";
   float top=h+.35f,w=length/2,d=span/2;
   string wall=farm?"brick":barn?"brick":"plaster";
   Color clay=new(.87f,.52f,.32f),oak=new(.51f,.43f,.32f);
   Vector3 P(float x,float py,float z)=>origin+q*new Vector3(x,py,z);
   void Box(string m,float x,float py,float z,float sx,float sy,float sz,Color? color=null)=>Part(m).Box(P(x,py,z),new Vector3(sx,sy,sz),q,color??Color.white);
   Box("stone",0,.2f,0,length+.12f,.4f,span+.12f);
   Box(wall,0,h/2+.35f,0,length,h,span);
   // Side gables; ridge and eaves run along the long street/yard facade.
   Part(wall).Triangle(P(-w,top,-d),P(-w,top,d),P(-w,top+rise,0),Color.white);
   Part(wall).Triangle(P(w,top,d),P(w,top,-d),P(w,top+rise,0),Color.white);
   Part("roof").Quad(P(-w-.2f,top,-d-.32f),P(-w-.2f,top+rise,0),P(w+.2f,top+rise,0),P(w+.2f,top,-d-.32f),clay);
   Part("roof").Quad(P(w+.2f,top,d+.32f),P(w+.2f,top+rise,0),P(-w-.2f,top+rise,0),P(-w-.2f,top,d+.32f),clay);
   Box("roof",0,top+rise+.035f,0,length+.5f,.15f,.24f,clay);
   foreach(float side in new[]{-1f,1f})
   {
    Box("wood",0,top-.04f,side*(d+.22f),length+.45f,.17f,.13f,oak);
    Part("wood").Beam(P(side*(w+.22f),top,-d-.32f),P(side*(w+.22f),top+rise,0),.06f,oak,4);
    Part("wood").Beam(P(side*(w+.22f),top+rise,0),P(side*(w+.22f),top,d+.32f),.06f,oak,4);
    if(farm)
    {
     // Restrained masonry shoulders and iron ties; no invented date inscription.
     foreach(float edge in new[]{-1f,1f})Box("stone",side*w,top,edge*(d-.08f),.32f,.27f,.48f);
     Box("iron",side*(w+.035f),top+.7f,0,.07f,.7f,.07f);
     Box("iron",side*(w+.035f),top+.7f,0,.07f,.07f,.45f);
    }
   }
   float doorW=barn?2.8f:1.05f,doorH=barn?2.9f:1.95f,doorX=barn?0:-length*.1f;
   Box("wood",doorX,doorH/2+.35f,-d-.05f,doorW,doorH,.18f,oak);
   Box("stone",doorX,.18f,-d-.3f,doorW+.28f,.22f,.5f);
   string frame=farm?"stone":"wood";
   Box(frame,doorX,doorH+.42f,-d-.17f,doorW+.3f,.18f,.2f);
   foreach(float side in new[]{-1f,1f})Box(frame,doorX+side*(doorW/2+.09f),doorH/2+.35f,-d-.15f,.14f,doorH+.08f,.16f);
   Box("iron",doorX+doorW*.29f,1.15f,-d-.17f,.06f,.15f,.05f);
   if(barn)
   {
    Box("wood",0,doorH/2+.35f,-d-.16f,.09f,doorH,.12f,oak);
    foreach(float side in new[]{-1f,1f})
    {
     Part("wood").Beam(P(side*.1f,.55f,-d-.19f),P(side*(doorW/2-.12f),doorH+.12f,-d-.19f),.055f,oak,4);
     // Ventilation slits instead of domestic glazed windows and shutters.
     Box("glass",side*length*.34f,2.45f,-d-.04f,.18f,.7f,.06f);
    }
   }
   else for(int side=-1;side<=1;side+=2)
   {
    int count=Mathf.Clamp(Mathf.FloorToInt(length/3),3,7);
    for(int i=0;i<count;i++)
    {
     float x=-w+(i+1)*length/(count+1);if(side==-1&&Mathf.Abs(x-doorX)<doorW/2+.85f)continue;
     float z=side*(d+.065f),wy=1.8f;Box("glass",x,wy,z,.76f,.96f,.08f);
     foreach(float edge in new[]{-.5f,.5f}){Box(frame,x+edge*.9f,wy,z+side*.035f,.09f,1.15f,.1f);Box(frame,x,wy+edge*1.08f,z+side*.035f,.99f,.09f,.1f);}
     Box("wood",x,wy,z+side*.055f,.045f,.98f,.1f);Box("wood",x,wy,z+side*.055f,.78f,.045f,.1f);
     Box("stone",x,wy-.62f,z+side*.08f,1.1f,.13f,.25f);
     foreach(float shutter in new[]{-.69f,.69f})Box("wood",x+shutter,wy,z,.35f,1.03f,.1f,oak);
     if(farm){Box("glass",x,3.68f,z,.62f,.39f,.07f);Box("wood",x,3.68f,z+side*.05f,.04f,.39f,.08f);}
    }
   }
   if(!barn)
   {
    // Chimney begins in the roof and clears the ridge, even on a wide footprint.
    float chimneyX=w*.52f,chimneyBase=top+rise-.65f;
    Box("brick",chimneyX,chimneyBase+.75f,0,.72f,1.5f,.68f);
    Box("brick",chimneyX,chimneyBase+1.5f,0,.9f,.15f,.85f);
    Box("iron",chimneyX,chimneyBase+1.58f,0,.44f,.025f,.4f);
   }
   var col=new GameObject(b.kind+" collision");col.transform.SetParent(parent,false);col.transform.SetPositionAndRotation(P(0,h/2+.35f,0),q);col.AddComponent<BoxCollider>().size=new Vector3(length,h,span);
   Part("wood").Beam(P(w-.65f,.1f,-d-.9f),P(w-.65f,.85f,-d-.9f),.3f,oak,10);
   for(int i=0;i<3;i++)Box("wood",-w+.8f,.18f+i*.2f,-d-.85f,1.4f,.17f,.35f,oak);
   if(barn)
   {
    for(int i=0;i<6;i++){float x=-w+i*length/5;Box("wood",x,.7f,d+2,.12f,1.4f,.12f,oak);}
    Box("wood",0,.55f,d+2,length,.12f,.12f,oak);Box("wood",0,1.1f,d+2,length,.12f,.12f,oak);
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
