
namespace Content.Shared._Lavaland.Anger.Components;

/// <summary>
/// Makes action's delay depend on current anger level of the parent entity.
/// </summary>
[RegisterComponent]
public sealed partial class AngerDelayActionComponent : Component
{
    [DataField(required: true)]
    public TimeSpan MinDelay;

    [DataField(required: true)]
    public TimeSpan MaxDelay;

    [DataField]
    public bool Inverse;
}
