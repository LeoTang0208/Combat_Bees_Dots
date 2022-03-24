using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

//[UpdateAfter(typeof(Bee_Is_Holding_System))]
public class Bee_Has_Target_Resource_System : SystemBase
{
    protected override void OnUpdate()
    {
        var beeParams = GetSingleton<BeeControlParams>();
        var field = GetSingleton<FieldAuthoring>();
        var resGridParams = GetSingleton<ResourceGridParams>();
        var bufferFromEntity = GetBufferFromEntity<StackHeightParams>();
        var bufferEntity = GetSingletonEntity<ResourceParams>();
        var stackHeights = bufferFromEntity[bufferEntity];
        float deltaTime = Time.fixedDeltaTime;

        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        Entities
            .WithName("Bee_Has_Target_Resource")
            .WithNone<Dead>()
            .WithNone<IsHolding>()
            .WithAll<TargetResource>()
            .WithNone<TargetBee>()
            .ForEach((Entity beeEntity, ref Velocity velocity, in Team Team, in TargetResource targetRes, in Translation pos) =>
            {
                // resource has no holder
                if (!HasComponent<HolderBee>(targetRes.res))
                {
                    // Get latest buffer
                    bufferFromEntity = GetBufferFromEntity<StackHeightParams>();
                    stackHeights = bufferFromEntity[bufferEntity];

                    // resource dead or not top of the stack
                    if (HasComponent<Dead>(targetRes.res))
                    {
                        ecb.RemoveComponent<TargetResource>(beeEntity);
                    }
                    else
                    {
                        bool stacked = HasComponent<Stacked>(targetRes.res);
                        int gridX = GetComponent<GridX>(targetRes.res).gridX;
                        int gridY = GetComponent<GridY>(targetRes.res).gridY;
                        int stackIndex = GetComponent<StackIndex>(targetRes.res).index;
                        if (Utils.IsTopOfStack(resGridParams, stackHeights, gridX, gridY, stackIndex, stacked) == false)
                        {
                            ecb.RemoveComponent<TargetResource>(beeEntity);
                        }
                        else
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
                                UnityEngine.Debug.Log($"ADD IS HOLDING");
                                ecb.AddComponent<IsHolding>(beeEntity);
                                ecb.AddComponent<HolderBee>(targetRes.res, new HolderBee { holder = beeEntity });
                                ecb.RemoveComponent<Stacked>(targetRes.res);
                                // Get latest buffer
                                bufferFromEntity = GetBufferFromEntity<StackHeightParams>();
                                stackHeights = bufferFromEntity[bufferEntity];
                                Utils.UpdateStackHeights(resGridParams, stackHeights, gridX, gridY, stacked, -1);
                            }
                        }
                    }
                }
                // resource has holder
                else
                {
                    Entity holder = GetComponent<HolderBee>(targetRes.res).holder;
                    Team resHolderTeam = GetComponent<Team>(holder);
                    if(resHolderTeam.team != Team.team)
                    {
                        if (HasComponent<TargetBee>(beeEntity) == false)
                        {
                            ecb.AddComponent<TargetBee>(beeEntity, new TargetBee { bee = holder });
                        }
                    }
                    else
                    {
                        ecb.RemoveComponent<TargetResource>(beeEntity);
                    }
                }
            }).Run();
        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}
