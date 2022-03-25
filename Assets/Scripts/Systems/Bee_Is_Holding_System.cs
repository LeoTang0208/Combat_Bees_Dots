using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class Bee_Is_Holding_System : SystemBase
{
    protected override void OnUpdate()
    {
        var beeParams = GetSingleton<BeeControlParams>();
        var field = GetSingleton<FieldAuthoring>();
        float deltaTime = Time.fixedDeltaTime;

        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        Dependency = Entities
            .WithNone<Dead>()
            .WithAll<IsHolding>()
            .WithAll<TargetResource>()
            .WithNone<TargetBee>()
            .ForEach((Entity beeEntity, ref Velocity velocity, in Team Team, in TargetResource targetRes, in Translation pos) => 
        {
            int t = Team.team;
            float3 targetPos = new float3(-field.size.x * .45f + field.size.x * .9f * t, 0f, pos.Value.z);
            float3 delta = targetPos - pos.Value;
            float dist = math.sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);

            if (dist > 0)
            {
                velocity.vel += delta * (beeParams.carryForce * deltaTime / dist);
                if (dist < 1f)
                {
                    ecb.RemoveComponent<HolderBee>(targetRes.res);
                    ecb.RemoveComponent<TargetResource>(beeEntity);
                    ecb.RemoveComponent<IsHolding>(beeEntity);
                }
            }
        }).Schedule(Dependency);
        Dependency.Complete();
        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}
