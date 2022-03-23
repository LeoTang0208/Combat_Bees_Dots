using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;

public class BeeAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public int team;
    public float deathTimer;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new Team { team = this.team });
        dstManager.AddComponentData(entity, new DeathTimer { dTimer = this.deathTimer });
        float3 smPos;
        smPos.x = this.transform.position.x;
        smPos.y = this.transform.position.y;
        smPos.z = this.transform.position.z;
        dstManager.AddComponentData(entity, new SmoothPosition { smPos = smPos + new float3(1, 0, 0) * .01f });
        dstManager.AddComponentData(entity, new SmoothDirection { smDir = new float3(0, 0, 0) });
    }

}

public struct Team : IComponentData
{
    public int team;
    /* Note:
    BLUE = 0
    YELLOW = 1
    */
}

public struct SmoothPosition : IComponentData
{
    public float3 smPos;
}

public struct SmoothDirection : IComponentData
{
    public float3 smDir;
}

public struct Size : IComponentData
{
    public float value;
}






