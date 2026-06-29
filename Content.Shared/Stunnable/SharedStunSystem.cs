// SPDX-FileCopyrightText: 2021 Paul Ritter <ritter.paul1@googlemail.com>
// SPDX-FileCopyrightText: 2021 Vera Aguilera Puerto <6766154+Zumorica@users.noreply.github.com>
// SPDX-FileCopyrightText: 2021 Vera Aguilera Puerto <gradientvera@outlook.com>
// SPDX-FileCopyrightText: 2021 metalgearsloth <comedian_vs_clown@hotmail.com>
// SPDX-FileCopyrightText: 2021 mirrorcult <lunarautomaton6@gmail.com>
// SPDX-FileCopyrightText: 2021 pointer-to-null <91910481+pointer-to-null@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 Acruid <shatter66@gmail.com>
// SPDX-FileCopyrightText: 2022 Chief-Engineer <119664036+Chief-Engineer@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 Rane <60792108+Elijahrane@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 keronshb <54602815+keronshb@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 wrexbe <81056464+wrexbe@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 DrSmugleaf <drsmugleaf@gmail.com>
// SPDX-FileCopyrightText: 2023 Jezithyr <jezithyr@gmail.com>
// SPDX-FileCopyrightText: 2023 Kara <lunarautomaton6@gmail.com>
// SPDX-FileCopyrightText: 2023 Pieter-Jan Briers <pieterjan.briers@gmail.com>
// SPDX-FileCopyrightText: 2023 deltanedas <39013340+deltanedas@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Aiden <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2024 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 John Space <bigdumb421@gmail.com>
// SPDX-FileCopyrightText: 2024 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 PJBot <pieterjan.briers+bot@gmail.com>
// SPDX-FileCopyrightText: 2024 Tadeo <td12233a@gmail.com>
// SPDX-FileCopyrightText: 2024 Tayrtahn <tayrtahn@gmail.com>
// SPDX-FileCopyrightText: 2024 whateverusername0 <whateveremail>
// SPDX-FileCopyrightText: 2025 Drywink <hugogrethen@gmail.com>
// SPDX-FileCopyrightText: 2025 Princess Cheeseballs <66055347+princess-cheeseballs@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Princess Cheeseballs <66055347+pronana@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Princess-Cheeseballs <https://github.com/Princess-Cheeseballs>
// SPDX-FileCopyrightText: 2025 Tay <td12233a@gmail.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: MIT

using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Alert;
using Content.Shared.Buckle.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Input;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Gravity;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Rejuvenate;
using Content.Shared.Standing;
using Content.Shared.Physics;
using Content.Shared.StatusEffect;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Containers;
using Content.Shared._White.Standing;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.Jittering;
using Robust.Shared.Timing;
using Robust.Shared.Configuration;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Stunnable;

public abstract partial class SharedStunSystem : EntitySystem
{
    private readonly Dictionary<EntityUid, TimeSpan> _nextToggleKnockdownAt = new();
    private readonly Dictionary<EntityUid, TimeSpan> _nextStandAttemptAt = new();
    private static readonly TimeSpan AutoStandRetryDelay = TimeSpan.FromSeconds(0.25);
    private static readonly TimeSpan ToggleKnockdownCooldown = TimeSpan.FromSeconds(0.8);
    private static readonly TimeSpan ManualStandAttemptCooldown = TimeSpan.FromSeconds(0.8);
    public static readonly ProtoId<AlertPrototype> KnockdownAlert = "KnockedDown";

    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly SharedBroadphaseSystem _broadphase = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly StandingStateSystem _standingState = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly SharedStutteringSystem _stutter = default!; // goob edit
    [Dependency] private readonly SharedJitteringSystem _jitter = default!; // goob edit
    [Dependency] private readonly ClothingModifyStunTimeSystem _modify = default!; // goob edit
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<KnockedDownComponent, RejuvenateEvent>(OnKnockRejuvenate);
        SubscribeLocalEvent<KnockedDownComponent, ComponentInit>(OnKnockInit);
        SubscribeLocalEvent<KnockedDownComponent, ComponentShutdown>(OnKnockShutdown);
        SubscribeLocalEvent<KnockedDownComponent, BuckleAttemptEvent>(OnBuckleAttempt);
        SubscribeLocalEvent<KnockedDownComponent, StandAttemptEvent>(OnStandAttempt);
        SubscribeLocalEvent<KnockedDownComponent, RefreshMovementSpeedModifiersEvent>(OnKnockedRefreshSpeed);
        SubscribeLocalEvent<CrawlerComponent, KnockedDownRefreshEvent>(OnCrawlerKnockedRefresh);
        SubscribeLocalEvent<CrawlerComponent, global::Content.Shared.Damage.DamageChangedEvent>(OnCrawlerDamaged);
        SubscribeLocalEvent<KnockedDownComponent, TryStandDoAfterEvent>(OnStandDoAfter);
        SubscribeLocalEvent<KnockedDownComponent, KnockedDownAlertEvent>(OnKnockedDownAlert);
        SubscribeAllEvent<ForceStandUpEvent>(OnForceStandup);
        SubscribeLocalEvent<KnockedDownComponent, DidEquipHandEvent>(OnHandEquippedWhileKnocked);
        SubscribeLocalEvent<KnockedDownComponent, DidUnequipHandEvent>(OnHandUnequippedWhileKnocked);
        SubscribeLocalEvent<KnockedDownComponent, HandCountChangedEvent>(OnHandCountChangedWhileKnocked);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ToggleKnockdown, InputCmdHandler.FromDelegate(HandleToggleKnockdown, handle: false))
            .Register<SharedStunSystem>();

        SubscribeLocalEvent<SlowedDownComponent, ComponentInit>(OnSlowInit);
        SubscribeLocalEvent<SlowedDownComponent, ComponentShutdown>(OnSlowRemove);

        SubscribeLocalEvent<StunnedComponent, ComponentStartup>(UpdateCanMove);
        SubscribeLocalEvent<StunnedComponent, ComponentShutdown>(OnStunShutdown);

        SubscribeLocalEvent<StunOnContactComponent, ComponentStartup>(OnStunOnContactStartup);
        SubscribeLocalEvent<StunOnContactComponent, StartCollideEvent>(OnStunOnContactCollide);

        // helping people up if they're knocked down
        SubscribeLocalEvent<SlowedDownComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);

        SubscribeLocalEvent<KnockedDownComponent, TileFrictionEvent>(OnKnockedTileFriction);

        // Attempt event subscriptions.
        SubscribeLocalEvent<StunnedComponent, ChangeDirectionAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<StunnedComponent, UpdateCanMoveEvent>(OnMoveAttempt);
        SubscribeLocalEvent<StunnedComponent, InteractionAttemptEvent>(OnAttemptInteract);
        SubscribeLocalEvent<StunnedComponent, UseAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<StunnedComponent, ThrowAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<StunnedComponent, DropAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<StunnedComponent, AttackAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<StunnedComponent, PickupAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<StunnedComponent, IsEquippingAttemptEvent>(OnEquipAttempt);
        SubscribeLocalEvent<StunnedComponent, IsUnequippingAttemptEvent>(OnUnequipAttempt);
        SubscribeLocalEvent<MobStateComponent, MobStateChangedEvent>(OnMobStateChanged);

        // Stun Appearance Data
        InitializeAppearance();
    }

    private void OnAttemptInteract(Entity<StunnedComponent> ent, ref InteractionAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnMobStateChanged(EntityUid uid, MobStateComponent component, MobStateChangedEvent args)
    {
        if (!TryComp<StatusEffectsComponent>(uid, out var status))
        {
            return;
        }
        switch (args.NewMobState)
        {
            case MobState.Alive:
            case MobState.SoftCritical:
                {
                    break;
                }
            case MobState.Critical:
            case MobState.HardCritical:
            case MobState.Dead:
                {
                    _statusEffect.TryRemoveStatusEffect(uid, "Stun");
                    break;
                }
            case MobState.Invalid:
            default:
                return;
        }

    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<SharedStunSystem>();
    }


    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<KnockedDownComponent>();
        while (query.MoveNext(out var uid, out var knocked))
        {
            if (!knocked.AutoStand || knocked.DoAfterId != null || knocked.NextUpdate > _timing.CurTime)
                continue;

            if (!TryStanding(uid, knocked) && knocked.NextUpdate <= _timing.CurTime)
                ScheduleAutoStandRetry(uid, knocked);
        }
    }

    private void OnStunShutdown(Entity<StunnedComponent> ent, ref ComponentShutdown args)
    {
        // This exists so the client can end their funny animation if they're playing one.
        UpdateCanMove(ent, ent.Comp, args);
        Appearance.RemoveData(ent, StunVisuals.SeeingStars);
    }

    private void UpdateCanMove(EntityUid uid, StunnedComponent component, EntityEventArgs args)
    {
        _blocker.UpdateCanMove(uid);
    }

    private void OnStunOnContactStartup(Entity<StunOnContactComponent> ent, ref ComponentStartup args)
    {
        if (TryComp<PhysicsComponent>(ent, out var body))
            _broadphase.RegenerateContacts((ent, body));
    }

    private void OnStunOnContactCollide(Entity<StunOnContactComponent> ent, ref StartCollideEvent args)
    {
        if (args.OurFixtureId != ent.Comp.FixtureId)
            return;

        if (_entityWhitelist.IsBlacklistPass(ent.Comp.Blacklist, args.OtherEntity))
            return;

        if (!TryComp<StatusEffectsComponent>(args.OtherEntity, out var status))
            return;

        TryStun(args.OtherEntity, ent.Comp.Duration, true, status);
        TryKnockdown(args.OtherEntity, ent.Comp.Duration, true, status);
    }

    private void OnKnockInit(EntityUid uid, KnockedDownComponent component, ComponentInit args)
    {
        UpdateKnockdownAlert(uid, component);
        RefreshKnockedMovement(uid, component);
        _standingState.Down(uid, true, false);
    }

    private void OnKnockShutdown(EntityUid uid, KnockedDownComponent component, ComponentShutdown args)
    {
        _nextToggleKnockdownAt.Remove(uid);
        _nextStandAttemptAt.Remove(uid);
        component.FrictionModifier = 1f;
        component.SpeedModifier = 1f;
        CancelKnockdownDoAfter(uid, component);
        component.DoAfterId = null;
        _alerts.ClearAlert(uid, KnockdownAlert);

        if (_mobState.IsIncapacitated(uid))
        {
            _standingState.Down(uid, playSound: false, dropHeldItems: false);
            return;
        }

        _standingState.Stand(uid);
    }

    private void OnKnockRejuvenate(EntityUid uid, KnockedDownComponent component, ref RejuvenateEvent args)
    {
        SetKnockdownNextUpdate(uid, component, _timing.CurTime);

        if (component.AutoStand)
            RemComp<KnockedDownComponent>(uid);
    }

    private void ScheduleAutoStandRetry(EntityUid uid, KnockedDownComponent component)
    {
        var nextUpdate = _timing.CurTime + AutoStandRetryDelay;
        if (component.NextUpdate >= nextUpdate)
            return;

        SetKnockdownNextUpdate(uid, component, nextUpdate);
    }

    public void SetAutoStand(EntityUid uid, KnockedDownComponent? component = null, bool autoStand = false)
    {
        if (!Resolve(uid, ref component, false) || component.AutoStand == autoStand)
            return;

        component.AutoStand = autoStand;
        Dirty(uid, component);
    }

    public void CancelKnockdownDoAfter(EntityUid uid, KnockedDownComponent? component = null)
    {
        if (!Resolve(uid, ref component, false) || component.DoAfterId == null)
            return;

        _doAfter.Cancel(uid, component.DoAfterId.Value);
        component.DoAfterId = null;
        Dirty(uid, component);
    }

    private void OnBuckleAttempt(EntityUid uid, KnockedDownComponent component, ref BuckleAttemptEvent args)
    {
        if (args.User == uid && component.NextUpdate > _timing.CurTime)
            args.Cancelled = true;
    }

    public void SetKnockdownTime(EntityUid uid, TimeSpan time, KnockedDownComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        SetKnockdownNextUpdate(uid, component, _timing.CurTime + time);
    }

    public void UpdateKnockdownTime(EntityUid uid, TimeSpan time, bool refresh = true, KnockedDownComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        if (refresh)
        {
            var knockedTime = _timing.CurTime + time;
            if (component.NextUpdate < knockedTime)
                SetKnockdownNextUpdate(uid, component, knockedTime);

            return;
        }

        if (component.NextUpdate < _timing.CurTime)
            SetKnockdownNextUpdate(uid, component, _timing.CurTime + time);
        else
            SetKnockdownNextUpdate(uid, component, component.NextUpdate + time);
    }

    private void SetKnockdownNextUpdate(EntityUid uid, KnockedDownComponent component, TimeSpan time)
    {
        if (_timing.CurTime > time)
            time = _timing.CurTime;

        component.NextUpdate = time;
        Dirty(uid, component);
        UpdateKnockdownAlert(uid, component);
    }

    private void UpdateKnockdownAlert(EntityUid uid, KnockedDownComponent component)
    {
        (TimeSpan, TimeSpan)? cooldown = component.NextUpdate > _timing.CurTime
            ? (_timing.CurTime, component.NextUpdate)
            : null;

        _alerts.ShowAlert(uid, KnockdownAlert, cooldown: cooldown);
    }

    private void RefreshKnockedMovement(EntityUid uid, KnockedDownComponent component)
    {
        var ev = new KnockedDownRefreshEvent();
        RaiseLocalEvent(uid, ref ev);

        if (MathHelper.CloseTo(component.SpeedModifier, ev.SpeedModifier) &&
            MathHelper.CloseTo(component.FrictionModifier, ev.FrictionModifier))
            return;

        component.SpeedModifier = ev.SpeedModifier;
        component.FrictionModifier = ev.FrictionModifier;
        Dirty(uid, component);
        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
    }

    private void OnKnockedRefreshSpeed(EntityUid uid, KnockedDownComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.SpeedModifier);
    }

    private void OnCrawlerKnockedRefresh(EntityUid uid, CrawlerComponent component, ref KnockedDownRefreshEvent args)
    {
        args.SpeedModifier *= component.SpeedModifier;
        args.FrictionModifier *= component.FrictionModifier;
    }

    private void OnCrawlerDamaged(EntityUid uid, CrawlerComponent component, ref global::Content.Shared.Damage.DamageChangedEvent args)
    {
        if (!TryComp(uid, out KnockedDownComponent? knocked) ||
            !args.InterruptsDoAfters ||
            !args.DamageIncreased ||
            args.DamageDelta == null ||
            _timing.ApplyingState)
        {
            return;
        }

        if (args.DamageDelta.GetTotal() >= component.KnockdownDamageThreshold)
            UpdateKnockdownTime(uid, component.DefaultKnockedDuration, component: knocked);
    }

    private void OnHandEquippedWhileKnocked(EntityUid uid, KnockedDownComponent component, ref DidEquipHandEvent args)
    {
        if (_timing.ApplyingState)
            return;

        RefreshKnockedMovement(uid, component);
    }

    private void OnHandUnequippedWhileKnocked(EntityUid uid, KnockedDownComponent component, ref DidUnequipHandEvent args)
    {
        if (_timing.ApplyingState)
            return;

        RefreshKnockedMovement(uid, component);
    }

    private void OnHandCountChangedWhileKnocked(EntityUid uid, KnockedDownComponent component, ref HandCountChangedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        RefreshKnockedMovement(uid, component);
    }

    private void HandleToggleKnockdown(ICommonSession? session)
    {
        if (session?.AttachedEntity is not { } uid || !_cfg.GetCVar(CCVars.MovementCrawling))
            return;

        if (!Exists(uid))
        {
            _nextToggleKnockdownAt.Remove(uid);
            _nextStandAttemptAt.Remove(uid);
            return;
        }

        if (!HasComp<CrawlerComponent>(uid))
            return;

        if (_mobState.IsIncapacitated(uid))
            return;

        if (_nextToggleKnockdownAt.TryGetValue(uid, out var nextToggle) && _timing.CurTime < nextToggle)
            return;

        _nextToggleKnockdownAt[uid] = _timing.CurTime + ToggleKnockdownCooldown;

        if (!TryComp(uid, out KnockedDownComponent? knocked))
        {
            var crawler = Comp<CrawlerComponent>(uid);
            TryKnockdown(uid, crawler.DefaultKnockedDuration, true, autoStand: false, drop: false);
            return;
        }

        var stand = !knocked.DoAfterId.HasValue;
        if (_nextStandAttemptAt.TryGetValue(uid, out var nextStandAttempt) && _timing.CurTime < nextStandAttempt)
            return;

        SetAutoStand(uid, knocked, stand);

        if (!TryStanding(uid, knocked, popupOnBlocked: true))
        {
            if (!stand || knocked.DoAfterId.HasValue)
                CancelKnockdownDoAfter(uid, knocked);

            _nextStandAttemptAt[uid] = _timing.CurTime + ManualStandAttemptCooldown;
        }
        else
        {
            _nextStandAttemptAt[uid] = _timing.CurTime + ManualStandAttemptCooldown;
        }
    }

    private bool IntersectingStandingColliders(EntityUid uid)
    {
        if (!TryComp(uid, out TransformComponent? xformComp))
            return false;

        var standingLayers = (int) (CollisionGroup.MidImpassable | CollisionGroup.HighImpassable);
        var fixtureQuery = GetEntityQuery<FixturesComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();
        var ourAabb = _entityLookup.GetAABBNoContainer(uid, xformComp.MapPosition.Position, xformComp.WorldRotation);

        var intersecting = _entityLookup.GetEntitiesIntersecting(xformComp.MapID, ourAabb, LookupFlags.Static | LookupFlags.Dynamic);

        foreach (var ent in intersecting)
        {
            if (ent == uid)
                continue;

            if (!fixtureQuery.TryGetComponent(ent, out var fixtures) || !xformQuery.TryComp(ent, out var xformOther))
                continue;

            var xform = new Transform(xformOther.MapPosition.Position, xformOther.WorldRotation);
            foreach (var fixture in fixtures.Fixtures.Values)
            {
                if (!fixture.Hard || (fixture.CollisionMask & standingLayers) == 0)
                    continue;

                for (var i = 0; i < fixture.Shape.ChildCount; i++)
                {
                    var intersection = fixture.Shape.ComputeAABB(xform, i).IntersectPercentage(ourAabb);
                    if (intersection > 0.1f)
                        return true;
                }
            }
        }

        return false;
    }

    private bool TryStanding(EntityUid uid, KnockedDownComponent? knocked = null, bool popupOnBlocked = false)
    {
        if (!Resolve(uid, ref knocked, false))
            return true;

        if (!KnockdownOver(uid, knocked) || _mobState.IsIncapacitated(uid))
            return false;

        var standEv = new StandUpAttemptEvent(knocked.AutoStand);
        RaiseLocalEvent(uid, ref standEv);

        if (standEv.Autostand != knocked.AutoStand)
            SetAutoStand(uid, knocked, standEv.Autostand);

        if (standEv.Message != null)
            _popup.PopupClient(standEv.Message.Value.Item1, uid, uid, standEv.Message.Value.Item2);

        if (standEv.Cancelled)
            return false;

        if (IntersectingStandingColliders(uid))
        {
            if (popupOnBlocked)
                _popup.PopupClient(Loc.GetString("knockdown-component-stand-no-room"), uid, uid, PopupType.SmallCaution);

            SetAutoStand(uid, knocked);
            ScheduleAutoStandRetry(uid, knocked);
            return false;
        }

        if (!TryComp(uid, out CrawlerComponent? crawler) || !_cfg.GetCVar(CCVars.MovementCrawling))
        {
            RemComp<KnockedDownComponent>(uid);
            return true;
        }

        if (knocked.DoAfterId != null)
            return false;

        var getUpTime = new GetStandUpTimeEvent(crawler.StandTime);
        RaiseLocalEvent(uid, ref getUpTime);

        var doAfter = new DoAfterArgs(EntityManager, uid, getUpTime.DoAfterTime, new TryStandDoAfterEvent(), uid, uid)
        {
            BreakOnDamage = true,
            DamageThreshold = 5f,
            CancelDuplicate = true,
            RequireCanInteract = false,
            BreakOnHandChange = true
        };

        if (!_doAfter.TryStartDoAfter(doAfter, out var id))
            return false;

        knocked.DoAfterId = id.Value.Index;
        if (TryComp(uid, out StandingStateComponent? standing))
        {
            standing.CurrentState = StandingState.GettingUp;
            Dirty(uid, standing);
        }

        Dirty(uid, knocked);
        return true;
    }

    public bool KnockdownOver(EntityUid uid, KnockedDownComponent? knocked = null)
    {
        if (!Resolve(uid, ref knocked, false))
            return true;

        return knocked.NextUpdate <= _timing.CurTime && _blocker.CanMove(uid);
    }

    public bool CanStand(EntityUid uid, KnockedDownComponent? knocked = null)
    {
        if (!Resolve(uid, ref knocked, false))
            return true;

        if (!KnockdownOver(uid, knocked))
            return false;

        var ev = new StandUpAttemptEvent();
        RaiseLocalEvent(uid, ref ev);
        return !ev.Cancelled && !IntersectingStandingColliders(uid);
    }

    private void OnStandDoAfter(EntityUid uid, KnockedDownComponent knocked, ref TryStandDoAfterEvent args)
    {
        knocked.DoAfterId = null;

        if (TryComp(uid, out StandingStateComponent? standing) &&
            standing.CurrentState == StandingState.GettingUp)
        {
            standing.CurrentState = StandingState.Lying;
            Dirty(uid, standing);
        }

        if (args.Cancelled || !KnockdownOver(uid, knocked) || _mobState.IsIncapacitated(uid))
        {
            Dirty(uid, knocked);
            return;
        }

        var standEv = new StandUpAttemptEvent(knocked.AutoStand);
        RaiseLocalEvent(uid, ref standEv);

        if (standEv.Autostand != knocked.AutoStand)
            SetAutoStand(uid, knocked, standEv.Autostand);

        if (standEv.Message != null)
            _popup.PopupClient(standEv.Message.Value.Item1, uid, uid, standEv.Message.Value.Item2);

        if (standEv.Cancelled)
        {
            Dirty(uid, knocked);
            return;
        }

        if (IntersectingStandingColliders(uid))
        {
            _popup.PopupClient(Loc.GetString("knockdown-component-stand-no-room"), uid, uid, PopupType.SmallCaution);
            ScheduleAutoStandRetry(uid, knocked);
            return;
        }

        RemComp<KnockedDownComponent>(uid);
    }

    private void OnForceStandup(ForceStandUpEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        ForceStandUp(user);
    }

    public void ForceStandUp(EntityUid uid, KnockedDownComponent? knocked = null)
    {
        if (!Resolve(uid, ref knocked, false))
            return;

        // If this fails now, keep trying to stand normally once the entity can.
        SetAutoStand(uid, knocked, true);

        if (StandingBlocked(uid, knocked))
            return;

        if (!_hands.TryGetEmptyHand(uid, out _))
            return;

        if (!TryForceStand(uid))
            return;

        CancelKnockdownDoAfter(uid, knocked);
        RemComp<KnockedDownComponent>(uid);

        _adminLogger.Add(LogType.Stamina, LogImpact.Medium,
            $"{ToPrettyString(uid):user} has force stood up from knockdown.");
    }

    private bool StandingBlocked(EntityUid uid, KnockedDownComponent knocked)
    {
        if (!KnockdownOver(uid, knocked))
            return true;

        var standEv = new StandUpAttemptEvent(knocked.AutoStand);
        RaiseLocalEvent(uid, ref standEv);

        if (standEv.Autostand != knocked.AutoStand)
            SetAutoStand(uid, knocked, standEv.Autostand);

        if (standEv.Message != null)
            _popup.PopupClient(standEv.Message.Value.Item1, uid, uid, standEv.Message.Value.Item2);

        if (standEv.Cancelled)
            return true;

        if (!IntersectingStandingColliders(uid))
            return false;

        _popup.PopupClient(Loc.GetString("knockdown-component-stand-no-room"), uid, uid, PopupType.SmallCaution);
        SetAutoStand(uid, knocked);
        return true;
    }

    private bool TryForceStand(EntityUid uid)
    {
        if (!TryComp(uid, out StaminaComponent? stamina))
            return false;

        var ev = new TryForceStandEvent(stamina.ForceStandStamina);
        RaiseLocalEvent(uid, ref ev);

        if (!_stamina.TryTakeStamina(uid, ev.Stamina, stamina, visual: true))
        {
            _popup.PopupClient(Loc.GetString("knockdown-component-pushup-failure"), uid, uid, PopupType.MediumCaution);
            return false;
        }

        _popup.PopupClient(Loc.GetString("knockdown-component-pushup-success"), uid, uid);
        _audio.PlayPredicted(stamina.ForceStandSuccessSound, uid, uid,
            AudioParams.Default.WithVariation(0.025f).WithVolume(5f));

        return true;
    }

    private void OnKnockedDownAlert(EntityUid uid, KnockedDownComponent component, ref KnockedDownAlertEvent args)
    {
        if (args.Handled)
            return;

        SetAutoStand(uid, component, true);

        if (!TryStanding(uid, component, popupOnBlocked: true))
            ForceStandUp(uid, component);

        args.Handled = true;
    }

    private void OnStandAttempt(EntityUid uid, KnockedDownComponent component, StandAttemptEvent args)
    {
        if (component.LifeStage <= ComponentLifeStage.Running)
            args.Cancel();
    }

    private void OnSlowInit(EntityUid uid, SlowedDownComponent component, ComponentInit args)
    {
        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
    }

    private void OnSlowRemove(EntityUid uid, SlowedDownComponent component, ComponentShutdown args)
    {
        component.SprintSpeedModifier = 1f;
        component.WalkSpeedModifier = 1f;
        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
    }

    private void OnRefreshMovespeed(EntityUid uid, SlowedDownComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.WalkSpeedModifier, component.SprintSpeedModifier);
    }

    // TODO STUN: Make events for different things. (Getting modifiers, attempt events, informative events...)

    /// <summary>
    ///     Stuns the entity, disallowing it from doing many interactions temporarily.
    /// </summary>
    public bool TryStun(EntityUid uid, TimeSpan time, bool refresh,
        StatusEffectsComponent? status = null)
    {
        time *= _modify.GetModifier(uid); // Goobstation

        if (time <= TimeSpan.Zero)
            return false;

        if (!Resolve(uid, ref status, false))
            return false;

        if (!_statusEffect.TryAddStatusEffect<StunnedComponent>(uid, "Stun", time, refresh))
            return false;

        var ev = new StunnedEvent();
        RaiseLocalEvent(uid, ref ev);
        RaiseLocalEvent(uid, new DropHandItemsEvent(), false);

        _adminLogger.Add(LogType.Stamina, LogImpact.Medium, $"{ToPrettyString(uid):user} stunned for {time.Seconds} seconds");
        return true;
    }

    public bool TryCrawling(EntityUid uid, bool refresh = true, bool autoStand = true, bool drop = true, bool force = false)
    {
        return TryCrawling(uid, null, refresh, autoStand, drop, force);
    }

    public void TrySetKnockedDownFrictionModifier(EntityUid uid, float frictionModifier, KnockedDownComponent? knocked = null)
    {
        if (!Resolve(uid, ref knocked, false))
            return;

        var newModifier = Math.Min(knocked.FrictionModifier, frictionModifier);
        if (MathHelper.CloseTo(knocked.FrictionModifier, newModifier))
            return;

        knocked.FrictionModifier = newModifier;
        Dirty(uid, knocked);
    }

    public bool TryCrawling(EntityUid uid,
        TimeSpan? time,
        bool refresh = true,
        bool autoStand = true,
        bool drop = true,
        bool force = false)
    {
        if (!TryComp(uid, out CrawlerComponent? crawler))
            return false;

        if (time == null)
            time = crawler.DefaultKnockedDuration;

        return TryKnockdown(uid, time.Value, refresh, autoStand: autoStand, drop: drop, force: force);
    }

    /// <summary>
    ///     Knocks down the entity, making it fall to the ground.
    /// </summary>
    public bool TryKnockdown(EntityUid uid, TimeSpan time, bool refresh,
        StatusEffectsComponent? status = null,
        bool autoStand = true,
        bool drop = true,
        bool force = false)
    {
        time *= _modify.GetModifier(uid); // Goobstation

        if (time <= TimeSpan.Zero)
            return false;

        if (!CanKnockdown(uid, ref time, ref autoStand, ref drop, force))
            return false;

        if (HasComp<CrawlerComponent>(uid) && _cfg.GetCVar(CCVars.MovementCrawling))
        {
            Knockdown(uid, time, refresh, autoStand, drop);
            return true;
        }

        if (!Resolve(uid, ref status, false) ||
            !_statusEffect.TryAddStatusEffect<KnockedDownComponent>(uid, "KnockedDown", time, refresh, status))
            return false;

        if (TryComp(uid, out KnockedDownComponent? knocked))
        {
            SetAutoStand(uid, knocked, autoStand);
            UpdateKnockdownTime(uid, time, refresh, knocked);
        }

        var ev = new KnockedDownEvent();
        RaiseLocalEvent(uid, ref ev);

        return true;
    }

    private bool CanKnockdown(EntityUid uid,
        ref TimeSpan time,
        ref bool autoStand,
        ref bool drop,
        bool force = false)
    {
        if (time <= TimeSpan.Zero)
            return false;

        if (!HasComp<StandingStateComponent>(uid) || (!force && _gravity.IsWeightless(uid)))
            return false;

        var attempt = new KnockDownAttemptEvent(autoStand, drop, time);
        RaiseLocalEvent(uid, ref attempt);

        autoStand = attempt.AutoStand;
        drop = attempt.Drop;
        if (attempt.Time is { } newTime)
            time = newTime;

        return time > TimeSpan.Zero && (force || !attempt.Cancelled);
    }

    private void Knockdown(EntityUid uid, TimeSpan time, bool refresh, bool autoStand, bool drop)
    {
        var hadKnocked = TryComp(uid, out KnockedDownComponent? knocked);

        if (!hadKnocked)
        {
            if (drop)
                RaiseLocalEvent(uid, new DropHandItemsEvent(), false);

            knocked = AddComp<KnockedDownComponent>(uid);
        }
        else
        {
            CancelKnockdownDoAfter(uid, knocked);
            RefreshKnockedMovement(uid, knocked!);
        }

        SetAutoStand(uid, knocked, autoStand);
        UpdateKnockdownTime(uid, time, refresh, knocked);

        var ev = new KnockedDownEvent();
        RaiseLocalEvent(uid, ref ev);

        _adminLogger.Add(LogType.Stamina, LogImpact.Medium,
            $"{ToPrettyString(uid):user} was knocked down for {time.TotalSeconds} seconds");
    }

    /// <summary>
    ///     Applies knockdown and stun to the entity temporarily.
    /// </summary>
    public bool TryParalyze(EntityUid uid, TimeSpan time, bool refresh,
        StatusEffectsComponent? status = null)
    {
        if (!Resolve(uid, ref status, false))
            return false;

        return TryKnockdown(uid, time, refresh, status) && TryStun(uid, time, refresh, status);
    }

    /// <summary>
    ///     Slows down the mob's walking/running speed temporarily
    /// </summary>
    public bool TrySlowdown(EntityUid uid, TimeSpan time, bool refresh,
        float walkSpeedMultiplier = 1f, float runSpeedMultiplier = 1f,
        StatusEffectsComponent? status = null)
    {
        if (!Resolve(uid, ref status, false))
            return false;

        if (time <= TimeSpan.Zero)
            return false;

        if (_statusEffect.TryAddStatusEffect<SlowedDownComponent>(uid, "SlowedDown", time, refresh, status))
        {
            var slowed = Comp<SlowedDownComponent>(uid);
            // Doesn't make much sense to have the "TrySlowdown" method speed up entities now does it?
            walkSpeedMultiplier = Math.Clamp(walkSpeedMultiplier, 0f, 1f);
            runSpeedMultiplier = Math.Clamp(runSpeedMultiplier, 0f, 1f);

            slowed.WalkSpeedModifier *= walkSpeedMultiplier;
            slowed.SprintSpeedModifier *= runSpeedMultiplier;

            _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);

            return true;
        }

        return false;
    }

    private void OnKnockedTileFriction(EntityUid uid, KnockedDownComponent component, ref TileFrictionEvent args)
    {
        args.Modifier *= component.FrictionModifier;
    }

    #region Attempt Event Handling

    private void OnMoveAttempt(EntityUid uid, StunnedComponent stunned, UpdateCanMoveEvent args)
    {
        if (stunned.LifeStage > ComponentLifeStage.Running)
            return;

        args.Cancel();
    }

    private void OnAttempt(EntityUid uid, StunnedComponent stunned, CancellableEntityEventArgs args)
    {
        args.Cancel();
    }

    private void OnEquipAttempt(EntityUid uid, StunnedComponent stunned, IsEquippingAttemptEvent args)
    {
        // is this a self-equip, or are they being stripped?
        if (args.Equipee == uid)
            args.Cancel();
    }

    private void OnUnequipAttempt(EntityUid uid, StunnedComponent stunned, IsUnequippingAttemptEvent args)
    {
        // is this a self-equip, or are they being stripped?
        if (args.Unequipee == uid)
            args.Cancel();
    }

    #endregion
}

/// <summary>
///     Raised directed on an entity when it is stunned.
/// </summary>
[ByRefEvent]
public record struct StunnedEvent;

/// <summary>
///     Raised directed on an entity when it is knocked down.
/// </summary>
[ByRefEvent]
public record struct KnockedDownEvent;

[ByRefEvent]
public record struct KnockDownAttemptEvent(bool AutoStand, bool Drop, TimeSpan? Time)
{
    public bool Cancelled;
}

[ByRefEvent]
public record struct StandUpAttemptEvent(bool Autostand)
{
    public bool Cancelled;
    public (string, PopupType)? Message;
}

[ByRefEvent]
public record struct GetStandUpTimeEvent(TimeSpan DoAfterTime);

[ByRefEvent]
public record struct TryForceStandEvent(float Stamina);

public sealed partial class KnockedDownAlertEvent : BaseAlertEvent;

[ByRefEvent]
public record struct KnockedDownRefreshEvent()
{
    public float SpeedModifier = 1f;
    public float FrictionModifier = 1f;
}


[ByRefEvent]
[Serializable, NetSerializable]
public sealed partial class TryStandDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed class ForceStandUpEvent : EntityEventArgs;
