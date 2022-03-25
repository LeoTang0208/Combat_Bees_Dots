using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Rendering;

public class Bee_Local2World : SystemBase
{
    protected override void OnUpdate()
    {
        var beeParams = GetSingleton<BeeControlParams>();
        float deltaTime = Time.fixedDeltaTime;

        Entities
            .WithAll<Team>()
            .WithNone<Dead>()
            .ForEach((
                Entity beeEntity, 
                ref LocalToWorld localToWorld, 
                ref URPMaterialPropertyBaseColor baseColor, 
                in Velocity velocity, 
                in Size size, 
                in SmoothDirection smoothDir, 
                in Translation translation, 
                in DeathTimer deathTimer) =>
        {
            float3 scale = new float3(size.value, size.value, size.value);

            float stretch = math.max(1f, math.length(velocity.vel) * beeParams.speedStretch);
            scale.z *= stretch;
            scale.x /= (stretch - 1f) / 5f + 1f;
            scale.y /= (stretch - 1f) / 5f + 1f;


            quaternion rotation = quaternion.identity;
            if(!smoothDir.smDir.Equals(float3.zero))
            {
                rotation = quaternion.LookRotation(smoothDir.smDir, math.up());
            }

            localToWorld.Value = float4x4.TRS(translation.Value, rotation, scale);
        }).ScheduleParallel();

        Entities
            .WithAll<Team>()
            .WithAll<Dead>()
            .ForEach((
                Entity beeEntity, 
                ref LocalToWorld localToWorld, 
                ref URPMaterialPropertyBaseColor baseColor, 
                in Velocity velocity, 
                in Size size, 
                in SmoothDirection smoothDir, 
                in Translation translation, 
                in DeathTimer deathTimer) =>
        {
            float3 scale = new float3(size.value, size.value, size.value);
            baseColor.Value *= .75f;
            scale *= math.sqrt(deathTimer.dTimer);

            quaternion rotation = quaternion.identity;
            if(!smoothDir.smDir.Equals(float3.zero))
            {
                rotation = quaternion.LookRotation(smoothDir.smDir, math.up());
            }

            localToWorld.Value = float4x4.TRS(translation.Value, rotation, scale);
        }).ScheduleParallel();
    }
}
