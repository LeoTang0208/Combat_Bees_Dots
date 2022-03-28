using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class Bee_Dead_System : SystemBase
{
    protected override void OnUpdate()
    {
        var field = GetSingleton<FieldAuthoring>();
        float deltaTime = Time.fixedDeltaTime;

        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        Entities
            .WithAll<Team>()
            .WithAll<Dead>()
            .ForEach((Entity beeEntity, ref DeathTimer deathTimer, ref Velocity velocity, in Translation pos) =>
        {
            // velocity.vel.y += field.gravity * deltaTime;
            deathTimer.dTimer -= 10f * deltaTime;
            if (deathTimer.dTimer <= 0f)
            {
                ecb.DestroyEntity(beeEntity);
            }
        }).Run();
        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}
