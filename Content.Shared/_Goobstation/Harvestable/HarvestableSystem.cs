using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;

namespace Content.Shared._Goobstation.Harvestable;

public sealed class HarvestableSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HarvestableComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<HarvestableComponent, HarvestedDoAfterEvent>(OnHarvestedDoAfter);
    }

    private void OnInteractHand(Entity<HarvestableComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryHarvest(ent, args.User);
    }

    private bool TryHarvest(Entity<HarvestableComponent> ent, EntityUid harvester)
    {
        return _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, harvester, ent.Comp.Delay, new HarvestedDoAfterEvent(), ent.Owner, ent.Owner)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            BlockDuplicate = true,
            CancelDuplicate = true,
            DistanceThreshold = 1.5f,
            NeedHand = true,
            RequireCanInteract = true,
        });
    }

    private void OnHarvestedDoAfter(Entity<HarvestableComponent> ent, ref HarvestedDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        Harvest(ent, args.User);
        args.Handled = true;
    }

    public void Harvest(Entity<HarvestableComponent> ent, EntityUid harvester)
    {
        if (ent.Comp.Loot != null)
        {
            var item = PredictedSpawnAtPosition(ent.Comp.Loot, Transform(harvester).Coordinates);
            if (_hands.TryGetActiveHand(harvester, out var activeHand))
                _hands.TryPickup(harvester, item, activeHand, false);
        }

        PredictedDel(ent.Owner);
    }
}
