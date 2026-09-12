using NUnit.Framework;
using UnityEngine;
namespace Ferraris.Tests
{
    public class PlaceNameTests
    {
        [Test] public void CuratedNamesHaveSourcesUncertaintyAndSupportedLocations()
        {
            var data=JsonUtility.FromJson<PlaceNameContent>(Resources.Load<TextAsset>("Discovery/names").text);
            Assert.That(data.entries.Length,Is.GreaterThanOrEqualTo(3));
            foreach(var n in data.entries){Assert.That(n.name,Is.Not.Empty);Assert.That(n.earlier,Is.Not.Empty);Assert.That(n.certainty,Is.Not.Empty);Assert.That(n.evidence,Does.Contain("https://"));Assert.That(Mathf.Abs(n.x),Is.LessThan(499));Assert.That(Mathf.Abs(n.z),Is.LessThan(499));}
            Assert.That(data.entries[0].certainty,Does.Contain("Waarschijnlijke"));Assert.That(data.entries[1].explanation,Does.Contain("1820"));Assert.That(data.entries[2].certainty,Does.Contain("onbekend"));
        }
    }
}
