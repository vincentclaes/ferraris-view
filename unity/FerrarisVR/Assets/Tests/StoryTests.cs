using NUnit.Framework;
using UnityEngine;
namespace Ferraris.Tests
{
    public class StoryTests
    {
        [Test] public void WorkCyclesKeepHandsOnObjectsAndDepositTheHarvest()
        {
            var area=JsonUtility.FromJson<AreaData>(Resources.Load<TextAsset>("Winksele/area").text);
            var data=JsonUtility.FromJson<WorldData>(Resources.Load<TextAsset>("Winksele/world").text);var go=new GameObject("Work cycle verification");
            try
            {
                var tableaux=go.AddComponent<LivingTableaux>();tableaux.Build(area,data);
                foreach(var worker in go.GetComponentsInChildren<TableauResident>())
                {
                    var work=worker.Work;if(work==null)continue;
                    if(work.Activity is "fruit rapen" or "halmen verzamelen")
                    {
                        work.Sample(.34f);Assert.That(Vector3.Distance(worker.RightHand,work.Pickup),Is.LessThan(.02f),work.Activity+" reaches the actual ground object");
                        work.Sample(.47f);Assert.That(Vector3.Distance(work.Item.position,worker.RightHand),Is.LessThan(.005f));
                        Assert.That(work.Item.position.y-work.Pickup.y,Is.GreaterThan(.3f),"Harvest lifted from ground");
                        work.Sample(.74f);var held=work.Item.position;work.Sample(.751f);
                        Assert.That(Vector3.Distance(held,work.Item.position),Is.LessThan(.02f),work.Activity+" does not jump when released");
                        work.Sample(.83f);Assert.That(Vector3.Distance(work.Item.position,work.Destination),Is.LessThan(.005f));
                        if(work.Basket!=null)
                        {
                            var p=work.Basket.InverseTransformPoint(work.Item.position);
                            Assert.That(new Vector2(p.x,p.z).magnitude,Is.LessThan(.16f));Assert.That(p.y,Is.InRange(.06f,.23f),"Fruit lands inside the basket");
                        }
                        work.Sample(.99f);work.Sample(.01f);Assert.That(work.Item.gameObject.activeSelf,Is.True,"The next loop restores the object");
                    }
                    else if(work.Activity=="dorsen")
                    {
                        Quaternion start=default;float hingeMotion=0;
                        for(int i=0;i<=40;i++)
                        {
                            work.Sample(i/40f);
                            Assert.That(Vector3.Distance(worker.RightHand,work.Handle.position),Is.LessThan(.025f),"Right hand grips shaft at "+i);
                            Assert.That(Vector3.Distance(worker.LeftHand,work.Handle.TransformPoint(Vector3.up*.18f)),Is.LessThan(.025f),"Left hand grips shaft at "+i);
                            Assert.That(Vector3.Distance(work.Beater.position,work.Handle.TransformPoint(Vector3.up*1.1f)),Is.LessThan(.001f));
                            Assert.That(Vector3.Distance(work.BeaterTip,work.Beater.position),Is.EqualTo(.65f).Within(.001f));
                            Assert.That(work.BeaterTip.y,Is.GreaterThanOrEqualTo(area.Height(work.BeaterTip.x,work.BeaterTip.z)+.10f),"No underground strike");
                            if(i==0)start=work.Beater.localRotation;else hingeMotion=Mathf.Max(hingeMotion,Quaternion.Angle(start,work.Beater.localRotation));
                        }
                        Assert.That(hingeMotion,Is.GreaterThan(45),"Knuppel hinges independently of shaft");
                        work.Sample(0);var handle=work.Handle.position;var tip=work.BeaterTip;work.Sample(1);
                        Assert.That(Vector3.Distance(handle,work.Handle.position),Is.LessThan(.001f),"Shaft loop has no jump");
                        Assert.That(Vector3.Distance(tip,work.BeaterTip),Is.LessThan(.001f),"Beater loop has no jump");
                    }
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
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
            var font=Resources.Load<Font>("Fonts/LiberationSans-Regular");Assert.That(font,Is.Not.Null);
            foreach(char c in "éë—–•1775")Assert.That(font.HasCharacter(c),Is.True,"Offline Dutch glyph: "+c);
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
