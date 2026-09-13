using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Ferraris
{
    // Build once from the same terrain and solid buildings used by the visitor.
    public sealed class StoryNavigation : System.IDisposable
    {
        NavMeshData data;
        NavMeshDataInstance instance;
        public Vector3[] Stops { get; private set; }
        public bool Ready { get; private set; }
        public int AgentType { get; private set; }

        public StoryNavigation(HistoricalWorld world,AreaData area,WorldData geometry,StoryStop[] stops)
        {
            var sources=new List<NavMeshBuildSource>();
            var terrain=world.TerrainObject;
            sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Mesh,
                sourceObject=terrain.GetComponent<MeshFilter>().sharedMesh,transform=terrain.transform.localToWorldMatrix,area=0});
            foreach(var box in world.GetComponentsInChildren<BoxCollider>(true))
            {
                if(box.isTrigger)continue;
                sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,
                    transform=box.transform.localToWorldMatrix*Matrix4x4.Translate(box.center),size=box.size,area=1});
            }
            foreach(var collider in world.GetComponentsInChildren<MeshCollider>(true))
            {
                if(collider.isTrigger||collider.gameObject==terrain||collider.name=="Distant countryside")continue;
                sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Mesh,sourceObject=collider.sharedMesh,
                    transform=collider.transform.localToWorldMatrix,area=1});
            }
            var bounds=new Bounds(new Vector3(stops[0].x,area.Height(stops[0].x,stops[0].z),stops[0].z),Vector3.zero);
            foreach(var stop in stops)bounds.Encapsulate(new Vector3(stop.x,area.Height(stop.x,stop.z),stop.z));
            bounds.Expand(new Vector3(70,60,70));
            var settings=NavMesh.GetSettingsByIndex(0);AgentType=settings.agentTypeID;
            settings.agentRadius=.45f;settings.agentHeight=1.75f;settings.agentClimb=.35f;settings.agentSlope=45;
            data=NavMeshBuilder.BuildNavMeshData(settings,sources,bounds,Vector3.zero,Quaternion.identity);
            if(data==null){Debug.LogWarning("STORY_NAV no navigation data");return;}
            instance=NavMesh.AddNavMeshData(data);
            Stops=new Vector3[stops.Length];
            var filter=new NavMeshQueryFilter{agentTypeID=AgentType,areaMask=NavMesh.AllAreas};
            for(int i=0;i<stops.Length;i++)
            {
                var stop=stops[i];var p=new Vector3(stop.x,area.Height(stop.x,stop.z),stop.z);
                bool found=false;
                for(int radius=0;radius<=25&&!found;radius++)for(int angle=0;angle<(radius==0?1:24);angle++)
                {
                    float a=angle*Mathf.PI/12;var candidate=p+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*radius;
                    candidate.y=area.Height(candidate.x,candidate.z);
                    if(!NavMesh.SamplePosition(candidate,out var hit,1.5f,filter))continue;
                    bool inside=false;
                    foreach(var building in geometry.buildings)if(building.Contains(hit.position.x,hit.position.z)){inside=true;break;}
                    if(inside)continue;
                    if(i>0)
                    {
                        var path=new NavMeshPath();
                        if(!NavMesh.CalculatePath(Stops[i-1],hit.position,filter,path)||path.status!=NavMeshPathStatus.PathComplete)continue;
                    }
                    Stops[i]=hit.position;found=true;break;
                }
                if(!found){Debug.LogWarning($"STORY_NAV no reachable exterior at stop {i}: {p}");return;}
            }
            // Do not offer a journey whose destinations are on disconnected islands.
            for(int i=1;i<Stops.Length;i++)
            {
                var path=new NavMeshPath();
                if(!NavMesh.CalculatePath(Stops[i-1],Stops[i],filter,path)||path.status!=NavMeshPathStatus.PathComplete){Debug.LogWarning($"STORY_NAV disconnected leg {i}: {Stops[i-1]} to {Stops[i]} ({path.status})");return;}
            }
            Ready=true;
        }
        public void Dispose()
        {
            if(instance.valid)instance.Remove();
            if(data!=null){if(Application.isPlaying)Object.Destroy(data);else Object.DestroyImmediate(data);}
        }
    }
}
