using Content.Shared.Hands.Components;
using Content.Shared.Stunnable;

namespace Content.Shared.Hands.EntitySystems;

/// <summary>
/// Events that are related to hand state but not direct pickup/drop interactions.
/// </summary>
public abstract partial class SharedHandsSystem
{
    private void InitializeEventListeners()
    {
        SubscribeLocalEvent<HandsComponent, GetStandUpTimeEvent>(OnStandupArgs);
        SubscribeLocalEvent<HandsComponent, KnockedDownRefreshEvent>(OnKnockedDownRefresh);
    }

    /// <summary>
    /// Free hands reduce the time it takes to stand up from crawling.
    /// </summary>
    private void OnStandupArgs(Entity<HandsComponent> ent, ref GetStandUpTimeEvent args)
    {
        if (!HasComp<KnockedDownComponent>(ent))
            return;

        var freeHands = ent.Comp.CountFreeHands();
        if (freeHands == 0)
            return;

        args.DoAfterTime *= (float) ent.Comp.Count / (freeHands + ent.Comp.Count);
    }

    /// <summary>
    /// Crawling movement scales with free hands.
    /// With no free hands, crawling movement becomes zero.
    /// </summary>
    private void OnKnockedDownRefresh(Entity<HandsComponent> ent, ref KnockedDownRefreshEvent args)
    {
        var totalHands = ent.Comp.Hands.Count;

        if (totalHands == 0)
            return;

        var freeHands = ent.Comp.CountFreeHands();
        args.SpeedModifier *= (float) freeHands / totalHands;
    }
}
