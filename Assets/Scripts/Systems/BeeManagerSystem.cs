using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Rendering;


[UpdateAfter(typeof(Bee_Spawner_System))]
[UpdateAfter(typeof(Resource_Spawner_System))]
[UpdateBefore(typeof(TransformSystemGroup))]
public class BeeManagerSystem : SystemBase
{
    private EntityQuery unHeldResQuery;

    protected override void OnCreate()
    {
        unHeldResQuery = GetEntityQuery(new EntityQueryDesc
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

        NativeList<Entity> Team_B = new NativeList<Entity>(beeParams.maxBeeCount, Allocator.TempJob);
        NativeList<Entity> Team_Y = new NativeList<Entity>(beeParams.maxBeeCount, Allocator.TempJob);

        var ecb0 = new EntityCommandBuffer(Allocator.TempJob);
        Entities
            .WithName("Bee_Get_Teams")
            .WithNone<Dead>()
            .ForEach((Entity beeEntity, in Team Team) =>
            {
                if (Team.team == 0)
                {
                    Team_B.Add(beeEntity);
                }
                else
                {
                    Team_Y.Add(beeEntity);
                }
            }).Run();
        ecb0.Playback(EntityManager);
        ecb0.Dispose();

        Entities
            .WithName("Bee_Calculate_Velocity")
            .WithAll<Team>()
            .WithNone<Dead>()
            .WithReadOnly(Team_B)
            .WithReadOnly(Team_Y)
            .ForEach((ref Velocity velocity, in Translation pos, in Team Team) =>
            {
                Vector3 rndVel_vector = UnityEngine.Random.insideUnitSphere;
                float3 rndVel;
                rndVel.x = rndVel_vector.x;
                rndVel.y = rndVel_vector.y;
                rndVel.z = rndVel_vector.z;

                velocity.vel += rndVel * beeParams.flightJitter * deltaTime;
                velocity.vel *= (1f - beeParams.damping);

                int Index;
                Entity attractiveFriend;
                Entity repellentFriend;
                if (Team.team == 0)
                {
                    Index = UnityEngine.Random.Range(0, Team_B.Length);
                    attractiveFriend = Team_B.ElementAt(Index);
                    Index = UnityEngine.Random.Range(0, Team_B.Length);
                    repellentFriend = Team_B.ElementAt(Index);
                }
                else
                {
                    Index = UnityEngine.Random.Range(0, Team_Y.Length);
                    attractiveFriend = Team_Y.ElementAt(Index);
                    Index = UnityEngine.Random.Range(0, Team_Y.Length);
                    repellentFriend = Team_Y.ElementAt(Index);
                }

                float3 delta;
                float dist;
                delta = GetComponent<Translation>(attractiveFriend).Value - pos.Value;
                dist = math.sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
                if (dist > 0f)
                {
                    velocity.vel += delta * (beeParams.teamAttraction * deltaTime / dist);
                }

                delta = GetComponent<Translation>(repellentFriend).Value - pos.Value;
                dist = math.sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
                if (dist > 0f)
                {
                    velocity.vel -= delta * (beeParams.teamRepulsion * deltaTime / dist);
                }
            }).Run();

        NativeArray<Entity> unHeldResArray = unHeldResQuery.ToEntityArrayAsync(Allocator.TempJob, out var unHeldResHandle);
        unHeldResHandle.Complete();
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        Entities
            .WithName("Bee_Has_No_TargetBee_And_TargetResource")
            .WithNone<Dead>()
            .WithNone<TargetBee>()
            .WithNone<TargetResource>()
            .WithReadOnly(Team_B)
            .WithReadOnly(Team_Y)
            .WithReadOnly(unHeldResArray)
            .ForEach((Entity beeEntity, in Team Team) =>
            {
                int Index;
                TargetBee targetBee;
                TargetResource targetRes;
                if (UnityEngine.Random.value < beeParams.aggression)
                {
                    if (Team.team == 0)
                    {
                        if (Team_Y.Length > 0)
                        {
                            Index = UnityEngine.Random.Range(0, Team_Y.Length);
                            targetBee.bee = Team_Y.ElementAt(Index);
                            ecb.AddComponent<TargetBee>(beeEntity, targetBee);
                        }
                    }
                    else
                    {
                        if (Team_B.Length > 0)
                        {
                            Index = UnityEngine.Random.Range(0, Team_B.Length);
                            targetBee.bee = Team_B.ElementAt(Index);
                            ecb.AddComponent<TargetBee>(beeEntity, targetBee);
                        }
                    }
                }
                else
                {
                    if (unHeldResArray.Length > 0)
                    {
                        Index = UnityEngine.Random.Range(0, unHeldResArray.Length);
                        targetRes.res = unHeldResArray[Index];

                        bool stacked = HasComponent<Stacked>(targetRes.res);
                        int gridX = GetComponent<GridX>(targetRes.res).gridX;
                        int gridY = GetComponent<GridY>(targetRes.res).gridY;
                        int stackIndex = GetComponent<StackIndex>(targetRes.res).index;

                        // Get latest buffer
                        bufferFromEntity = GetBufferFromEntity<StackHeightParams>();
                        stackHeights = bufferFromEntity[bufferEntity];
                        int index = resGridParams.gridCounts.y * gridX + gridY;
                        
                        if ((HasComponent<HolderBee>(targetRes.res) == false) || (stackIndex == stackHeights[index].Value - 1))
                        {
                            ecb.AddComponent<TargetResource>(beeEntity, targetRes);
                        }
                    }
                }
            }).Run();
        ecb.Playback(EntityManager);
        ecb.Dispose();

        Team_B.Dispose();
        Team_Y.Dispose();
        unHeldResArray.Dispose();
    }
}
