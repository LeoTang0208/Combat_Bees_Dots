using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Rendering;


[UpdateAfter(typeof(BeeSpawnerSystem))]
[UpdateAfter(typeof(ResourceSpawnerSystem))]
[UpdateBefore(typeof(TransformSystemGroup))]
public class BeeManagerSystem : SystemBase
{
    private EntityCommandBufferSystem m_ECB;
    private EntityQuery Blue_Team_Query;
    private EntityQuery unHeldResQuery;

    protected override void OnCreate()
    {
        Blue_Team_Query = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(Team) }
        });

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
        var random = new Unity.Mathematics.Random(1234);

        NativeList<Entity> Team_B = new NativeList<Entity>(beeParams.maxBeeCount, Allocator.TempJob);
        NativeList<Entity> Team_Y = new NativeList<Entity>(beeParams.maxBeeCount, Allocator.TempJob);

        var ecb0 = new EntityCommandBuffer(Allocator.TempJob);
        Entities
            .WithName("Bee_Get_Teams")
            .WithNone<Dead>()
            .ForEach((Entity beeEntity, in Team Team) =>
            {
                if(HasComponent<IsHoldingResource>(beeEntity))
                {
                    ecb0.RemoveComponent<IsHoldingResource>(beeEntity);
                }

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
                // Random move
                var rndVel = random.NextFloat3();
                velocity.vel += rndVel * beeParams.flightJitter * deltaTime;
                velocity.vel *= (1f - beeParams.damping);

                // Get friend
                int Index;
                Entity attractiveFriend;
                Entity repellentFriend;
                if (Team.team == 0)
                {
                    Index = random.NextInt(0, Team_B.Length);
                    attractiveFriend = Team_B.ElementAt(Index);
                    Index = random.NextInt(0, Team_B.Length);
                    repellentFriend = Team_B.ElementAt(Index);
                }
                else
                {
                    Index = random.NextInt(0, Team_Y.Length);
                    attractiveFriend = Team_Y.ElementAt(Index);
                    Index = random.NextInt(0, Team_Y.Length);
                    repellentFriend = Team_Y.ElementAt(Index);
                }

                // Move towards attractive
                float3 delta;
                float dist;
                delta = GetComponent<Translation>(attractiveFriend).Value - pos.Value;
                dist = math.sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
                if (dist > 0f)
                {
                    velocity.vel += delta * (beeParams.teamAttraction * deltaTime / dist);
                }

                // Move away from repellent
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
            .WithDisposeOnCompletion(Team_B)
            .WithReadOnly(Team_Y)
            .WithDisposeOnCompletion(Team_Y)
            .WithReadOnly(unHeldResArray)
            .WithDisposeOnCompletion(unHeldResArray)
            .ForEach((Entity beeEntity, in Team Team) =>
            {
                int Index;
                TargetBee targetBee;
                TargetResource targetRes;
                if (random.NextFloat() < beeParams.aggression)
                {
                    if (Team.team == 0)
                    {
                        if (Team_Y.Length > 0)
                        {
                            Index = random.NextInt(0, Team_Y.Length);
                            targetBee.bee = Team_Y.ElementAt(Index);
                            ecb.AddComponent<TargetBee>(beeEntity, targetBee);
                        }
                    }
                    else
                    {
                        if (Team_B.Length > 0)
                        {
                            Index = random.NextInt(0, Team_B.Length);
                            targetBee.bee = Team_B.ElementAt(Index);
                            ecb.AddComponent<TargetBee>(beeEntity, targetBee);
                        }
                    }
                }
                else
                {
                    if (unHeldResArray.Length > 0)
                    {
                        Index = random.NextInt(0, unHeldResArray.Length);
                        targetRes.res = unHeldResArray[Index];

                        bool stacked = HasComponent<Stacked>(targetRes.res);
                        int gridX = GetComponent<GridX>(targetRes.res).gridX;
                        int gridY = GetComponent<GridY>(targetRes.res).gridY;
                        int stackIndex = GetComponent<StackIndex>(targetRes.res).index;

                        // Get latest buffer
                        bufferFromEntity = GetBufferFromEntity<StackHeightParams>();
                        stackHeights = bufferFromEntity[bufferEntity];
                        int index = resGridParams.gridCounts.y * gridX + gridY;
                        
                        if ((HasComponent<HolderBee>(targetRes.res) == false) ||
                            (stackIndex == stackHeights[index].Value - 1))
                        {
                            ecb.AddComponent<TargetResource>(beeEntity, targetRes);
                        }
                    }
                }
            }).Run();
        ecb.Playback(EntityManager);
        ecb.Dispose();

        unHeldResArray.Dispose();
        Team_B.Dispose();
        Team_Y.Dispose();

        var ecb1 = new EntityCommandBuffer(Allocator.TempJob);
        Entities
            .WithName("Bee_Has_Target_Bee")
            .WithNone<Dead>()
            .WithAll<Velocity>()
            .ForEach((Entity beeEntity, in Team Team, in TargetBee targetBee, in Translation pos) =>
            {
                Velocity velocity = GetComponent<Velocity>(beeEntity);

                // target bee is dead
                if (HasComponent<Dead>(targetBee.bee))
                {
                    ecb1.RemoveComponent<TargetBee>(beeEntity);
                }
                else
                {
                    float3 delta = GetComponent<Translation>(targetBee.bee).Value - pos.Value;
                    float sqrDist = delta.x * delta.x + delta.y * delta.y + delta.z * delta.z;

                    if (sqrDist > beeParams.attackDistance * beeParams.attackDistance)
                    {
                        velocity.vel += delta * (beeParams.chaseForce * deltaTime / math.sqrt(sqrDist));
                        ecb1.SetComponent<Velocity>(beeEntity, new Velocity { vel = velocity.vel });
                    }
                    else
                    {
                        if (sqrDist > 0)
                        {
                            ecb1.AddComponent<IsAttacking>(beeEntity);

                            velocity.vel += delta * (beeParams.attackForce * deltaTime / math.sqrt(sqrDist));
                            ecb1.SetComponent<Velocity>(beeEntity, new Velocity { vel = velocity.vel });

                            if (sqrDist < beeParams.hitDistance * beeParams.hitDistance)
                            {
                                // ToDo, spawn blood particle
                                ecb1.AddComponent<Dead>(targetBee.bee);
                                Velocity targetVelocity = GetComponent<Velocity>(targetBee.bee);
                                ecb1.SetComponent<Velocity>(targetBee.bee, new Velocity { vel = targetVelocity.vel * .5f });
                                ecb1.RemoveComponent<TargetBee>(beeEntity);
                            }
                        }
                    }
                }
            }).Run();
        ecb1.Playback(EntityManager);
        ecb1.Dispose();

        var ecb2 = new EntityCommandBuffer(Allocator.TempJob);
        Entities
            .WithName("Bee_Has_Target_Resource")
            .WithNone<Dead>()
            .WithNone<TargetBee>()
            .ForEach((Entity beeEntity, ref Velocity velocity, in Team Team, in TargetResource targetRes, in Translation pos) =>
            {
                // resource has no holder
                if (HasComponent<HolderBee>(targetRes.res) == false)
                {
                    // Get latest buffer
                    bufferFromEntity = GetBufferFromEntity<StackHeightParams>();
                    stackHeights = bufferFromEntity[bufferEntity];

                    // resource dead or not top of the stack
                    if (HasComponent<Dead>(targetRes.res))
                    {
                        ecb2.RemoveComponent<TargetResource>(beeEntity);
                    }
                    else
                    {
                        bool dead = HasComponent<Dead>(targetRes.res);
                        bool stacked = HasComponent<Stacked>(targetRes.res);
                        int gridX = GetComponent<GridX>(targetRes.res).gridX;
                        int gridY = GetComponent<GridY>(targetRes.res).gridY;
                        int stackIndex = GetComponent<StackIndex>(targetRes.res).index;
                        if (Utils.IsTopOfStack(resGridParams, stackHeights, gridX, gridY, stackIndex, stacked) == false)
                        {
                            ecb2.RemoveComponent<TargetResource>(beeEntity);
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
                                ecb2.AddComponent<HolderBee>(targetRes.res, new HolderBee { holder = beeEntity });
                                ecb2.RemoveComponent<Stacked>(targetRes.res);

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
                    if (holder == beeEntity)
                    {
                        int team = (int)Team.team;
                        float3 targetPos = new float3(-field.size.x * .45f + field.size.x * .9f * team, 0f, pos.Value.z);
                        float3 delta = targetPos - pos.Value;
                        float dist = math.sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);

                        // make sure dist is non zero
                        if (dist > 0)
                        {
                            velocity.vel += delta * (beeParams.carryForce * deltaTime / dist);
                            if (dist < 1f)
                            {
                                ecb2.RemoveComponent<HolderBee>(targetRes.res);
                                ecb2.RemoveComponent<TargetResource>(beeEntity);
                            }
                            else
                            {
                                ecb2.AddComponent<IsHoldingResource>(beeEntity);
                            }
                        }
                    }
                    else
                    {
                        Team resHolderTeam = GetComponent<Team>(holder);
                        if(resHolderTeam.team != Team.team)
                        {
                            if (HasComponent<TargetBee>(beeEntity) == false)
                            {
                                ecb2.AddComponent<TargetBee>(beeEntity, new TargetBee { bee = holder });
                            }
                        }
                        else
                        {
                            ecb2.RemoveComponent<TargetResource>(beeEntity);
                        }
                    }
                }
            }).Run();
        ecb2.Playback(EntityManager);
        ecb2.Dispose();

        var ecb3 = new EntityCommandBuffer(Allocator.TempJob);
        Entities
            .WithName("Bee_Is_Dead")
            .WithAll<Team>()
            .WithAll<Dead>()
            .ForEach((Entity beeEntity, ref DeathTimer deathTimer, ref Velocity velocity, in Translation pos) =>
            {
                if (random.NextFloat() < (deathTimer.dTimer - .5f) * .5f)
                {
                    // ToDo, Blood Particle
                }

                velocity.vel.y += field.gravity * deltaTime;
                deathTimer.dTimer -= 10f * deltaTime;
                if (deathTimer.dTimer < 0f)
                {
                    ecb3.DestroyEntity(beeEntity);
                }

            }).Run();
        ecb3.Playback(EntityManager);
        ecb3.Dispose();

        Entities
            .WithName("Bee_Calculate_Position")
            .WithAll<Team>()
            .WithNone<Dead>()
            .ForEach((ref Translation pos, in Velocity velocity) =>
            {
                pos.Value += deltaTime * velocity.vel;
            }).ScheduleParallel();

        Entities
            .WithName("Bee_Adjust_Move")
            .WithAll<Team>()
            .ForEach((Entity beeEntity, ref Velocity velocity, ref Translation pos) =>
            {
                float size = 0f;
                if(HasComponent<Size>(beeEntity))
                {
                    size = GetComponent<Size>(beeEntity).value;
                }

                if (math.abs(pos.Value.x) > field.size.x * .5f)
                {
                    pos.Value.x = field.size.x * .5f * math.sign(pos.Value.x);
                    velocity.vel.x *= -.5f;
                    velocity.vel.y *= .8f;
                    velocity.vel.z *= .8f;
                }

                if (math.abs(pos.Value.z) > field.size.z * .5f)
                {
                    pos.Value.z = field.size.z * .5f * math.sign(pos.Value.z);
                    velocity.vel.z *= -.5f;
                    velocity.vel.x *= .8f;
                    velocity.vel.y *= .8f;
                }

                float resModifier = 0f;
                if (HasComponent<IsHoldingResource>(beeEntity))
                {
                    resModifier = resParams.resourceSize;
                }
                if (math.abs(pos.Value.y) > field.size.y * .5f - resModifier)
                {
                    pos.Value.y = (field.size.y * .5f - resModifier) * math.sign(pos.Value.y);
                    velocity.vel.y *= -.5f;
                    velocity.vel.x *= .8f;
                    velocity.vel.z *= .8f;
                }
                
            }).ScheduleParallel();

        Entities
            .WithName("Bee_Smooth_Direction")
            .WithAll<Team>()
            .WithNone<Dead>()
            .ForEach((
                Entity beeEntity, 
                ref SmoothPosition smoothPos, 
                ref SmoothDirection smoothDir, 
                in Velocity velocity, in Translation pos) =>
            {
                float3 oldSmPos = smoothPos.smPos;
                if (HasComponent<IsAttacking>(beeEntity) == false)
                {
                    smoothPos.smPos = math.lerp(smoothPos.smPos, pos.Value, deltaTime * beeParams.rotationStiffness);
                }
                else 
                {
                    smoothPos.smPos = pos.Value;
                }

                smoothDir.smDir = smoothPos.smPos - oldSmPos;

            }).ScheduleParallel();

        Entities
            .WithName("Bee_Local_To_World_TRS")
            .WithAll<Team>()
            //.WithNone<Dead>()
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
                if (HasComponent<Dead>(beeEntity) == false)
                {
                    float stretch = math.max(1f, math.length(velocity.vel) * beeParams.speedStretch);
                    scale.z *= stretch;
                    scale.x /= (stretch - 1f) / 5f + 1f;
                    scale.y /= (stretch - 1f) / 5f + 1f;
                }

                if (HasComponent<Dead>(beeEntity))
                {   
                    baseColor.Value *= .75f;
                    scale *= math.sqrt(deathTimer.dTimer);
                }

                quaternion rotation = quaternion.identity;
                if(!smoothDir.smDir.Equals(float3.zero))
                {
                    rotation = quaternion.LookRotation(smoothDir.smDir, math.up());
                }

                localToWorld.Value = float4x4.TRS(translation.Value, rotation, scale);

            }).ScheduleParallel();
    }
}
