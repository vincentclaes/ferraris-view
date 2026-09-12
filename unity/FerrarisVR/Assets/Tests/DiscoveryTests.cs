using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace Ferraris.Tests
{
    public class DiscoveryTests
    {
        [Test] public void EveryRenderedTypeHasOfflineLegendAwareHistory()
        {
            var content=JsonUtility.FromJson<ObjectContent>(Resources.Load<TextAsset>("Discovery/objects").text);
            var legend=JsonUtility.FromJson<LegendContent>(Resources.Load<TextAsset>("Discovery/legend").text);
            string[] keys={"house","barn","farmhouse","church","road","soil","crop","grass","orchard","tree","cow","sheep","barrel","woodpile","fence","terrain","sky"};
            Assert.That(legend.entries.Length,Is.EqualTo(150));Assert.That(legend.entries.Select(e=>e.id).Distinct().Count(),Is.EqualTo(150));
            foreach(string key in keys){var entry=content.For(key);Assert.That(entry,Is.Not.Null,key);Assert.That(entry.summary.Length,Is.GreaterThan(70));Assert.That(entry.sections.Length,Is.GreaterThanOrEqualTo(4));Assert.That(entry.sections.Last().title,Is.EqualTo("Hoe weten we dit?"));}
            Assert.That(content.For("barrel").legend,Does.Contain("Geen"));Assert.That(content.For("fence").legend,Does.Contain("niet automatisch"));Assert.That(content.For("terrain").legend,Does.Contain("Geen"));
            Assert.That(content.For("house"),Is.SameAs(content.For("barn")));Assert.That(content.For("soil"),Is.SameAs(content.For("crop")));
        }
        [Test] public void SceneInstancesAndRotatedFarmDetailsCanBePicked()
        {
            var area=JsonUtility.FromJson<AreaData>(Resources.Load<TextAsset>("Winksele/area").text);var data=JsonUtility.FromJson<WorldData>(Resources.Load<TextAsset>("Winksele/world").text);
            var go=new GameObject("Discovery verification");
            try
            {
                var world=go.AddComponent<HistoricalWorld>();world.Build(area,data);Physics.SyncTransforms();var picker=new ObjectPicker(area,data,world);
                foreach(string key in new[]{"house","barn","farmhouse","church","tree","cow","sheep","barrel","woodpile","fence"})
                {
                    bool found=false;
                    foreach(var volume in picker.Volumes.Where(v=>v.key==key))
                    {
                        var ray=new Ray(volume.centre+volume.rotation*new Vector3(0,0,-volume.size.z/2-.05f),volume.rotation*Vector3.forward);
                        Assert.That(volume.Intersect(ray,out float distance),Is.True);Assert.That(distance,Is.EqualTo(.05f).Within(.002f));
                        if(picker.Ray(ray).key==key){found=true;break;}
                    }
                    Assert.That(found,Is.True,"No selectable instance: "+key);
                }
                foreach(var b in data.buildings)Assert.That(picker.Map(b.x,b.z).key,Is.EqualTo(b.kind),"Repeated building "+b.x+","+b.z);
                foreach(var kind in new[]{"grass","soil","crop","orchard"})
                {
                    bool found=false;foreach(var patch in data.patches.Where(p=>p.kind==kind))for(int i=0;i<patch.points.Length;i+=3){var p=patch.points;float x=(p[i].x+p[i+1].x+p[i+2].x)/3,z=(p[i].z+p[i+1].z+p[i+2].z)/3;if(picker.Map(x,z).key==kind)found=true;}
                    Assert.That(found,Is.True,"No mapped land-use instance: "+kind);
                }
                Assert.That(picker.Ray(new Ray(new Vector3(0,100,0),Vector3.up)).key,Is.EqualTo("sky"));
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
    }
}
