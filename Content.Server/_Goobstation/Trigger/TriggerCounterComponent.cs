namespace Content.Server._Goobstation.Trigger;

/// <summary>
/// Counts how many times an entity has triggered during its lifetime.
/// </summary>
[RegisterComponent]
public sealed partial class TriggerCounterComponent : Component
{
    [DataField]
    public int Count;
}
