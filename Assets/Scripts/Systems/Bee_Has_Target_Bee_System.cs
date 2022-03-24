using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class Bee_Has_Target_Bee_System : SystemBase
{
    protected override void OnUpdate()
    {
        var beeParams = GetSingleton<BeeControlParams>();
        float deltaTime = Time.fixedDeltaTime;

        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        Entities
            .WithName("Bee_Has_Target_Bee")
            .WithNone<Dead>()
            .WithAll<Velocity>()
            .WithAll<TargetBee>()
            .WithNone<TargetResource>()
            .ForEach((Entity beeEntity, in Team Team, in TargetBee targetBee, in Translation pos) =>
            {
                Velocity velocity = GetComponent<Velocity>(beeEntity);

                if (HasComponent<Dead>(targetBee.bee))
                {
                    ecb.RemoveComponent<TargetBee>(beeEntity);
                }
                else
                {
                    float3 delta = GetComponent<Translation>(targetBee.bee).Value - pos.Value;
                    float sqrDist = delta.x * delta.x + delta.y * delta.y + delta.z * delta.z;

                    if (sqrDist > beeParams.attackDistance * beeParams.attackDistance)
                    {
                        velocity.vel += delta * (beeParams.chaseForce * deltaTime / math.sqrt(sqrDist));
                        ecb.SetComponent<Velocity>(beeEntity, new Velocity { vel = velocity.vel });
                    }
                    else
                    {
                        if (sqrDist > 0)
                        {
                            ecb.AddComponent<IsAttacking>(beeEntity);

                            velocity.vel += delta * (beeParams.attackForce * deltaTime / math.sqrt(sqrDist));
                            ecb.SetComponent<Velocity>(beeEntity, new Velocity { vel = velocity.vel });

                            if (sqrDist < beeParams.hitDistance * beeParams.hitDistance)
                            {
                                // ToDo, spawn blood particle
                                ecb.AddComponent<Dead>(targetBee.bee);
                                Velocity targetVelocity = GetComponent<Velocity>(targetBee.bee);
                                ecb.SetComponent<Velocity>(targetBee.bee, new Velocity { vel = targetVelocity.vel * .5f });
                                ecb.RemoveComponent<TargetBee>(beeEntity);
                            }
                        }
                    }
                }
            }).Run();
        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}
