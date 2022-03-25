using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class Inside_Blue_system : SystemBase
{
    private EntityQuery Blue_Query;

    protected override void OnCreate()
    {
        Blue_Query = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(Team_B), typeof(Translation) },
            None = new ComponentType[] { typeof(Dead) }
        });
    }
    protected override void OnUpdate()
    {
        var beeParams = GetSingleton<BeeControlParams>();
        float deltaTime = Time.fixedDeltaTime;

        NativeArray<Translation> Blue_Pos = Blue_Query.ToComponentDataArrayAsync<Translation>(Allocator.TempJob, out JobHandle Blue_Handle);
        Blue_Handle.Complete();

        Dependency = Entities
            .WithAll<Team_B>()
            .WithNone<Dead>()
            .WithReadOnly(Blue_Pos)
            .ForEach((int entityInQueryIndex, ref Velocity velocity, in Translation pos) =>
            {
                var random = Unity.Mathematics.Random.CreateFromIndex((uint)entityInQueryIndex);
                
                float3 rndVel = random.NextFloat3Direction();
                velocity.vel += rndVel * beeParams.flightJitter * deltaTime;
                velocity.vel *= (1f - beeParams.damping);

                int Index = random.NextInt(0, Blue_Pos.Length);
                float3 delta = Blue_Pos[Index].Value - pos.Value;
                float dist = math.sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
                if (dist > 0f)
                {
                    velocity.vel += delta * (beeParams.teamAttraction * deltaTime / dist);
                }

                Index = random.NextInt(0, Blue_Pos.Length);
                delta = Blue_Pos[Index].Value - pos.Value;
                dist = math.sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
                if (dist > 0f)
                {
                    velocity.vel -= delta * (beeParams.teamRepulsion * deltaTime / dist);
                }
            }).ScheduleParallel(Dependency);
        Dependency.Complete();
        Blue_Pos.Dispose();
    }
}
