using System.Diagnostics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public class Bee_Spawner_System : SystemBase
{
    protected override void OnUpdate()
    {
        var field = GetSingleton<FieldAuthoring>();
        var beeParams = GetSingleton<BeeControlParams>();
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var random = new Unity.Mathematics.Random(1234);

        Entities
            .ForEach((Entity spawnerEntity, ref BeeSpawner spawner) =>
            {
                for (int i = 0; i < spawner.count; i++)
                {
                    var bee = ecb.Instantiate(spawner.beePrefab);

                    int team = (int)spawner.team;
                    float3 pos = math.right() * (-field.size.x * .4f + field.size.x * .8f * team);
                    ecb.SetComponent(bee, new Translation { Value = pos });

                    float size = random.NextFloat(beeParams.minBeeSize, beeParams.maxBeeSize);
                    ecb.AddComponent(bee, new Size { value = size });

                    Vector3 init_vel_vector = UnityEngine.Random.insideUnitSphere * spawner.maxSpawnSpeed;
                    float3 init_vel;
                    init_vel.x = init_vel_vector.x;
                    init_vel.y = init_vel_vector.y;
                    init_vel.z = init_vel_vector.z;
                    ecb.AddComponent(bee, new Velocity {vel = init_vel});

                    URPMaterialPropertyBaseColor baseColor;
                    if (spawner.team == 0)
                    {
                        baseColor.Value = new float4(0.16471f, 0.61569f, 0.95686f, 1f);
                    }
                    else
                    {
                        baseColor.Value = new float4(0.8f, 0.8f, 0f, 1f);
                    }
                    ecb.AddComponent<URPMaterialPropertyBaseColor>(bee, baseColor);
                }
                ecb.DestroyEntity(spawnerEntity);
            }).Run();
        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}
