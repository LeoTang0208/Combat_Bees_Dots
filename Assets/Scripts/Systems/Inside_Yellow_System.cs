using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class Inside_Yellow_system : SystemBase
{
    private EntityQuery Yellow_Query;

    protected override void OnCreate()
    {
        Yellow_Query = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(Team_Y), typeof(Translation) },
            None = new ComponentType[] { typeof(Dead) }
        });
    }
    protected override void OnUpdate()
    {
        var beeParams = GetSingleton<BeeControlParams>();
        float deltaTime = Time.fixedDeltaTime;

        NativeArray<Translation> Yellow_Pos = Yellow_Query.ToComponentDataArrayAsync<Translation>(Allocator.TempJob, out JobHandle Yellow_Handle);
        Yellow_Handle.Complete();

        Dependency = Entities
            .WithAll<Team_Y>()
            .WithNone<Dead>()
            .WithReadOnly(Yellow_Pos)
            .ForEach((int entityInQueryIndex, ref Velocity velocity, in Translation pos) =>
        {
            var random = Unity.Mathematics.Random.CreateFromIndex((uint)entityInQueryIndex);

            int Index = random.NextInt(0, Yellow_Pos.Length);
            float3 delta = Yellow_Pos[Index].Value - pos.Value;
            float dist = math.sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
            if (dist > 0f)
            {
                velocity.vel += delta * (beeParams.teamAttraction * deltaTime / dist);
            }

            Index = random.NextInt(0, Yellow_Pos.Length);
            delta = Yellow_Pos[Index].Value - pos.Value;
            dist = math.sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
            if (dist > 0f)
            {
                velocity.vel -= delta * (beeParams.teamRepulsion * deltaTime / dist);
            }
        }).ScheduleParallel(Dependency);
        Dependency.Complete();
        Yellow_Pos.Dispose();
    }
}
