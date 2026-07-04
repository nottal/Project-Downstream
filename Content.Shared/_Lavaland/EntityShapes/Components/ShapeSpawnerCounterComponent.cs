namespace Content.Shared._Lavaland.EntityShapes.Components;

/// <summary>
/// Used for different shape spawner components to count new steps for spawns.
/// </summary>
[RegisterComponent]
public sealed partial class ShapeSpawnerCounterComponent : Component
{
    [DataField]
    public TimeSpan SpawnPeriod = TimeSpan.FromSeconds(1f);

    [DataField]
    public int MaxCounter = 1;

    [ViewVariables]
    public TimeSpan NextSpawn;

    [ViewVariables]
    public int Counter = 1; // We spawn 1 shape not in a loop, so we have to start from 1.
}
