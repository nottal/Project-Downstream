// SPDX-FileCopyrightText: 2025 GabyChangelog <agentepanela2@gmail.com>
// SPDX-FileCopyrightText: 2025 Kyoth25f <kyoth25f@gmail.com>
//
// SPDX-License-Identifier: MIT

using Content.Shared.Inventory;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chat.RadioIconsEvents;

/// <summary>
///     Raised whenever a radio message is sent so equipped items can override the displayed job icon.
/// </summary>
public sealed class TransformSpeakerJobIconEvent(EntityUid sender, ProtoId<JobIconPrototype> jobIcon, string? jobName)
    : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = SlotFlags.WITHOUT_POCKET;
    public EntityUid Sender = sender;
    public ProtoId<JobIconPrototype> JobIcon = jobIcon;
    public string? JobName = jobName;
}
