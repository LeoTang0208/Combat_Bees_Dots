using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateAfter(typeof(Bee_Is_Holding_System))]
[UpdateAfter(typeof(Bee_Has_Target_Bee_System))]
[UpdateAfter(typeof(Bee_Has_Target_Resource_System))]
[UpdateAfter(typeof(Bee_No_Target_System))]
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
            if (HasComponent<IsHolding>(beeEntity))
            {
                resModifier = resParams.resourceSize;
            }
            if (math.abs(pos.Value.y) > field.size.y * .5f - resModifier - size)
            {
                pos.Value.y = (field.size.y * .5f - resModifier - size) * math.sign(pos.Value.y);
                velocity.vel.y *= -.5f;
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
