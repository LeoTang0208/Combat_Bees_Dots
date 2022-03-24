using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class Resource_Dead_System : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = Time.fixedDeltaTime;

        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        Entities
            .WithName("Resource_Is_Dead")
            .WithAll<StackIndex>()
            .WithAll<Dead>()
            .ForEach((Entity resEntity, ref DeathTimer deathTimer) =>
            {
                deathTimer.dTimer -= 5 * deltaTime;
                if (deathTimer.dTimer < 0f)
                {
                    ecb.DestroyEntity(resEntity);
                }
            }).Run();
        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}
