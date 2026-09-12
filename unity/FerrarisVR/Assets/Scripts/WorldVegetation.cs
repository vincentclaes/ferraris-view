using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Ferraris
{
 public class WorldVegetation:MonoBehaviour
 {
  public bool ShowTrees=true;
  Mesh branches,leaves,farLeaves,grass,crop;
  Material bark,leafMaterial,grassMaterial,cropMaterial;
  readonly List<Matrix4x4> trees=new();
  readonly List<Matrix4x4[]> nearBatches=new(),farBatches=new();
  readonly List<PlantChunk> chunks=new();
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
   foreach(var t in data.trees)trees.Add(Matrix4x4.TRS(new Vector3(t.x,area.Height(t.x,t.z),t.z),Quaternion.Euler(0,t.x*13,0),new Vector3(t.scale,t.scale*R(random,.9f,1.2f),t.scale)));
   grassMaterial=new Material(leafMaterial);grassMaterial.SetTexture("_MainTex",HistoricalWorld.Texture("meadow_diff"));grassMaterial.SetTexture("_AlphaTex",HistoricalWorld.Texture("meadow_alpha"));grassMaterial.SetFloat("_Wind",.055f);cropMaterial=HistoricalWorld.Surface(null);
   var wheat=new WorldMesh();
   for(int i=0;i<16;i++)
   {
    float x=R(random,-.6f,.6f),z=R(random,-.6f,.6f);
    Vector3 stem=new(x,0,z),tip=new(x+.04f,.72f+R(random,0,.25f),z);wheat.Beam(stem,tip,.013f,new Color(.6f,.56f,.23f),3,.006f);wheat.Ellipsoid(tip,new Vector3(.034f,.11f,.035f),Quaternion.identity,new Color(.76f,.65f,.33f),5,4);
   }
   var meadow=Resources.Load<GameObject>("Visuals/Meadow");
   if(meadow==null)throw new InvalidOperationException("Meadow asset missing; run pipeline/fetch_visual_assets.py");
   var filters=meadow.GetComponentsInChildren<MeshFilter>();
   var selected=Array.Find(filters,f=>f.name.EndsWith("_e"));if(selected==null)throw new InvalidOperationException("Scanned meadow variant missing");
   var placement=selected.transform.localToWorldMatrix;placement.SetColumn(3,new Vector4(0,0,0,1));
   var combine=new[]{new CombineInstance{mesh=selected.sharedMesh,transform=placement}};
   grass=new Mesh{name="Scanned meadow",indexFormat=IndexFormat.UInt32};grass.CombineMeshes(combine,true,true);var grassColors=new Color[grass.vertexCount];Array.Fill(grassColors,Color.white);grass.colors=grassColors;
   Debug.Log($"FERRARIS_MEADOW vertices={grass.vertexCount} bounds={grass.bounds.size}");
   crop=wheat.Mesh("Ripening grain ears");
   // Small spatial chunks avoid drawing distant ground cover on mobile GPUs.
   var planted=new List<Patch>(data.patches);
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
     float px=x+R(random,-.7f,.7f),pz=z+R(random,-.7f,.7f);if(!Contains(patch,px,pz)||NearRoad(data,px,pz,4))continue;
     if(patch.kind=="commons"){bool mapped=false;foreach(var original in data.patches)if(Contains(original,px,pz)){mapped=true;break;}if(mapped)continue;}
     bool building=false;foreach(var b in data.buildings)if(Mathf.Abs(px-b.x)<b.width*.6f+2&&Mathf.Abs(pz-b.z)<b.depth*.6f+2){building=true;break;}if(building)continue;
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
   foreach(var r in d.roads)foreach(var p in r.points)if(Mathf.Abs(p.x-x)<distance&&Mathf.Abs(p.z-z)<distance)return true;return false;
  }
  static void Batch(List<Matrix4x4> source,List<Matrix4x4[]> result)
  {
   result.Clear();for(int i=0;i<source.Count;i+=128)result.Add(source.GetRange(i,Mathf.Min(128,source.Count-i)).ToArray());
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
   foreach(var chunk in chunks){Vector3 p=chunk.centre;p.y=eye.y;float range=chunk.crop?65:50;if((p-eye).sqrMagnitude<range*range)Draw(chunk.crop?crop:grass,chunk.crop?cropMaterial:grassMaterial,chunk.matrices,ShadowCastingMode.Off);}
  }
  static void Draw(Mesh mesh,Material material,Matrix4x4[] matrices,ShadowCastingMode shadows)=>Graphics.DrawMeshInstanced(mesh,0,material,matrices,matrices.Length,null,shadows,true,0,null,LightProbeUsage.Off);
 }
}
