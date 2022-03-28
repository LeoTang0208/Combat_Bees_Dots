using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateBefore(typeof(Bee_Dead_System))]
public class Blood_Particle_System : SystemBase
{
    ParticleSystem ps;
    ParticleSystem.EmitParams emit;
    protected override void OnCreate()
    {
        base.OnCreate();
        Enabled = false;
    }
    public void Init(ParticleSystem ps)
    {
        this.ps = ps;
        Enabled = true;
    }
    protected override void OnUpdate()
    {
        Entities
            .WithoutBurst()
            .WithAll<Blood_Particle>()
            .ForEach((ref Translation pos) => 
        {
            emit.position = new Vector3(pos.Value.x, pos.Value.z, pos.Value.y * -1f);
            emit.rotation = 90;
            emit.startSize = .5f;

            ps.Emit(emit, 1);
            //ps.Play();
        }).Run();
    }
}
