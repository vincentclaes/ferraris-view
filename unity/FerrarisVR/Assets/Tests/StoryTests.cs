using NUnit.Framework;
using UnityEngine;
namespace Ferraris.Tests
{
    public class StoryTests
    {
        [Test] public void TableauxKeepBuildingsAndApproachesClearAndResidentsMove()
        {
            var area=JsonUtility.FromJson<AreaData>(Resources.Load<TextAsset>("Winksele/area").text);
            var data=JsonUtility.FromJson<WorldData>(Resources.Load<TextAsset>("Winksele/world").text);
            var go=new GameObject("Tableau verification");
            try
            {
                var tableaux=go.AddComponent<LivingTableaux>();tableaux.Build(area,data);Physics.SyncTransforms();
                Assert.That(tableaux.Sites.Count,Is.EqualTo(5));
                for(int i=0;i<tableaux.Sites.Count;i++)
                {
                    var site=tableaux.Sites[i];var p=site.position;
                    Assert.That(LivingTableaux.Clear(data,p.x,p.z,3),Is.True,site.name);
                    Assert.That(LivingTableaux.Clear(data,p.x,p.z-5.5f,.8f),Is.True,"Visitor approach: "+site.name);
                    Assert.That(tableaux.Content.stops[i].x,Is.EqualTo(p.x));Assert.That(tableaux.Content.stops[i].z,Is.EqualTo(p.z));
                    foreach(var resident in site.GetComponentsInChildren<TableauResident>())
                    {
                        resident.Pose(0);var before=resident.Torso.localRotation;float motion=0;
                        for(int t=1;t<=3;t++){resident.Pose(t);motion=Mathf.Max(motion,Quaternion.Angle(before,resident.Torso.localRotation));}
                        Assert.That(motion,Is.GreaterThan(.05f),"Visible animation");
                        var centre=resident.transform.position+Vector3.up*.85f;
                        Assert.That(Physics.Raycast(centre+Vector3.back,Vector3.forward,out var hit,2),Is.True);
                        Assert.That(hit.collider.GetComponentInParent<TableauSite>(),Is.Not.Null,"Resident selectable");
                    }
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test] public void StoryCanPauseResumeAndReachAnEnding()
        {
            var content=JsonUtility.FromJson<StoryContent>(Resources.Load<TextAsset>("Discovery/day").text);
            Assert.That(content.stops.Length,Is.GreaterThanOrEqualTo(4));Assert.That(content.introduction,Does.Contain("verzonnen"));
            var state=new StoryProgress();state.Resume();Assert.That(state.Advance(false,5),Is.False);
            Assert.That(state.Advance(true,5),Is.True);state.Pause();Assert.That(state.Advance(true,5),Is.False);Assert.That(state.Step,Is.EqualTo(1));
            state.Resume();for(int i=1;i<content.stops.Length;i++)Assert.That(state.Advance(true,content.stops.Length),Is.True);
            Assert.That(state.Complete(content.stops.Length),Is.True);Assert.That(state.Advance(true,content.stops.Length),Is.False);
            foreach(var stop in content.stops){Assert.That(Mathf.Abs(stop.x),Is.LessThan(500));Assert.That(Mathf.Abs(stop.z),Is.LessThan(500));Assert.That(stop.text.Length,Is.GreaterThan(80));}
        }
    }
}
