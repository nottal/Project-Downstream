using System.Numerics;

namespace Content.Shared._Lavaland.EntityShapes.Components;

/// <summary>
/// Scales <see cref="ShapeSpawnerCounterComponent"/> with anger
/// of an owner that spawned this EntityShapeSpawner.
/// </summary>
[RegisterComponent]
public sealed partial class AngerShapeSpawnerComponent : Component
{
    [DataField("counterRange")]
    public Vector2i? MaxCounterRange;

    [DataField("inverseCounter")]
    public bool InverseCounter;

    [DataField("periodRange")]
    public Vector2? SpawnPeriodRange;

    [DataField("inversePeriod")]
    public bool InverseSpawnPeriod;
}
