using NUnit.Framework;
using UnityEngine;
namespace Ferraris.Tests
{
    public class StoryTests
    {
        [Test] public void GuideRouteConnectsAllStopsOutsideSolidBuildings()
        {
            var area=JsonUtility.FromJson<AreaData>(Resources.Load<TextAsset>("Winksele/area").text);
            var data=JsonUtility.FromJson<WorldData>(Resources.Load<TextAsset>("Winksele/world").text);
            var content=JsonUtility.FromJson<StoryContent>(Resources.Load<TextAsset>("Discovery/day").text);
            var go=new GameObject("Story route verification");
            try
            {
                var world=go.AddComponent<HistoricalWorld>();world.Build(area,data);
                using var route=new StoryNavigation(world,area,content.stops);
                Assert.That(route.Ready,Is.True,"Every guide destination needs a complete path");
                foreach(var point in route.Stops)
                    foreach(var box in world.GetComponentsInChildren<BoxCollider>())
                        Assert.That(box.bounds.Contains(point+Vector3.up*.8f),Is.False,"Guide stop inside a building");
            }
            finally{Object.DestroyImmediate(go);}
        }
        [Test] public void EveryStopHasDutchDialogueAQuestionAndHistoricalContext()
        {
            var content=JsonUtility.FromJson<StoryContent>(Resources.Load<TextAsset>("Discovery/day").text);
            Assert.That(content.stops.Length,Is.GreaterThanOrEqualTo(4));Assert.That(content.introduction,Does.Contain("verzonnen"));
            foreach(var stop in content.stops){Assert.That(Mathf.Abs(stop.x),Is.LessThan(500));Assert.That(Mathf.Abs(stop.z),Is.LessThan(500));Assert.That(stop.text.Length,Is.GreaterThan(80));Assert.That(stop.dialogue,Is.Not.Empty);Assert.That(stop.question,Is.Not.Empty);Assert.That(stop.answer,Is.Not.Empty);Assert.That(stop.follow,Is.Not.Empty);}
        }
    }
}
