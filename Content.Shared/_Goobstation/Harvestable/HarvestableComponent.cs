using Robust.Shared.Prototypes;

namespace Content.Shared._Goobstation.Harvestable;

/// <summary>
/// Simple component for click-to-harvest entities that produce one loot item.
/// </summary>
[RegisterComponent]
public sealed partial class HarvestableComponent : Component
{
    [DataField(required: true)]
    public EntProtoId? Loot;

    [DataField]
    public float Delay = 1f;
}
