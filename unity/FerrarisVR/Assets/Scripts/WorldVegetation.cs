using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Ferraris
{
 public class WorldVegetation:MonoBehaviour
 {
  public bool ShowTrees=true;
  public Bounds CanopyBounds;
  public IReadOnlyList<Matrix4x4> TreeTransforms=>trees;
  Mesh branches,leaves,farLeaves,grass,crop;
  Material bark,leafMaterial,grassMaterial,cropMaterial;
  readonly List<Matrix4x4> trees=new();
  readonly List<Matrix4x4[]> nearBatches=new(),farBatches=new();
  readonly List<PlantChunk> chunks=new();
  readonly Matrix4x4[] plantBatch=new Matrix4x4[1023];
  const float CropRange=65,GrassRange=50;
  float nextCull;
  struct PlantChunk {public Vector3 centre;public Matrix4x4[] matrices;public bool crop;}
  static float R(System.Random r,float a,float b)=>a+(float)r.NextDouble()*(b-a);
  public void Build(AreaData area,WorldData data)
  {
   bark=HistoricalWorld.Surface("bark");leafMaterial=new Material(Shader.Find("Ferraris/Foliage")){enableInstancing=true};leafMaterial.SetTexture("_MainTex",HistoricalWorld.Texture("leaves_diff"));leafMaterial.SetTexture("_AlphaTex",HistoricalWorld.Texture("leaves_alpha"));leafMaterial.SetFloat("_Wind",.11f);
   var trunk=new WorldMesh();var crown=new WorldMesh();var distant=new WorldMesh();var random=new System.Random(1775);
   trunk.Beam(Vector3.zero,new Vector3(.15f,4.7f,0),.22f,Color.white,9,.065f);
   for(int limb=0;limb<12;limb++)
   {
    float angle=limb*2.39996f;Vector3 start=new(.08f,1.6f+limb*.22f,0);Vector3 end=new(Mathf.Cos(angle)*R(random,1.3f,2.5f),3.5f+limb*.17f,Mathf.Sin(angle)*R(random,1.3f,2.5f));
    trunk.Beam(start,end,.07f,Color.white,6,.012f);
    for(int twig=0;twig<6;twig++)
    {
     Vector3 p=end+new Vector3(R(random,-.8f,.8f),R(random,-.35f,.75f),R(random,-.8f,.8f));Color tint=Color.Lerp(new Color(.56f,.70f,.40f),new Color(.94f,1,.70f),(float)random.NextDouble());
     for(int plane=0;plane<2;plane++)crown.Card(p,new Vector2(.85f,1.7f),Quaternion.Euler(R(random,-65,65),R(random,0,360),R(random,-20,20)),tint,.42f);
    }
    for(int plane=0;plane<3;plane++)distant.Card(end,new Vector2(1.45f,2.7f),Quaternion.Euler(plane*60,limb*51+plane*60,15),new Color(.7f,.85f,.56f),.42f);
   }
   branches=trunk.Mesh("Branching orchard tree");leaves=crown.Mesh("Wind swept broadleaf crown");farLeaves=distant.Mesh("Distant orchard crown");
   CanopyBounds=leaves.bounds;CanopyBounds.Encapsulate(farLeaves.bounds);
   foreach(var t in data.trees)trees.Add(Matrix4x4.TRS(new Vector3(t.x,area.Height(t.x,t.z),t.z),Quaternion.Euler(0,t.x*13,0),new Vector3(t.scale,t.scale*R(random,.9f,1.2f),t.scale)));
   grassMaterial=new Material(leafMaterial);grassMaterial.SetTexture("_MainTex",HistoricalWorld.Texture("meadow_diff"));grassMaterial.SetTexture("_AlphaTex",HistoricalWorld.Texture("meadow_alpha"));grassMaterial.SetFloat("_Wind",.055f);
   cropMaterial=new Material(Shader.Find("Ferraris/Foliage")){enableInstancing=true};cropMaterial.SetFloat("_Wind",.045f);cropMaterial.SetFloat("_HeightWind",1);cropMaterial.SetFloat("_EarDetail",1);
   cropMaterial.SetVector("_FadeRange",new Vector4(CropRange*.65f,CropRange,0,0));grassMaterial.SetVector("_FadeRange",new Vector4(GrassRange*.65f,GrassRange,0,0));
   var wheat=new WorldMesh();
   for(int i=0;i<16;i++)
   {
    Vector3 root=new(R(random,-.75f,.75f),0,R(random,-.75f,.75f));float height=R(random,.85f,1.3f);
    Vector3 lean=new(R(random,-.14f,.14f),0,R(random,-.14f,.14f)),joint=root+Vector3.up*height*.55f+lean*.2f,tip=root+Vector3.up*height+lean;
    Color straw=Color.Lerp(new Color(.39f,.43f,.15f,0),new Color(.67f,.57f,.26f,0),R(random,0,1));
    wheat.Beam(root,joint,.008f,straw,3,.005f);wheat.Beam(joint,tip,.005f,straw,3,.0025f);
    Quaternion tilt=Quaternion.FromToRotation(Vector3.up,tip-joint);
    Color ear=Color.Lerp(straw,new Color(.84f,.71f,.39f),.65f);ear.a=1;
    wheat.Ellipsoid(tip+tilt*Vector3.up*.065f,new Vector3(.013f,R(random,.08f,.10f),.014f),tilt,ear,5,4);
    for(int leaf=0;leaf<2;leaf++)
    {
     float angle=R(random,0,Mathf.PI*2);Vector3 outward=new(Mathf.Cos(angle),0,Mathf.Sin(angle)),side=Vector3.Cross(Vector3.up,outward)*.016f;
     Vector3 start=Vector3.Lerp(root,joint,.55f+leaf*.4f),bend=start+outward*.14f+Vector3.up*.13f,end=start+outward*R(random,.25f,.36f)+Vector3.up*.04f;
     wheat.Quad(start-side*.25f,bend-side,bend+side,start+side*.25f,straw);wheat.Triangle(bend-side,end,bend+side,straw);
    }
   }
   var meadow=Resources.Load<GameObject>("Visuals/Meadow");
   if(meadow==null)throw new InvalidOperationException("Grasmodel ontbreekt. Bereid de lokale beeldbestanden opnieuw voor.");
   var filters=meadow.GetComponentsInChildren<MeshFilter>();
   var selected=Array.Find(filters,f=>f.name.EndsWith("_e"));if(selected==null)throw new InvalidOperationException("De benodigde grasvariant ontbreekt.");
   var placement=selected.transform.localToWorldMatrix;placement.SetColumn(3,new Vector4(0,0,0,1));
   var combine=new[]{new CombineInstance{mesh=selected.sharedMesh,transform=placement}};
   grass=new Mesh{name="Scanned meadow",indexFormat=IndexFormat.UInt32};grass.CombineMeshes(combine,true,true);var grassColors=new Color[grass.vertexCount];Array.Fill(grassColors,Color.white);grass.colors=grassColors;
   Debug.Log($"FERRARIS_MEADOW vertices={grass.vertexCount} bounds={grass.bounds.size}");
   crop=wheat.Mesh("Ripening grain ears");
   var cropBounds=crop.bounds;cropBounds.Expand(.25f);crop.bounds=cropBounds;
   Debug.Log($"FERRARIS_GRAIN vertices={crop.vertexCount} triangles={crop.triangles.Length/3}");
   // Small spatial chunks avoid drawing distant ground cover on mobile GPUs.
   var planted=new List<Patch>(data.patches);
   var tableaux=GetComponent<LivingTableaux>();
   var commons=new Patch{kind="commons",points=new[]{new MapPoint(-200,-200),new MapPoint(200,-200),new MapPoint(200,200),new MapPoint(-200,-200),new MapPoint(200,200),new MapPoint(-200,200)}};planted.Add(commons);
   foreach(var patch in planted)
   {
    if(patch.kind=="soil")continue;
    bool isCrop=patch.kind=="crop";var grid=new Dictionary<Vector2Int,List<Matrix4x4>>();
    float minX=float.MaxValue,maxX=float.MinValue,minZ=float.MaxValue,maxZ=float.MinValue;
    foreach(var p in patch.points){minX=Mathf.Min(minX,p.x);maxX=Mathf.Max(maxX,p.x);minZ=Mathf.Min(minZ,p.z);maxZ=Mathf.Max(maxZ,p.z);}
    float spacing=isCrop?2.4f:2.6f;
    for(float z=minZ;z<maxZ;z+=spacing)for(float x=minX;x<maxX;x+=spacing)
    {
     float px=x+R(random,-spacing/2,spacing/2),pz=z+R(random,-spacing/2,spacing/2);if(!Contains(patch,px,pz)||NearRoad(data,px,pz,4))continue;
     if(patch.kind=="commons"){bool mapped=false;foreach(var original in data.patches)if(Contains(original,px,pz)){mapped=true;break;}if(mapped)continue;}
     bool building=false;foreach(var b in data.buildings)if(b.Contains(px,pz)){building=true;break;}if(building)continue;
     // Leave a small working space around the illustrative activities.
     if(tableaux!=null&&tableaux.WorkingSpace(px,pz))continue;
     var key=new Vector2Int(Mathf.FloorToInt(px/32),Mathf.FloorToInt(pz/32));if(!grid.TryGetValue(key,out var list)){list=new List<Matrix4x4>();grid[key]=list;}
     list.Add(Matrix4x4.TRS(new Vector3(px,area.Height(px,pz),pz),Quaternion.Euler(0,R(random,0,360),0),Vector3.one*R(random,.8f,1.3f)));
    }
    foreach(var entry in grid)chunks.Add(new PlantChunk{centre=new Vector3((entry.Key.x+.5f)*32,0,(entry.Key.y+.5f)*32),matrices=entry.Value.ToArray(),crop=isCrop});
   }
  }
  public static bool Contains(Patch p,float x,float z)
  {
   float Cross(MapPoint a,MapPoint b)=> (x-b.x)*(a.z-b.z)-(a.x-b.x)*(z-b.z);
   for(int i=0;i<p.points.Length;i+=3){float a=Cross(p.points[i],p.points[i+1]),b=Cross(p.points[i+1],p.points[i+2]),c=Cross(p.points[i+2],p.points[i]);if(!((a<0||b<0||c<0)&&(a>0||b>0||c>0)))return true;}return false;
  }
  public static bool NearRoad(WorldData d,float x,float z,float distance)
  {
   var point=new Vector2(x,z);
   foreach(var r in d.roads)for(int i=0;i<r.points.Length;i++)
   {
    var a=r.points[Mathf.Max(0,i-1)];var b=r.points[i];float clearance=Mathf.Max(distance,r.width/2);
    if(x<Mathf.Min(a.x,b.x)-clearance||x>Mathf.Max(a.x,b.x)+clearance||z<Mathf.Min(a.z,b.z)-clearance||z>Mathf.Max(a.z,b.z)+clearance)continue;
    if(HomeSearch.SegmentDistance(point,a,b)<clearance)return true;
   }
   return false;
  }
  static void Batch(List<Matrix4x4> source,List<Matrix4x4[]> result)
  {
   result.Clear();for(int i=0;i<source.Count;i+=128)result.Add(source.GetRange(i,Mathf.Min(128,source.Count-i)).ToArray());
  }
  public static int FilterPlants(Matrix4x4[] source,Vector3 eye,float range,Matrix4x4[] result)
  {
   if(result.Length<source.Length)throw new ArgumentException("Plant buffer is smaller than its spatial chunk.");
   int count=0;float squaredRange=range*range;
   foreach(var matrix in source){float x=matrix.m03-eye.x,z=matrix.m23-eye.z;if(x*x+z*z<squaredRange)result[count++]=matrix;}
   return count;
  }
  void Update()
  {
   Camera cam=Camera.main;if(cam==null)return;Vector3 eye=cam.transform.position;
   if(Time.time>=nextCull)
   {
    nextCull=Time.time+.5f;var near=new List<Matrix4x4>();var far=new List<Matrix4x4>();foreach(var matrix in trees){Vector3 p=matrix.GetColumn(3);float d=Vector3.Distance(eye,p);if(d<95)near.Add(matrix);else if(d<650)far.Add(matrix);}Batch(near,nearBatches);Batch(far,farBatches);
   }
   if(ShowTrees)
   {
    foreach(var batch in nearBatches){Draw(branches,bark,batch,ShadowCastingMode.On);Draw(leaves,leafMaterial,batch,ShadowCastingMode.On);}
    foreach(var batch in farBatches){Draw(branches,bark,batch,ShadowCastingMode.Off);Draw(farLeaves,leafMaterial,batch,ShadowCastingMode.Off);}
   }
   // Use one centre-eye position in both eyes, so the fade has no stereo mismatch.
   cropMaterial.SetVector("_PlantEye",eye);grassMaterial.SetVector("_PlantEye",eye);
   foreach(var chunk in chunks)
   {
    float range=chunk.crop?CropRange:GrassRange;
    float x=Mathf.Max(0,Mathf.Abs(chunk.centre.x-eye.x)-16),z=Mathf.Max(0,Mathf.Abs(chunk.centre.z-eye.z)-16);
    if(x*x+z*z>=range*range)continue;
    int count=FilterPlants(chunk.matrices,eye,range,plantBatch);
    if(count>0)Draw(chunk.crop?crop:grass,chunk.crop?cropMaterial:grassMaterial,plantBatch,ShadowCastingMode.Off,count);
   }
  }
  static void Draw(Mesh mesh,Material material,Matrix4x4[] matrices,ShadowCastingMode shadows,int count=-1)=>Graphics.DrawMeshInstanced(mesh,0,material,matrices,count<0?matrices.Length:count,null,shadows,true,0,null,LightProbeUsage.Off);
 }
}
