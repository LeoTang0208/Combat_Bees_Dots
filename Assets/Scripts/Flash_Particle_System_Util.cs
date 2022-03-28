using Unity.Entities;
using UnityEngine;

public class Flash_Particle_System_Util : MonoBehaviour
{
    public ParticleSystem ps;
    private void Start()
    {
        World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<Flash_Particle_System>().Init(ps);
    }
}