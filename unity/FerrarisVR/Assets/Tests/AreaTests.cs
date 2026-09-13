using NUnit.Framework;
using UnityEngine;

namespace Ferraris.Tests
{
    public class AreaTests
    {
        [Test] public void VegetationClearanceFollowsRoadSegmentsAndRoundedEnds()
        {
            var data=new WorldData{roads=new[]{new Road{width=5,points=new[]{new MapPoint(0,0),new MapPoint(20,0),new MapPoint(20,20)}}}};
            Assert.That(WorldVegetation.NearRoad(data,10,3.9f,4),Is.True,"Between sparse vertices");
            Assert.That(WorldVegetation.NearRoad(data,10,4.1f,4),Is.False);
            Assert.That(WorldVegetation.NearRoad(data,-3,-3,4),Is.False,"Rounded, not square, endpoint clearance");
            Assert.That(WorldVegetation.NearRoad(data,17,10,4),Is.True,"Second segment");
            Assert.That(WorldVegetation.NearRoad(data,10,2,1),Is.True,"At least the road width remains clear");
            data.roads[0].points=new[]{new MapPoint(0,0),new MapPoint(20,20)};
            Assert.That(WorldVegetation.NearRoad(data,10,10,4),Is.True,"Diagonal segment");
            data.roads[0].points=new[]{new MapPoint(0,0),new MapPoint(0,0)};
            Assert.That(WorldVegetation.NearRoad(data,1,1,4),Is.True,"Repeated vertex stays finite");
        }
        [Test] public void JoystickPressureProgressivelyIncreasesSpeedWithoutDiagonalBoost()
        {
            Assert.That(FerrarisApp.LocomotionInput(Vector2.zero),Is.EqualTo(Vector2.zero));
            Assert.That(FerrarisApp.LocomotionInput(Vector2.up*.25f).magnitude*6,Is.EqualTo(.75f).Within(.001));
            Assert.That(FerrarisApp.LocomotionInput(Vector2.up*.5f).magnitude*6,Is.EqualTo(2.1213f).Within(.001));
            Assert.That(FerrarisApp.LocomotionInput(Vector2.up).magnitude*6,Is.EqualTo(6).Within(.001));
            Assert.That(FerrarisApp.LocomotionInput(Vector2.one).magnitude,Is.EqualTo(1).Within(.001));
        }
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
            try
            {
                var world=go.AddComponent<HistoricalWorld>();world.Build(a,d);
                var terrain=world.TerrainObject.GetComponent<MeshCollider>();Assert.That(terrain,Is.Not.Null);
                Assert.That(world.TerrainObject.GetComponent<MeshRenderer>().sharedMaterial.GetTexture("_RoadMask"),Is.SameAs(Resources.Load<Texture2D>("Winksele/roads")));
                Assert.That(world.BuildingRoot,Is.Not.Null);Assert.That(world.TreeCount,Is.GreaterThan(100));Assert.That(world.AnimalCount,Is.GreaterThan(0));Assert.That(world.BuildingRoot.GetComponentInChildren<LODGroup>(),Is.Not.Null);
                var horizon=go.transform.Find("Distant countryside").GetComponent<MeshCollider>();Physics.SyncTransforms();
                for(int side=0;side<4;side++)for(int i=1;i<256;i++)
                {
                    float t=(i/256f*2-1)*a.size/2;var outward=side==0?Vector3.back:side==1?Vector3.right:side==2?Vector3.forward:Vector3.left;
                    var edge=outward*a.size/2+(side%2==0?Vector3.right:Vector3.forward)*t;
                    Assert.That(terrain.Raycast(new Ray(edge-outward*.02f+Vector3.up*200,Vector3.down),out var inner,300),Is.True);
                    Assert.That(horizon.Raycast(new Ray(edge+outward*.02f+Vector3.up*200,Vector3.down),out var outer,300),Is.True);
                    Assert.That(Mathf.Abs(inner.point.y-outer.point.y),Is.LessThan(.04f),"Horizon meets sampled terrain at "+side+" / "+i);
                }
            }
            finally{Object.DestroyImmediate(go);}
        }
    }
}
