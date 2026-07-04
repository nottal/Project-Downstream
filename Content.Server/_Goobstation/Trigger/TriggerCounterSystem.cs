using Content.Server.Explosion.EntitySystems;

namespace Content.Server._Goobstation.Trigger;

public sealed class TriggerCounterSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerCounterComponent, TriggerEvent>(OnTriggerCounter);
        SubscribeLocalEvent<TriggerCounterLimitComponent, BeforeTriggerEvent>(OnTriggerLimit);
    }

    private void OnTriggerCounter(Entity<TriggerCounterComponent> ent, ref TriggerEvent args)
    {
        if (args.Handled)
            return;

        ent.Comp.Count++;
    }

    private void OnTriggerLimit(Entity<TriggerCounterLimitComponent> ent, ref BeforeTriggerEvent args)
    {
        if (!TryComp(ent.Owner, out TriggerCounterComponent? counter))
            return;

        if (counter.Count >= ent.Comp.MaxCount)
            args.Cancelled = true;
    }
}
