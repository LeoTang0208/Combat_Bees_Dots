using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class Bee_Has_Target_Bee_System : SystemBase
{
    private EntityCommandBufferSystem m_ECBSystem;
    protected override void OnCreate()
    {
        m_ECBSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate()
    {
        var beeParams = GetSingleton<BeeControlParams>();
        float deltaTime = Time.fixedDeltaTime;

        var ecb = m_ECBSystem.CreateCommandBuffer();
        Entities
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
                            ecb.AddComponent<Blood_Particle>(targetBee.bee);
                            /*
                            var something_new = ecb.CreateEntity();
                            ecb.AddComponent<Translation>(something_new, GetComponent<Translation>(targetBee.bee));
                            ecb.AddComponent<Blood_Particle>(something_new);
                            */

                            ecb.AddComponent<Dead>(targetBee.bee);
                            //Velocity targetVelocity = GetComponent<Velocity>(targetBee.bee);
                            //ecb.SetComponent<Velocity>(targetBee.bee, new Velocity { vel = targetVelocity.vel * .5f });
                            ecb.RemoveComponent<TargetBee>(beeEntity);
                        }
                    }
                }
            }
        }).Schedule();
        m_ECBSystem.AddJobHandleForProducer(Dependency);
    }
}
