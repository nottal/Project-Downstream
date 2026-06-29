// SPDX-FileCopyrightText: 2020 Remie Richards <remierichards@gmail.com>
// SPDX-FileCopyrightText: 2021 Acruid <shatter66@gmail.com>
// SPDX-FileCopyrightText: 2021 Clyybber <darkmine956@gmail.com>
// SPDX-FileCopyrightText: 2021 Vera Aguilera Puerto <6766154+Zumorica@users.noreply.github.com>
// SPDX-FileCopyrightText: 2021 Vera Aguilera Puerto <gradientvera@outlook.com>
// SPDX-FileCopyrightText: 2021 mirrorcult <notzombiedude@gmail.com>
// SPDX-FileCopyrightText: 2022 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 Rane <60792108+Elijahrane@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 mirrorcult <lunarautomaton6@gmail.com>
// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Visne <39844191+Visne@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Tadeo <td12233a@gmail.com>
// SPDX-FileCopyrightText: 2024 TemporalOroboros <TemporalOroboros@gmail.com>
// SPDX-FileCopyrightText: 2024 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 whateverusername0 <whateveremail>
// SPDX-FileCopyrightText: 2025 Tay <td12233a@gmail.com>
// SPDX-FileCopyrightText: 2025 slarticodefast <161409025+slarticodefast@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: MIT

using System.Numerics;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Standing;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Components;
using Robust.Shared.Maths;
using Robust.Shared.Random;

namespace Content.Server.Standing;

public sealed class StandingStateSystem : EntitySystem
{
    private const float DropHeldItemsSpread = 45f;

    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly ThrowingSystem _throwingSystem = default!;

    private void FallOver(EntityUid uid, StandingStateComponent component, DropHandItemsEvent args)
    {
        var holderVelocity = EntityManager.TryGetComponent(uid, out PhysicsComponent? comp) ? comp.LinearVelocity : Vector2.Zero;
        var spreadMaxAngle = Angle.FromDegrees(DropHeldItemsSpread);

        var fellEvent = new FellDownEvent(uid);
        RaiseLocalEvent(uid, fellEvent, false);

        if (!TryComp(uid, out HandsComponent? handsComp))
            return;

        foreach (var hand in handsComp.Hands.Values)
        {
            if (hand.HeldEntity is not EntityUid held)
                continue;

            var throwAttempt = new FellDownThrowAttemptEvent(uid);
            RaiseLocalEvent(held, ref throwAttempt);
            if (throwAttempt.Cancelled)
                continue;

            if (!_handsSystem.TryDrop(uid, hand, null, checkActionBlocker: false, handsComp: handsComp))
                continue;

            var angleOffset = _random.NextAngle(-spreadMaxAngle, spreadMaxAngle);
            var itemVelocity = angleOffset.RotateVec(holderVelocity);
            itemVelocity *= _random.NextFloat(1f);
            itemVelocity *= EntityManager.TryGetComponent(held, out PhysicsComponent? heldPhysics) ? heldPhysics.InvMass : 0f;
            var throwSpeed = handsComp.BaseThrowspeed * _random.NextFloat(0.45f, 0.55f);

            _throwingSystem.TryThrow(held,
                itemVelocity,
                throwSpeed,
                uid,
                pushbackRatio: 0,
                compensateFriction: false);
        }
    }

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StandingStateComponent, DropHandItemsEvent>(FallOver);
    }

}

/// <summary>
/// Raised after an entity falls down.
/// </summary>
public sealed class FellDownEvent : EntityEventArgs
{
    public EntityUid Uid { get; }
    public FellDownEvent(EntityUid uid)
    {
        Uid = uid;
    }
}
