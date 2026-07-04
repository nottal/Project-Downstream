using Content.Shared.Whitelist;

namespace Content.Shared.Armor;

/// <summary>
/// Compatibility marker for upstream suit-storage whitelists.
/// </summary>
[RegisterComponent]
public sealed partial class AllowSuitStorageComponent : Component
{
    [DataField]
    public EntityWhitelist Whitelist = new()
    {
        Components = new[] { "Item" }
    };
}
