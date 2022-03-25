using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Rendering;


//[UpdateAfter(typeof(Bee_Spawner_System))]
//[UpdateAfter(typeof(Resource_Spawner_System))]
[UpdateBefore(typeof(TransformSystemGroup))]
public class Bee_No_Target_System : SystemBase
{
    private EntityQuery Blue_Query;
    private EntityQuery Yellow_Query;
    private EntityQuery UnHeld_Res_Query;

    protected override void OnCreate()
    {
        Blue_Query = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(Team_B) },
            None = new ComponentType[] { typeof(Dead) }
        });

        Yellow_Query = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(Team_Y) },
            None = new ComponentType[] { typeof(Dead) }
        });
        
        UnHeld_Res_Query = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(StackIndex) },
            None = new ComponentType[] { typeof(Dead), typeof(HolderBee) }
        });
    }
    protected override void OnUpdate()
    {
        var beeParams = GetSingleton<BeeControlParams>();
        var field = GetSingleton<FieldAuthoring>();
        var resParams = GetSingleton<ResourceParams>();
        var resGridParams = GetSingleton<ResourceGridParams>();
        var bufferFromEntity = GetBufferFromEntity<StackHeightParams>();
        var bufferEntity = GetSingletonEntity<ResourceParams>();
        var stackHeights = bufferFromEntity[bufferEntity];
        float deltaTime = Time.fixedDeltaTime;

        NativeArray<Entity> UnHeld_Res = UnHeld_Res_Query.ToEntityArrayAsync(Allocator.TempJob, out var UnHeldResHandle);
        UnHeldResHandle.Complete();
        NativeArray<Entity> Blue = Blue_Query.ToEntityArrayAsync(Allocator.TempJob, out var Blue_Handle);
        Blue_Handle.Complete();
        NativeArray<Entity> Yellow = Yellow_Query.ToEntityArrayAsync(Allocator.TempJob, out var Yellow_Handle);
        Yellow_Handle.Complete();

        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        Entities
            .WithNone<Dead>()
            .WithNone<TargetBee>()
            .WithNone<TargetResource>()
            .WithReadOnly(UnHeld_Res)
            .ForEach((Entity beeEntity, in Team Team) =>
            {
                int Index;
                TargetBee targetBee;
                TargetResource targetRes;
                UnityEngine.Debug.Log($"Just Dandy");
                if (UnityEngine.Random.value < beeParams.aggression)
                {
                    if (Team.team == 0)
                    {
                        if (Yellow.Length > 0)
                        {
                            Index = UnityEngine.Random.Range(0, Yellow.Length);
                            targetBee.bee = Yellow[Index];
                            ecb.AddComponent<TargetBee>(beeEntity, targetBee);
                        }
                    }
                    else
                    {
                        if (Blue.Length > 0)
                        {
                            Index = UnityEngine.Random.Range(0, Blue.Length);
                            targetBee.bee = Blue[Index];
                            ecb.AddComponent<TargetBee>(beeEntity, targetBee);
                        }
                    }
                }
                else
                {
                    if (UnHeld_Res.Length > 0)
                    {
                        Index = UnityEngine.Random.Range(0, UnHeld_Res.Length);
                        targetRes.res = UnHeld_Res[Index];

                        bool stacked = HasComponent<Stacked>(targetRes.res);
                        int gridX = GetComponent<GridX>(targetRes.res).gridX;
                        int gridY = GetComponent<GridY>(targetRes.res).gridY;
                        int stackIndex = GetComponent<StackIndex>(targetRes.res).index;

                        // Get latest buffer
                        bufferFromEntity = GetBufferFromEntity<StackHeightParams>();
                        stackHeights = bufferFromEntity[bufferEntity];
                        int index = resGridParams.gridCounts.y * gridX + gridY;
                        
                        if ((!HasComponent<HolderBee>(targetRes.res)) || (stackIndex == stackHeights[index].Value - 1))
                        {
                            ecb.AddComponent<TargetResource>(beeEntity, targetRes);
                        }
                    }
                }
            }).Run();
        ecb.Playback(EntityManager);
        ecb.Dispose();

        Blue.Dispose();
        Yellow.Dispose();
        UnHeld_Res.Dispose();
    }
}
