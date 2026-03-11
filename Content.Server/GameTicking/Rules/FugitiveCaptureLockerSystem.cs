// SPDX-FileCopyrightText: 2026 OpenAI
//
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Ghost;
using Content.Server.Popups;
using Content.Server.Storage.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;

namespace Content.Server.GameTicking.Rules;

public sealed class FugitiveCaptureLockerSystem : EntitySystem
{
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FugitiveCaptureLockerComponent, GetVerbsEvent<InteractionVerb>>(OnCaptureVerb);
    }

    private void OnCaptureVerb(Entity<FugitiveCaptureLockerComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!TryComp<EntityStorageComponent>(ent, out var storage) || storage.Open)
            return;

        var user = args.User;
        args.Verbs.Add(new InteractionVerb
        {
            Text = Loc.GetString("fugitive-capture-locker-confirm"),
            Priority = 5,
            Act = () => ConfirmCapture(ent.Owner, user, storage)
        });
    }

    private void ConfirmCapture(EntityUid locker, EntityUid user, EntityStorageComponent storage)
    {
        var occupants = new List<EntityUid>(storage.Contents.ContainedEntities);
        var capturedAny = false;
        foreach (var occupant in occupants)
        {
            if (!TryComp<FugitiveCaptureTargetComponent>(occupant, out var target) || target.Captured)
                continue;

            var hasMind = TryGetCaptureTargetMind(occupant, out var fugitiveMind);

            target.Captured = true;

            if (hasMind)
                _ghost.SpawnGhost(fugitiveMind, spawnPosition: Transform(locker).Coordinates, canReturn: false);
            QueueDel(occupant);

            var ev = new FugitiveCapturedEvent(occupant, hasMind ? fugitiveMind.Owner : null);
            RaiseLocalEvent(ev);
            capturedAny = true;
        }

        if (capturedAny)
            _popup.PopupEntity(Loc.GetString("fugitive-capture-locker-success"), locker, user);
        else
            _popup.PopupEntity(Loc.GetString("fugitive-capture-locker-incorrect-target"), locker, user);
    }

    private bool TryGetCaptureTargetMind(EntityUid occupant, out Entity<MindComponent?> fugitiveMind)
    {
        fugitiveMind = default;

        if (!_mind.TryGetMind(occupant, out var mindId, out var mind))
            return false;
		
        fugitiveMind = (mindId, mind);
        return true;
    }
}

public readonly record struct FugitiveCapturedEvent(EntityUid FugitiveEntityUid, EntityUid? FugitiveMindId);
