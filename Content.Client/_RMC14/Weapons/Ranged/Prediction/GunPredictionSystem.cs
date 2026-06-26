// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Aiden <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 DrSmugleaf <10968691+DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Toaster <mrtoastymyroasty@gmail.com>
// SPDX-FileCopyrightText: 2025 Toastermeister <215405651+Toastermeister@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Numerics;
using Content.Client.Projectiles;
using Content.Shared._RMC14.Weapons.Ranged.Prediction;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Fluids.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Physics;
using Robust.Client.Player;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Weapons.Ranged.Prediction;

public sealed class GunPredictionSystem : SharedGunPredictionSystem
{
    public const string ProjectileFixture = "projectile";
    private const float MinimumSweepDistance = 0.01f;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly ProjectileSystem _projectile = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<IgnorePredictionHideComponent> _ignorePredictionHideQuery;
    private EntityQuery<SpriteComponent> _spriteQuery;

    public override void Initialize()
    {
        base.Initialize();

        _ignorePredictionHideQuery = GetEntityQuery<IgnorePredictionHideComponent>();
        _spriteQuery = GetEntityQuery<SpriteComponent>();

        SubscribeLocalEvent<PhysicsUpdateBeforeSolveEvent>(OnBeforeSolve);
        SubscribeLocalEvent<PhysicsUpdateAfterSolveEvent>(OnAfterSolve);
        SubscribeLocalEvent<RequestShootEvent>(OnShootRequest);

        SubscribeLocalEvent<PredictedProjectileClientComponent, UpdateIsPredictedEvent>(OnClientProjectileUpdateIsPredicted);
        SubscribeLocalEvent<PredictedProjectileClientComponent, StartCollideEvent>(OnClientProjectileStartCollide);

        SubscribeLocalEvent<PredictedProjectileServerComponent, ComponentStartup>(OnServerProjectileStartup);

        UpdatesBefore.Add(typeof(TransformSystem));
    }

    private void OnBeforeSolve(ref PhysicsUpdateBeforeSolveEvent ev)
    {
        var query = EntityQueryEnumerator<PredictedProjectileClientComponent>();
        while (query.MoveNext(out var uid, out var predicted))
        {
            predicted.Coordinates = Transform(uid).Coordinates;
        }
    }

    private void OnAfterSolve(ref PhysicsUpdateAfterSolveEvent ev)
    {
        var query = EntityQueryEnumerator<PredictedProjectileClientComponent>();
        while (query.MoveNext(out var uid, out var predicted))
        {
            if (_timing.IsFirstTimePredicted)
                continue;

            if (predicted.Coordinates is { } coordinates)
                _transform.SetCoordinates(uid, coordinates);

            predicted.Coordinates = null;
        }
    }

    private void OnShootRequest(RequestShootEvent ev, EntitySessionEventArgs args)
    {
        if (_timing.IsFirstTimePredicted)
            return;

        _gun.ShootRequested(ev.Gun, ev.Coordinates, ev.Target, null, args.SenderSession);
    }

    private void OnClientProjectileUpdateIsPredicted(Entity<PredictedProjectileClientComponent> ent, ref UpdateIsPredictedEvent args)
    {
        args.IsPredicted = true;
    }

    private void PredictHit(EntityUid projectileUid, ProjectileComponent projectile, PhysicsComponent physics, EntityUid hit)
    {
        var netEnt = GetNetEntity(hit);
        var pos = _transform.GetMapCoordinates(hit);
        var hits = new HashSet<(NetEntity, MapCoordinates)> { (netEnt, pos) };
        var ev = new PredictedProjectileHitEvent(projectileUid.Id, hits);
        RaiseNetworkEvent(ev);

        _projectile.ProjectileCollide((projectileUid, projectile, physics), hit);
    }

    private bool TryGetProjectileMask(EntityUid uid, out int projectileMask)
    {
        projectileMask = 0;
        if (!TryComp<FixturesComponent>(uid, out var fixtures) ||
            !fixtures.Fixtures.TryGetValue(ProjectileFixture, out var projectileFixture))
        {
            return false;
        }

        projectileMask = projectileFixture.CollisionMask;
        return true;
    }

    private bool IsValidPredictedHit(
        EntityUid projectileUid,
        ProjectileComponent projectile,
        int projectileMask,
        EntityUid contact)
    {
        if (contact == projectileUid)
            return false;

        if (projectile.IgnoreShooter && (contact == projectile.Shooter || contact == projectile.Weapon))
            return false;

        if (HasComp<PuddleComponent>(contact))
            return false;

        if (!TryComp<PhysicsComponent>(contact, out _))
            return false;

        if (!TryComp<FixturesComponent>(contact, out var contactFixtures))
            return false;

        if (TryComp<RequireProjectileTargetComponent>(contact, out var requireTarget) &&
            requireTarget.Active &&
            CompOrNull<TargetedProjectileComponent>(projectileUid)?.Target != contact)
        {
            return false;
        }

        var isAnchored = false;
        if (TryComp<TransformComponent>(contact, out var contactXform))
            isAnchored = contactXform.Anchored;

        if (!isAnchored)
        {
            var canBeHit = HasComp<DamageableComponent>(contact) ||
                           HasComp<MobStateComponent>(contact);

            if (!canBeHit)
                return false;
        }

        foreach (var fixture in contactFixtures.Fixtures.Values)
        {
            if (!fixture.Hard)
                continue;

            if ((fixture.CollisionLayer & projectileMask) == 0)
                continue;

            return true;
        }

        return false;
    }

    private bool TryPredictSweptHit(
        EntityUid uid,
        PredictedProjectileClientComponent predicted,
        ProjectileComponent projectile,
        PhysicsComponent physics,
        int projectileMask,
        out EntityUid hit)
    {
        hit = default;

        if (predicted.Coordinates is not { } previousCoordinates)
            return false;

        var previous = _transform.ToMapCoordinates(previousCoordinates);
        var current = _transform.GetMapCoordinates(uid);
        if (previous.MapId != current.MapId)
            return false;

        var delta = current.Position - previous.Position;
        var distance = delta.Length();
        if (distance <= MinimumSweepDistance)
            return false;

        var ray = new CollisionRay(previous.Position, delta / distance, projectileMask);
        var results = _physics.IntersectRayWithPredicate(
            previous.MapId,
            ray,
            (Projectile: uid, Shooter: projectile.Shooter, Weapon: projectile.Weapon),
            static (ent, ignored) => ent == ignored.Projectile ||
                                     ent == ignored.Shooter ||
                                     ent == ignored.Weapon,
            distance,
            false);

        foreach (var result in results)
        {
            if (!IsValidPredictedHit(uid, projectile, projectileMask, result.HitEntity))
                continue;

            hit = result.HitEntity;
            return true;
        }

        return false;
    }

    private void OnClientProjectileStartCollide(Entity<PredictedProjectileClientComponent> ent, ref StartCollideEvent args)
{
    if (ent.Comp.Hit)
        return;

    if (!TryComp(ent, out ProjectileComponent? projectile) ||
        !TryComp(ent, out PhysicsComponent? physics))
    {
        return;
    }

    // Skip collision with shooter and weapon if IgnoreShooter is true
    if (args.OurFixtureId != ProjectileFixture || !args.OtherFixture.Hard ||
        projectile.DamagedEntity || projectile is { Weapon: null, OnlyCollideWhenShot: true })
        return;

    // Skip puddles - they should never be hit by projectiles
    if (HasComp<PuddleComponent>(args.OtherEntity))
        return;

    // Check if contact has physics component
    if (!TryComp<PhysicsComponent>(args.OtherEntity, out var contactPhysics))
        return;

    // Check if contact is anchored for directional filtering
    var isAnchored = false;
    if (TryComp<TransformComponent>(args.OtherEntity, out var contactXform))
        isAnchored = contactXform.Anchored;

    // Additional filtering for non-anchored entities - match Update() logic
    if (!isAnchored)
    {
        // Only hit non-anchored entities if they can be damaged or are mobs
        var canBeHit = HasComp<DamageableComponent>(args.OtherEntity) ||
                       HasComp<MobStateComponent>(args.OtherEntity);

        if (!canBeHit)
            return;
    }

    // For anchored entities (walls, fixtures), check if they're in the direction of travel
    if (isAnchored && physics.LinearVelocity.LengthSquared() > 0.01f)
    {
        var projectileMapCoords = _transform.GetMapCoordinates(ent);
        var contactMapCoords = _transform.GetMapCoordinates(args.OtherEntity);
        var toContact = contactMapCoords.Position - projectileMapCoords.Position;

        var toContactNormalized = toContact.Normalized();
        var velocityNormalized = physics.LinearVelocity.Normalized();
        var dot = Vector2.Dot(toContactNormalized, velocityNormalized);

        // Only collide with anchored entities if they're in front
        if (dot < 0.3f)
            return;
    }

    var netEnt = GetNetEntity(args.OtherEntity);
    var pos = _transform.GetMapCoordinates(args.OtherEntity);
    var hit = new HashSet<(NetEntity, MapCoordinates)> { (netEnt, pos) };
    var ev = new PredictedProjectileHitEvent(ent.Owner.Id, hit);
    RaiseNetworkEvent(ev);

    _projectile.ProjectileCollide((ent, projectile, physics), args.OtherEntity);
}

    private void OnServerProjectileStartup(Entity<PredictedProjectileServerComponent> ent, ref ComponentStartup args)
    {
        if (!GunPrediction)
            return;

        if (ent.Comp.ClientEnt != _player.LocalEntity)
            return;

        if (_ignorePredictionHideQuery.HasComp(ent))
            return;

        if (_spriteQuery.TryComp(ent, out var sprite))
            sprite.Visible = false;
    }

public override void Update(float frameTime)
{
    base.Update(frameTime);

    if (!_timing.IsFirstTimePredicted)
        return;

    // TODO gun prediction remove this once the client reliably detects collisions
    var projectiles = EntityQueryEnumerator<PredictedProjectileClientComponent, ProjectileComponent, PhysicsComponent>();
    while (projectiles.MoveNext(out var uid, out var predicted, out var projectile, out var physics))
    {
        if (predicted.Hit)
            continue;

        if (!TryGetProjectileMask(uid, out var projectileMask))
            continue;

        if (TryPredictSweptHit(uid, predicted, projectile, physics, projectileMask, out var sweptHit))
        {
            PredictHit(uid, projectile, physics, sweptHit);
            continue;
        }

        var contacts = _physics.GetContactingEntities(uid, physics, true);
        if (contacts.Count == 0)
            continue;

        // Get projectile position and velocity for directional checking
        var projectileMapCoords = _transform.GetMapCoordinates(uid);
        var projectileVelocity = physics.LinearVelocity;
        var hasVelocity = projectileVelocity.LengthSquared() > 0.01f;

        // Filter contacts - matching server-side logic from SharedProjectileSystem.OnStartCollide
        var filteredContacts = new List<EntityUid>();
        foreach (var contact in contacts)
        {
            // Skip shooter and weapon to prevent immediate collision at spawn point
            if (!IsValidPredictedHit(uid, projectile, projectileMask, contact))
                continue;

            // Check if contact is anchored
            var isAnchored = false;
            if (TryComp<TransformComponent>(contact, out var contactXform))
                isAnchored = contactXform.Anchored;

            // For anchored entities (walls, fixtures), check if they're in the direction of travel
            // This prevents hitting walls behind the shooter
            if (hasVelocity && isAnchored)
            {
                var contactMapCoords = _transform.GetMapCoordinates(contact);
                var toContact = contactMapCoords.Position - projectileMapCoords.Position;

                // Calculate dot product to check if contact is in front of projectile
                var toContactNormalized = toContact.Normalized();
                var velocityNormalized = projectileVelocity.Normalized();
                var dot = Vector2.Dot(toContactNormalized, velocityNormalized);

                // Only collide with anchored entities if they're in front
                if (dot < 0.3f)
                    continue;
            }

            filteredContacts.Add(contact);
        }

        if (filteredContacts.Count == 0)
            continue;

        var hit = new HashSet<(NetEntity, MapCoordinates)>();
        foreach (var contact in filteredContacts)
        {
            var netEnt = GetNetEntity(contact);
            var pos = _transform.GetMapCoordinates(contact);
            hit.Add((netEnt, pos));
        }

        var ev = new PredictedProjectileHitEvent(uid.Id, hit);
        RaiseNetworkEvent(ev);

        _projectile.ProjectileCollide((uid, projectile, physics), filteredContacts.First());
    }

    var predictedQuery = EntityQueryEnumerator<PredictedProjectileHitComponent, SpriteComponent, TransformComponent>();
    while (predictedQuery.MoveNext(out var hit, out var sprite, out var xform))
    {
        var origin = hit.Origin;
        var coordinates = xform.Coordinates;
        if (!origin.TryDistance(EntityManager, _transform, coordinates, out var distance) ||
            distance >= hit.Distance)
        {
            sprite.Visible = false;
        }
    }
}

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        // TODO bullet prediction remove this when lerping doesnt make the client's entity slightly slower
        var projectiles = EntityQueryEnumerator<PredictedProjectileClientComponent, TransformComponent>();
        while (projectiles.MoveNext(out _, out var xform))
        {
            xform.ActivelyLerping = false;
        }
    }
}
