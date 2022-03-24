using Unity.Mathematics;
using Unity.Entities;
public struct Velocity : IComponentData
{
    public float3 vel;
}

public struct DeathTimer : IComponentData
{
    public float dTimer;
}
public struct Dead : IComponentData {}

public struct IsAttacking : IComponentData {}

public struct Stacked : IComponentData {}

public struct IsHolding : IComponentData {}

public struct TargetBee : IComponentData
{
    public Entity bee;
}

public struct TargetResource : IComponentData
{
    public Entity res;
}

public struct HolderBee : IComponentData
{
    public Entity holder;
}
