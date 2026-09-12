using NUnit.Framework;
using UnityEngine;
namespace Ferraris.Tests
{
    public class AddressTests
    {
        [Test] public void NearestAddressChangesAndNeverLeaksPreviousResultOutsideCoverage()
        {
            var a=new CurrentAddress{street="Dorpsstraat",number="1",locality="Herent",x=0,z=0};
            var b=new CurrentAddress{street="Kerkstraat",number="",locality="Herent",x=100,z=0};
            var book=new AddressBook{addresses=new[]{a,b}};
            Assert.That(book.Nearest(1,0,1000,out float d),Is.SameAs(a));Assert.That(d,Is.EqualTo(1));
            Assert.That(book.Nearest(99,0,1000,out _),Is.SameAs(b));Assert.That(b.Label,Does.Contain("huisnummer onbekend"));
            Assert.That(book.Nearest(501,0,1000,out _),Is.Null);
            Assert.That(book.Nearest(float.NaN,0,1000,out _),Is.Null);
            Assert.That(new AddressBook().Nearest(0,0,1000,out _),Is.Null);
        }
        [Test] public void BundledOfficialSnapshotCoversTheWalkableAreaWithoutNetwork()
        {
            var book=JsonUtility.FromJson<AddressBook>(Resources.Load<TextAsset>("Discovery/addresses").text);
            Assert.That(book.addresses.Length,Is.GreaterThan(500));Assert.That(book.source,Does.StartWith("https://geo.api.vlaanderen.be/"));
            for(int x=-500;x<=500;x+=100)for(int z=-500;z<=500;z+=100){Assert.That(book.Nearest(x,z,1000,out float d),Is.Not.Null);Assert.That(d,Is.LessThan(450));}
        }
    }
}
