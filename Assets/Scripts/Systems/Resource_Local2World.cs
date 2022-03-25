using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class Resource_Local2World : SystemBase
{
    protected override void OnUpdate()
    {
        Entities
            .WithNone<Dead>()
            .WithAll<StackIndex>()
            .ForEach((Entity resEntity, ref LocalToWorld localToWorld, in Scale scale, in Translation translation) =>
            {
                localToWorld.Value = float4x4.TRS(translation.Value, quaternion.identity, scale.Value);
            }).ScheduleParallel();
    }
}
