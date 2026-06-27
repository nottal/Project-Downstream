using Content.Server._Funkystation.Genetics.Mutations.Components;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;

namespace Content.Server._Funkystation.Genetics.Mutations.Systems;

public sealed class MutationBloodReagentSystem : EntitySystem
{
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MutationBloodReagentComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<MutationBloodReagentComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<MutationBloodReagentComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<BloodstreamComponent>(ent.Owner, out var bloodstream))
            return;

        ent.Comp.OriginalReagent = bloodstream.BloodReagent;
        _bloodstream.ChangeBloodReagent(ent.Owner, ent.Comp.Reagent, bloodstream);
    }

    private void OnShutdown(Entity<MutationBloodReagentComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.OriginalReagent is not { } originalReagent ||
            !TryComp<BloodstreamComponent>(ent.Owner, out var bloodstream))
            return;

        _bloodstream.ChangeBloodReagent(ent.Owner, originalReagent, bloodstream);
        ent.Comp.OriginalReagent = null;
    }
}
