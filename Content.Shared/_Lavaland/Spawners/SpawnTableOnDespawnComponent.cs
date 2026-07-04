using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Shared._Lavaland.Spawners;

/// <summary>
/// Spawns a table of entities on despawn.
/// </summary>
[RegisterComponent]
public sealed partial class SpawnTableOnDespawnComponent : Component
{
    [DataField(required: true)]
    public EntityTableSelector Table;
}
