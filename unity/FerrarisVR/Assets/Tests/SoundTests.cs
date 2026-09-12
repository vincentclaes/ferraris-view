using NUnit.Framework;
using UnityEngine;
namespace Ferraris.Tests
{
    public class SoundTests
    {
        [Test] public void LandscapeSoundHasDistanceFalloffAndAccessibleDirection()
        {
            Assert.That(LandscapeSound.Gain(4,80),Is.EqualTo(1));Assert.That(LandscapeSound.Gain(80,80),Is.Zero);Assert.That(LandscapeSound.Gain(40,80),Is.InRange(.4f,.6f));
            Assert.That(LandscapeSound.Bearing(Vector3.left),Is.EqualTo("links"));Assert.That(LandscapeSound.Bearing(Vector3.right),Is.EqualTo("rechts"));Assert.That(LandscapeSound.Bearing(Vector3.back),Is.EqualTo("achter je"));
            foreach(string clip in new[]{"bell","sheep","wood"}){var a=Resources.Load<AudioClip>("Discovery/Audio/"+clip);Assert.That(a,Is.Not.Null);Assert.That(a.length,Is.GreaterThan(.5f));Assert.That(a.channels,Is.EqualTo(1));}
        }
    }
}
