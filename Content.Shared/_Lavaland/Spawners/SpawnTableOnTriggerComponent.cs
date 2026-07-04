using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Shared._Lavaland.Spawners;

/// <summary>
/// Spawns a table of entities when the owner is triggered.
/// </summary>
[RegisterComponent]
public sealed partial class SpawnTableOnTriggerComponent : Component
{
    [DataField(required: true)]
    public EntityTableSelector Table;
}
