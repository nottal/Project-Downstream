namespace Content.Shared._Goobstation.Heretic.Components;

[RegisterComponent]
public sealed partial class RustRequiresPathStageComponent : Component
{
    /// <summary>
    /// Minimum rust-path stage required to rust this surface.
    /// </summary>
    [DataField]
    public int PathStage = 2;
}
