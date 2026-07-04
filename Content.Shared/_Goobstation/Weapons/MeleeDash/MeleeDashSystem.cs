using Content.Goobstation.Common.Weapons.MeleeDash;
using Content.Shared.Emoting;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Standing;
using Content.Shared.Throwing;
using Content.Shared.Timing;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;

namespace Content.Goobstation.Shared.Weapons.MeleeDash;

public sealed class MeleeDashSystem : EntitySystem
{
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    private const int DashCollisionLayer = (int) CollisionGroup.MidImpassable;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DashingComponent, LandEvent>(OnLand);
        SubscribeLocalEvent<DashingComponent, StartCollideEvent>(OnCollide);
        SubscribeAllEvent<MeleeDashEvent>(OnDash);
    }

    private void OnCollide(Entity<DashingComponent> ent, ref StartCollideEvent args)
    {
        if (!TryComp(ent, out ActorComponent? actor))
            return;

        if (!TryComp(ent.Comp.Weapon, out MeleeWeaponComponent? melee))
            return;

        if (ent.Comp.HitEntities.Contains(args.OtherEntity))
            return;

        if (!HasComp<MobStateComponent>(args.OtherEntity))
            return;

        if (!_hands.IsHolding(ent.Owner, ent.Comp.Weapon.Value))
            return;

        ent.Comp.HitEntities.Add(args.OtherEntity);
        Dirty(ent);

        var ev = new LightAttackEvent(GetNetEntity(args.OtherEntity),
            GetNetEntity(ent.Comp.Weapon.Value),
            GetNetCoordinates(Transform(args.OtherEntity).Coordinates));
        _melee.DoLightAttackFromExternalSystem(ent.Owner, ev, ent.Comp.Weapon.Value, melee, actor.PlayerSession);
    }

    private void OnLand(Entity<DashingComponent> ent, ref LandEvent args)
    {
        if (TryComp(ent, out FixturesComponent? fixtureComponent))
        {
            foreach (var key in ent.Comp.ChangedFixtures)
            {
                if (!fixtureComponent.Fixtures.TryGetValue(key, out var fixture))
                    continue;

                _physics.SetCollisionMask(ent,
                    key,
                    fixture,
                    fixture.CollisionMask | DashCollisionLayer,
                    fixtureComponent);
            }
        }

        RemCompDeferred(ent, ent.Comp);
    }

    private void OnDash(MeleeDashEvent msg, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.AttachedEntity;
        if (user == null)
            return;

        if (_standing.IsDown(user.Value))
            return;

        if (_container.IsEntityInContainer(user.Value))
            return;

        var weapon = GetEntity(msg.Weapon);

        if (!TryComp(weapon, out MeleeDashComponent? dash) ||
            !TryComp(weapon, out UseDelayComponent? delay) ||
            _useDelay.IsDelayed((weapon, delay)))
            return;

        var length = MathF.Min(msg.Direction.Length(), dash.MaxDashLength);
        if (length <= 0f)
            return;

        var dir = msg.Direction.Normalized() * length;

        _useDelay.TryResetDelay((weapon, delay));

        var dashing = EnsureComp<DashingComponent>(user.Value);
        dashing.HitEntities.Clear();
        dashing.ChangedFixtures.Clear();

        if (TryComp(user.Value, out FixturesComponent? fixtureComponent))
        {
            foreach (var (key, fixture) in fixtureComponent.Fixtures)
            {
                if ((fixture.CollisionMask & DashCollisionLayer) == 0)
                    continue;

                dashing.ChangedFixtures.Add(key);
                _physics.SetCollisionMask(user.Value,
                    key,
                    fixture,
                    fixture.CollisionMask & ~DashCollisionLayer,
                    manager: fixtureComponent);
            }
        }

        dashing.Weapon = weapon;
        Dirty(user.Value, dashing);

        _throwing.TryThrow(user.Value, dir, dash.DashForce, null, 0f, null, false, false, false, false, false);
        _audio.PlayPredicted(dash.DashSound, user.Value, user.Value);

        if (dash.EmoteOnDash == null || !TryComp(user.Value, out AnimatedEmotesComponent? emotes))
            return;

        emotes.Emote = dash.EmoteOnDash;
        Dirty(user.Value, emotes);
    }
}
