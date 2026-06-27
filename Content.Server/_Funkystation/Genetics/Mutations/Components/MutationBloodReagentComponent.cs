using Robust.Shared.Prototypes;
using Content.Shared.Chemistry.Reagent;

namespace Content.Server._Funkystation.Genetics.Mutations.Components;

[RegisterComponent]
public sealed partial class MutationBloodReagentComponent : Component
{
    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent;

    [DataField]
    public ProtoId<ReagentPrototype>? OriginalReagent;
}
