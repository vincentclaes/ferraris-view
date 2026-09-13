using System;
using System.Collections.Generic;
using UnityEngine;
namespace Ferraris
{
    public class PickVolume
    {
        public string key;public Vector3 centre,size;public Quaternion rotation;public bool worldOnly;
        public bool Intersect(Ray ray,out float distance)
        {
            Quaternion inverse=Quaternion.Inverse(rotation);
            return new Bounds(Vector3.zero,size).IntersectRay(new Ray(inverse*(ray.origin-centre),inverse*ray.direction),out distance);
        }
        public bool Contains(float x,float z)
        {
            var p=Quaternion.Inverse(rotation)*(new Vector3(x,centre.y,z)-centre);return Mathf.Abs(p.x)<=size.x/2&&Mathf.Abs(p.z)<=size.z/2;
        }
    }
    public class PickedObject { public string key;public Vector3 point;public float radius;public PickVolume volume; }
    public class ObjectPicker
    {
        public readonly List<PickVolume> Volumes=new();
        readonly AreaData area;readonly WorldData data;
        public ObjectPicker(AreaData area,WorldData data,HistoricalWorld world)
        {
            this.area=area;this.data=data;
            foreach(var b in data.buildings)
            {
                float y=area.Height(b.x,b.z)-.18f;Quaternion q=Quaternion.Euler(0,b.yaw,0);Vector3 origin=new(b.x,y,b.z);
                void Add(string key,Vector3 local,Vector3 size,bool only=false)=>Volumes.Add(new PickVolume{key=key,centre=origin+q*local,size=size,rotation=q,worldOnly=only});
                if(b.kind=="church")
                {
                    // Include the actual visible landmark envelope, including its tower.
                    var bounds=new Bounds(new Vector3(b.x,y+13,b.z),new Vector3(b.width,27,b.depth));
                    foreach(var lod in world.GetComponentsInChildren<LODGroup>(true))foreach(var r in lod.GetLODs()[0].renderers)if(r!=null)bounds.Encapsulate(r.bounds);
                    Volumes.Add(new PickVolume{key="church",centre=bounds.center,size=bounds.size,rotation=Quaternion.identity});continue;
                }
                var form=HistoricalWorld.RuralForm(b);q=form.rotation;
                float height=form.height+.35f+form.rise+1f;
                Add(b.kind,new Vector3(0,height/2,0),new Vector3(form.length+.6f,height,form.span+.7f));
                Add("barrel",new Vector3(form.length/2-.65f,.48f,-form.span/2-.9f),new Vector3(.65f,.8f,.65f),true);
                Add("woodpile",new Vector3(-form.length/2+.8f,.38f,-form.span/2-.85f),new Vector3(1.4f,.6f,.4f),true);
                if(b.kind=="barn")Add("fence",new Vector3(0,.7f,form.span/2+2),new Vector3(form.length+.12f,1.4f,.18f),true);
            }
            var vegetation=world.GetComponent<WorldVegetation>();
            foreach(var matrix in vegetation.TreeTransforms)
            {
                Volumes.Add(new PickVolume{key="tree",centre=matrix.MultiplyPoint3x4(Vector3.up*2.35f),size=Vector3.Scale(new Vector3(.8f,4.7f,.8f),matrix.lossyScale),rotation=matrix.rotation,worldOnly=true});
                Volumes.Add(new PickVolume{key="tree",centre=matrix.MultiplyPoint3x4(vegetation.CanopyBounds.center),size=Vector3.Scale(vegetation.CanopyBounds.size,matrix.lossyScale),rotation=matrix.rotation,worldOnly=true});
            }
            foreach(var animal in world.GetComponentsInChildren<GrazingAnimal>(true))
            {
                var parent=animal.transform.parent;bool cow=parent.name=="Grazing cattle";float s=cow?1.5f:1;
                Volumes.Add(new PickVolume{key=cow?"cow":"sheep",centre=parent.position+Vector3.up*.75f*s,size=new Vector3(.9f,1.5f,2.1f)*s,rotation=parent.rotation,worldOnly=true});
            }
        }
        public PickedObject Map(float x,float z)
        {
            // Map positions use the reviewed geometry, not oversized world selection envelopes.
            foreach(var b in data.buildings)
            {
                var p=Quaternion.Euler(0,-b.yaw,0)*new Vector3(x-b.x,0,z-b.z);
                if(Mathf.Abs(p.x)<=b.width/2&&Mathf.Abs(p.z)<=b.depth/2)return Result(b.kind,new Vector3(x,area.Height(x,z),z),Mathf.Max(b.width,b.depth)/2);
            }
            foreach(var road in data.roads)for(int i=1;i<road.points.Length;i++)if(HomeSearch.SegmentDistance(new Vector2(x,z),road.points[i-1],road.points[i])<=road.width/2)return Result("road",new Vector3(x,area.Height(x,z),z),road.width);
            foreach(var patch in data.patches)if(WorldVegetation.Contains(patch,x,z))return Result(patch.kind,new Vector3(x,area.Height(x,z),z),7);
            return Result("terrain",new Vector3(x,area.Height(x,z),z),5);
        }
        public PickedObject Ray(Ray ray)
        {
            float nearest=1800;PickedObject selected=null;
            // Terrain and horizon are exact mesh hits. Existing building colliders
            // are superseded by semantic envelopes including roof and facade details.
            foreach(var hit in Physics.RaycastAll(ray,1800))if(hit.collider.gameObject.name is "DHMV terrain" or "Distant countryside")
                if(hit.distance<nearest){nearest=hit.distance;selected=Map(hit.point.x,hit.point.z);selected.point=hit.point;}
            foreach(var volume in Volumes)if(volume.Intersect(ray,out float distance)&&distance>=0&&distance<nearest)
            {nearest=distance;selected=Result(volume.key,ray.GetPoint(distance),Mathf.Max(volume.size.x,volume.size.z)/2);selected.volume=volume;}
            return selected??Result("sky",ray.origin+ray.direction*100,0);
        }
        static PickedObject Result(string key,Vector3 point,float radius)=>new(){key=key,point=point,radius=Mathf.Clamp(radius,1,18)};
    }
    [Serializable] public class HistorySection { public string title,text; }
    [Serializable] public class ObjectHistory { public string[] keys;public string name,summary,legend;public HistorySection[] sections; }
    [Serializable] public class ObjectContent
    {
        public ObjectHistory[] entries;
        public ObjectHistory For(string key)=>Array.Find(entries,e=>Array.IndexOf(e.keys,key)>=0);
    }
    [Serializable] public class LegendEntry { public string id,label,source,status;public string[] types; }
    [Serializable] public class LegendContent { public string source,sourceBundle;public LegendEntry[] entries; }
}
