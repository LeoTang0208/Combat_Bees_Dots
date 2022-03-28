using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class Bee_Jitter_System : SystemBase
{
    protected override void OnUpdate()
    {
        var beeParams = GetSingleton<BeeControlParams>();
        float deltaTime = Time.fixedDeltaTime;

        Entities
            .WithAll<Team>()
            .WithNone<Dead>()
            .ForEach((int entityInQueryIndex, ref Velocity velocity) =>
        {
            var random = Unity.Mathematics.Random.CreateFromIndex((uint)entityInQueryIndex);

            float3 rndVel = random.NextFloat3Direction();
            velocity.vel += rndVel * beeParams.flightJitter * deltaTime;
            velocity.vel *= (1f - beeParams.damping);
        }).ScheduleParallel();
    }
}
