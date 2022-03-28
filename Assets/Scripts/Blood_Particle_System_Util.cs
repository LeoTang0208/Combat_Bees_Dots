using Unity.Entities;
using UnityEngine;

public class Blood_Particle_System_Util : MonoBehaviour
{
    public ParticleSystem ps;
    private void Start()
    {
        World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<Blood_Particle_System>().Init(ps);
    }
}