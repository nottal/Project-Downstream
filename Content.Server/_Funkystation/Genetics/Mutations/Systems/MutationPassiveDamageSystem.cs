using Content.Server._Funkystation.Genetics.Mutations.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Funkystation.Genetics.Mutations.Systems;

public sealed class MutationPassiveDamageSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MutationPassiveDamageComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<MutationPassiveDamageComponent> ent, ref ComponentInit args)
    {
        ent.Comp.NextTick = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.Interval);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MutationPassiveDamageComponent, DamageableComponent>();
        while (query.MoveNext(out var uid, out var comp, out var damageable))
        {
            if (_timing.CurTime < comp.NextTick)
                continue;

            comp.NextTick += TimeSpan.FromSeconds(comp.Interval);

            var damageAmount = comp.DamagePerSecond * comp.Interval;
            var damage = new DamageSpecifier(_prototype.Index<DamageTypePrototype>(comp.DamageType), damageAmount);
            _damageable.TryChangeDamage(uid, damage, true, damageable: damageable);
        }
    }
}
