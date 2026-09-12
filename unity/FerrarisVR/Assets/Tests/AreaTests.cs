using NUnit.Framework;
using UnityEngine;

namespace Ferraris.Tests
{
    public class AreaTests
    {
        [Test] public void ResourcesLoadAndOriginMapsToZero()
        {
            var area=JsonUtility.FromJson<AreaData>(Resources.Load<TextAsset>("Winksele/area").text);
            var p=area.GeoCoordinateToUnity(50.89795,4.64385);
            Assert.That(p.x,Is.EqualTo(0).Within(.01));Assert.That(p.z,Is.EqualTo(0).Within(.01));
            Assert.That(Resources.Load<Texture2D>("Winksele/ferraris").width,Is.GreaterThanOrEqualTo(2048));
        }
        [Test] public void RealTerrainAndHistoricalObjectsBuild()
        {
            var a=JsonUtility.FromJson<AreaData>(Resources.Load<TextAsset>("Winksele/area").text);
            var d=JsonUtility.FromJson<WorldData>(Resources.Load<TextAsset>("Winksele/world").text);
            var go=new GameObject("Smoke world");
            try{var world=go.AddComponent<HistoricalWorld>();world.Build(a,d);Assert.That(world.TerrainObject.GetComponent<MeshCollider>(),Is.Not.Null);Assert.That(world.RoadRoot,Is.Not.Null);Assert.That(world.BuildingRoot,Is.Not.Null);Assert.That(world.TreeCount,Is.GreaterThan(100));}
            finally{Object.DestroyImmediate(go);}
        }
    }
}
