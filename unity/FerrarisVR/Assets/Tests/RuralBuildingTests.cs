using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace Ferraris.Tests
{
    public class RuralBuildingTests
    {
        [TestCase(16,8,31,"house")]
        [TestCase(8,16,-17,"farmhouse")]
        [TestCase(18,9,70,"barn")]
        public void RoofAndPickerFollowTheTracedFootprint(float width,float depth,float yaw,string kind)
        {
            var area=JsonUtility.FromJson<AreaData>(Resources.Load<TextAsset>("Winksele/area").text);
            var b=new Building{x=70,z=-60,width=width,depth=depth,yaw=yaw,kind=kind};
            var data=new WorldData{buildings=new[]{b},roads=Array.Empty<Road>(),patches=Array.Empty<Patch>(),trees=Array.Empty<Tree>()};
            var go=new GameObject("Rural form regression");
            try
            {
                // This isolated building fixture has no geographical story parcels.
                var world=go.AddComponent<HistoricalWorld>();world.Build(area,data,includeTableaux:false);
                var collider=world.BuildingRoot.GetComponentInChildren<BoxCollider>();
                var inverse=Quaternion.Inverse(Quaternion.Euler(0,yaw,0));
                foreach(float x in new[]{-.5f,.5f})foreach(float z in new[]{-.5f,.5f})
                {
                    var corner=collider.transform.TransformPoint(new Vector3(collider.size.x*x,0,collider.size.z*z));
                    var local=inverse*(corner-new Vector3(b.x,corner.y,b.z));
                    Assert.That(Mathf.Abs(local.x),Is.EqualTo(width/2).Within(.001));
                    Assert.That(Mathf.Abs(local.z),Is.EqualTo(depth/2).Within(.001));
                }
                var roof=world.BuildingRoot.Find("roof architectural details").GetComponent<MeshFilter>().sharedMesh;
                var v=roof.vertices;
                foreach(int start in new[]{0,4})
                {
                    var normal=Vector3.Cross(v[start+1]-v[start],v[start+2]-v[start]).normalized;
                    Assert.That(normal.y,Is.GreaterThan(.5f),"Both visible roof slopes face the sky");
                }
                var ridge=inverse*(v[2]-v[1]);
                Assert.That(Mathf.Abs(width>depth?ridge.x:ridge.z),Is.GreaterThan(15));
                Assert.That(Mathf.Abs(width>depth?ridge.z:ridge.x),Is.LessThan(.001),"Ridge follows the long traced side");
                var picker=new ObjectPicker(area,data,world);
                var envelope=picker.Volumes.First(p=>p.key==kind);
                foreach(var point in v)
                {
                    var local=Quaternion.Inverse(envelope.rotation)*(point-envelope.centre);
                    Assert.That(Mathf.Abs(local.y),Is.LessThanOrEqualTo(envelope.size.y/2+.01f),"Roof is selectable up to the ridge");
                }
                Assert.That(picker.Map(b.x,b.z).key,Is.EqualTo(kind));
                Assert.That(picker.Volumes.Any(p=>p.key=="barrel"),Is.True);
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
    }
}
