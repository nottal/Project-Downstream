using Content.Server._Lavaland.Weapons.Melee.Components;
using Content.Shared._Lavaland.Weapons.Melee;
using Content.Shared.EntityEffects;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server._Lavaland.Weapons.Melee;

public sealed class MeleeUpgradesSystem : SharedMeleeUpgradesSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WeaponUpgradeEffectsComponent, MeleeHitEvent>(OnEffectsUpgradeHit);
    }

    private void OnEffectsUpgradeHit(Entity<WeaponUpgradeEffectsComponent> ent, ref MeleeHitEvent args)
    {
        foreach (var hit in args.HitEntities)
        {
            var effectArgs = new EntityEffectBaseArgs(hit, EntityManager);

            foreach (var effect in ent.Comp.Effects)
            {
                if (!effect.ShouldApply(effectArgs))
                    continue;

                effect.Effect(effectArgs);
            }
        }
    }
}
