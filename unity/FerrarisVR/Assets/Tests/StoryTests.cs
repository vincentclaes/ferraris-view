using NUnit.Framework;
using UnityEngine;
namespace Ferraris.Tests
{
    public class StoryTests
    {
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
