using System.Numerics;

namespace Content.Shared._Lavaland.TileChaser;

/// <summary>
/// Makes a tile chaser depend on anger levels from the spawned owner.
/// </summary>
[RegisterComponent]
public sealed partial class AngerTileChaserComponent : Component
{
    [DataField]
    public Vector2 SpeedRange;

    [DataField]
    public Vector2i StepsRange;

    [DataField]
    public bool Inverse;
}
