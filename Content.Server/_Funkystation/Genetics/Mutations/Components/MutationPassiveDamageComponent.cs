using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._Funkystation.Genetics.Mutations.Components;

[RegisterComponent]
public sealed partial class MutationPassiveDamageComponent : Component
{
    [DataField]
    public ProtoId<DamageTypePrototype> DamageType = "Cellular";

    [DataField]
    public float DamagePerSecond = 0.1f;

    [DataField]
    public float Interval = 1.0f;

    [ViewVariables]
    public TimeSpan NextTick;
}
