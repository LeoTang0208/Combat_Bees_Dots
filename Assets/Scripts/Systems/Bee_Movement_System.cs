using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class Bee_Movement_System : SystemBase
{
    protected override void OnUpdate()
    {
        var beeParams = GetSingleton<BeeControlParams>();
        var field = GetSingleton<FieldAuthoring>();
        var resParams = GetSingleton<ResourceParams>();
        float deltaTime = Time.fixedDeltaTime;

        Entities
            .WithAll<Team>()
            .WithNone<Dead>()
            .ForEach((ref Translation pos, in Velocity velocity) =>
        {
            pos.Value += deltaTime * velocity.vel;
        }).ScheduleParallel();

        Entities
            .WithAll<Team>()
            .WithNone<Dead>()
            .ForEach((Entity beeEntity, ref Velocity velocity, ref Translation pos) =>
        {
            if(HasComponent<Size>(beeEntity))
            {
                float size = GetComponent<Size>(beeEntity).value;
            }

            if (math.abs(pos.Value.x) > field.size.x * .5f)
            {
                pos.Value.x = field.size.x * .5f * math.sign(pos.Value.x);
                velocity.vel.x *= -.15f;
                velocity.vel.y *= .8f;
                velocity.vel.z *= .8f;
            }

            if (math.abs(pos.Value.z) > field.size.z * .5f)
            {
                pos.Value.z = field.size.z * .5f * math.sign(pos.Value.z);
                velocity.vel.z *= -.15f;
                velocity.vel.x *= .8f;
                velocity.vel.y *= .8f;
            }

            float resModifier = 0f;
            if (HasComponent<IsHolding>(beeEntity))
            {
                resModifier = resParams.resourceSize;
            }
            if (math.abs(pos.Value.y) > field.size.y * .5f - resModifier)
            {
                pos.Value.y = (field.size.y * .5f - resModifier) * math.sign(pos.Value.y);
                velocity.vel.y *= -.15f;
                velocity.vel.x *= .8f;
                velocity.vel.z *= .8f;
            }
        }).ScheduleParallel();

        Entities
            .WithAll<Team>()
            .WithNone<Dead>()
            .ForEach((
                Entity beeEntity, 
                ref SmoothPosition smoothPos, 
                ref SmoothDirection smoothDir, 
                in Velocity velocity, in Translation pos) =>
        {
            float3 oldSmPos = smoothPos.smPos;
            if (HasComponent<IsAttacking>(beeEntity))
            {
                smoothPos.smPos = pos.Value;
            }
            else 
            {
                smoothPos.smPos = math.lerp(smoothPos.smPos, pos.Value, deltaTime * beeParams.rotationStiffness);
            }

            smoothDir.smDir = smoothPos.smPos - oldSmPos;
        }).ScheduleParallel();
    }
}
