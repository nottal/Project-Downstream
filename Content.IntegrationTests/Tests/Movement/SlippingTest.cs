// SPDX-FileCopyrightText: 2023 TemporalOroboros <TemporalOroboros@gmail.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Aiden <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2024 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: MIT

#nullable enable
using System.Collections.Generic;
using Content.Shared.Alert;
using Content.Shared.Damage.Components;
using Content.Shared.Hands.Components;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Movement.Components;
using Content.Shared.Slippery;
using Content.Shared.Stunnable;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Maths;
using ServerStunSystem = Content.Server.Stunnable.StunSystem;

namespace Content.IntegrationTests.Tests.Movement;

public sealed class SlippingTest : MovementTest
{
    public sealed class SlipTestSystem : EntitySystem
    {
        public HashSet<EntityUid> Slipped = new();
        public override void Initialize()
        {
            SubscribeLocalEvent<SlipperyComponent, SlipEvent>(OnSlip);
        }

        private void OnSlip(EntityUid uid, SlipperyComponent component, ref SlipEvent args)
        {
            Slipped.Add(args.Slipped);
        }
    }

    [Test]
    public async Task BananaSlipTest()
    {
        var sys = SEntMan.System<SlipTestSystem>();
        await SpawnTarget("TrashBananaPeel");

        var modifier = Comp<MovementSpeedModifierComponent>(Player).SprintSpeedModifier;
        Assert.That(modifier, Is.EqualTo(1), "Player is not moving at full speed.");

        // Player is to the left of the banana peel and has not slipped.
        Assert.That(Delta(), Is.GreaterThan(0.5f));
        Assert.That(sys.Slipped, Does.Not.Contain(SEntMan.GetEntity(Player)));

        // Walking over the banana slowly does not trigger a slip.
        await SetKey(EngineKeyFunctions.Walk, BoundKeyState.Down);
        await Move(DirectionFlag.East, 1f);
        Assert.That(Delta(), Is.LessThan(0.5f));
        Assert.That(sys.Slipped, Does.Not.Contain(SEntMan.GetEntity(Player)));
        AssertComp<KnockedDownComponent>(false, Player);

        // Moving at normal speeds does trigger a slip.
        await SetKey(EngineKeyFunctions.Walk, BoundKeyState.Up);
        await Move(DirectionFlag.West, 1f);
        Assert.That(sys.Slipped, Does.Contain(SEntMan.GetEntity(Player)));
        AssertComp<KnockedDownComponent>(true, Player);
    }

    [Test]
    public async Task SlipMiniStunExpiresBeforeCrawl()
    {
        await SpawnTarget("TrashBananaPeel");

        var player = SEntMan.GetEntity(Player);
        var peel = SEntMan.GetEntity(Target!.Value);
        var slippery = SEntMan.System<SlipperySystem>();

        await Server.WaitPost(() =>
        {
            var slip = SEntMan.GetComponent<SlipperyComponent>(peel);
            slippery.TrySlip(peel, slip, player, force: true);
        });
        await RunTicks(1);

        AssertComp<StunnedComponent>(true, Player);
        AssertComp<KnockedDownComponent>(true, Player);

        await RunSeconds(0.75f);

        AssertComp<StunnedComponent>(false, Player);
        AssertComp<KnockedDownComponent>(true, Player);

        var alerts = SEntMan.System<AlertsSystem>();
        var slipData = SEntMan.GetComponent<SlipperyComponent>(peel).SlipData;
        var crawler = SEntMan.GetComponent<CrawlerComponent>(player);
        var hands = SEntMan.GetComponent<HandsComponent>(player);
        var expectedStandTime = crawler.StandTime * hands.Count / (hands.CountFreeHands() + hands.Count);

        Assert.That(alerts.IsShowingAlert(player, SharedStunSystem.KnockdownAlert), Is.True);

        var remainingKnockdown = (float) (slipData.KnockdownTime - TimeSpan.FromSeconds(0.75)).TotalSeconds;
        await RunSeconds(Math.Max(0.25f, remainingKnockdown + 0.25f));

        AssertComp<KnockedDownComponent>(true, Player);
        Assert.That(SEntMan.GetComponent<KnockedDownComponent>(player).DoAfterId, Is.Not.Null);

        await RunSeconds((float) expectedStandTime.TotalSeconds + 0.5f);

        AssertComp<KnockedDownComponent>(false, Player);
        Assert.That(alerts.IsShowingAlert(player, SharedStunSystem.KnockdownAlert), Is.False);
    }

    [Test]
    public async Task ForceStandCancelsGetUpDoAfter()
    {
        await SpawnTarget("TrashBananaPeel");

        var player = SEntMan.GetEntity(Player);
        var peel = SEntMan.GetEntity(Target!.Value);
        var slippery = SEntMan.System<SlipperySystem>();
        var stun = SEntMan.System<ServerStunSystem>();

        await Server.WaitPost(() =>
        {
            var slip = SEntMan.GetComponent<SlipperyComponent>(peel);
            slippery.TrySlip(peel, slip, player, force: true);
        });
        await RunTicks(1);

        var slipData = SEntMan.GetComponent<SlipperyComponent>(peel).SlipData;
        await RunSeconds((float) slipData.KnockdownTime.TotalSeconds + 0.25f);

        AssertComp<KnockedDownComponent>(true, Player);
        Assert.That(SEntMan.GetComponent<KnockedDownComponent>(player).DoAfterId, Is.Not.Null);

        var staminaBefore = SEntMan.GetComponent<StaminaComponent>(player).StaminaDamage;

        await Server.WaitPost(() => stun.ForceStandUp(player));
        await RunTicks(1);

        AssertComp<KnockedDownComponent>(false, Player);
        Assert.That(SEntMan.GetComponent<StaminaComponent>(player).StaminaDamage, Is.GreaterThan(staminaBefore));
    }
}

