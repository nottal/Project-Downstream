namespace Content.Server._Goobstation.Trigger;

/// <summary>
/// Cancels triggering after the paired TriggerCounter reaches this limit.
/// </summary>
[RegisterComponent]
public sealed partial class TriggerCounterLimitComponent : Component
{
    [DataField]
    public int MaxCount = 1;
}
