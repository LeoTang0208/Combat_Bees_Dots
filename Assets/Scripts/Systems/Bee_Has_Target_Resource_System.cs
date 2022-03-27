using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class Bee_Has_Target_Resource_System : SystemBase
{
    private EntityCommandBufferSystem m_ECBSystem;
    protected override void OnCreate()
    {
        m_ECBSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate()
    {
        var beeParams = GetSingleton<BeeControlParams>();
        var field = GetSingleton<FieldAuthoring>();
        var resGridParams = GetSingleton<ResourceGridParams>();
        var bufferFromEntity = GetBufferFromEntity<StackHeightParams>();
        var bufferEntity = GetSingletonEntity<ResourceParams>();
        var stackHeights = bufferFromEntity[bufferEntity];
        float deltaTime = Time.fixedDeltaTime;

        var ecb = m_ECBSystem.CreateCommandBuffer();
        Entities
            .WithNone<Dead>()
            .WithNone<IsHolding>()
            .WithAll<TargetResource>()
            .WithNone<TargetBee>()
            .ForEach((Entity beeEntity, ref Velocity velocity, in Team Team, in TargetResource targetRes, in Translation pos) =>
        {
            if (HasComponent<Dead>(targetRes.res))
            {
                ecb.RemoveComponent<TargetResource>(beeEntity);
            }
            else
            {
                // resource has holder
                if (HasComponent<HolderBee>(targetRes.res))
                {
                    Entity holder = GetComponent<HolderBee>(targetRes.res).holder;
                    Team resHolderTeam = GetComponent<Team>(holder);
                    if(resHolderTeam.team != Team.team)
                    {
                        if (!HasComponent<TargetBee>(beeEntity))
                        {
                            ecb.AddComponent<TargetBee>(beeEntity, new TargetBee { bee = holder });
                        }
                    }
                    else
                    {
                        ecb.RemoveComponent<TargetResource>(beeEntity);
                    }
                }
                // resource has no holder
                else
                {
                    // Get latest buffer
                    bufferFromEntity = GetBufferFromEntity<StackHeightParams>();
                    stackHeights = bufferFromEntity[bufferEntity];

                    bool stacked = HasComponent<Stacked>(targetRes.res);
                    int gridX = GetComponent<GridX>(targetRes.res).gridX;
                    int gridY = GetComponent<GridY>(targetRes.res).gridY;
                    int stackIndex = GetComponent<StackIndex>(targetRes.res).index;
                    if (Utils.IsTopOfStack(resGridParams, stackHeights, gridX, gridY, stackIndex, stacked))
                    {
                        var delta = GetComponent<Translation>(targetRes.res).Value - pos.Value;
                        float sqrDist = delta.x * delta.x + delta.y * delta.y + delta.z * delta.z;
                        if (sqrDist > beeParams.grabDistance * beeParams.grabDistance)
                        {
                            velocity.vel += delta * (beeParams.chaseForce * deltaTime / math.sqrt(sqrDist));
                        }
                        // Get source
                        else if (stacked)
                        {
                            ecb.AddComponent<IsHolding>(beeEntity);
                            ecb.AddComponent<HolderBee>(targetRes.res, new HolderBee { holder = beeEntity });
                            ecb.RemoveComponent<Stacked>(targetRes.res);
                            // Get latest buffer
                            bufferFromEntity = GetBufferFromEntity<StackHeightParams>();
                            stackHeights = bufferFromEntity[bufferEntity];
                            Utils.UpdateStackHeights(resGridParams, stackHeights, gridX, gridY, stacked, -1);
                        }
                    }
                    else
                    {
                        ecb.RemoveComponent<TargetResource>(beeEntity);
                    }
                }
            }

            
        }).Run();
        m_ECBSystem.AddJobHandleForProducer(Dependency);
    }
}
