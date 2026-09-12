using NUnit.Framework;
using UnityEngine;
namespace Ferraris.Tests
{
    public class HomeTests
    {
        [Test] public void AddressSearchSupportsAmbiguousResultsAndHistoricalLandUse()
        {
            var a=new CurrentAddress{street="Dalenstraat",number="1",locality="Herent"};var b=new CurrentAddress{street="Dalenstraat",number="2",locality="Herent",x=600};
            var book=new AddressBook{addresses=new[]{a,b}};
            Assert.That(HomeSearch.Search(book,"dalenstraat").Length,Is.EqualTo(2));Assert.That(HomeSearch.Search(book,"dalenstraat 2")[0],Is.SameAs(b));
            Assert.That(HomeSearch.Search(book,"onbekend"),Is.Empty);Assert.That(HomeSearch.Search(null,"test"),Is.Empty);
            Assert.That(HomeSearch.Covered(a,1000),Is.True);Assert.That(HomeSearch.Covered(b,1000),Is.False);
            var data=JsonUtility.FromJson<WorldData>(Resources.Load<TextAsset>("Winksele/world").text);
            var church=System.Array.Find(data.buildings,b=>b.kind=="church");Assert.That(HomeSearch.LandUse(data,church.x,church.z),Is.EqualTo("kerk"));
            Assert.That(HomeSearch.LandUse(data,5000,5000),Does.Contain("niet geclassificeerd"));
            var orchard=System.Array.Find(data.patches,p=>p.kind=="orchard");var p=orchard.points;
            Assert.That(HomeSearch.LandUse(data,(p[0].x+p[1].x+p[2].x)/3,(p[0].z+p[1].z+p[2].z)/3),Is.EqualTo("boomgaard"));
        }
    }
}
