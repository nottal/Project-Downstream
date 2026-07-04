using System.Numerics;

namespace Content.Shared._Lavaland.EntityShapes.Components;

/// <summary>
/// Spawns an entity shape periodically or with a delay. Can be modified to expand, shrink, or move with time.
/// </summary>
[RegisterComponent]
public sealed partial class ExpandingShapeSpawnerComponent : Component
{
    [DataField]
    public Vector2? CounterOffset;

    [DataField]
    public float? CounterSize;

    [DataField]
    public float? CounterStepSize;
}
