using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class Resource_Spawner_System : SystemBase
{
    protected override void OnUpdate()
    {
        var field = GetSingleton<FieldAuthoring>();
        var resParams = GetSingleton<ResourceParams>();
        var resGridParams = GetSingleton<ResourceGridParams>();
        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        Entities
            .ForEach((Entity spawnerEntity, in resourceSpawner spawner, in Translation spawnerPos) =>
            {
                for (int i = 0; i < spawner.count; i++)
                {
                    var bee = ecb.Instantiate(spawner.resPrefab);

                    ecb.SetComponent(bee, new Scale { Value = resParams.resourceSize });

                    float3 pos;
                    if (spawner.isRandom)
                    {
                        pos = Utils.GetRandomPosition(resGridParams, field);
                        ecb.SetComponent(bee, new Translation { Value = pos });
                    }
                    else
                    {
                        pos = spawnerPos.Value;
                        ecb.SetComponent(bee, new Translation { Value = spawnerPos.Value });
                    }

                    float size = resParams.resourceSize;
                    ecb.AddComponent(bee, new NonUniformScale { Value = new float3(size, size, size) });
                }
                ecb.DestroyEntity(spawnerEntity);
            }).Run();

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}