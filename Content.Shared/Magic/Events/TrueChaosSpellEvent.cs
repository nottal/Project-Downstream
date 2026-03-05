// SPDX-FileCopyrightText: 2026 OpenAI
//
// SPDX-License-Identifier: MIT

using Content.Shared.Actions;

namespace Content.Shared.Magic.Events;

public sealed partial class TrueChaosSpellEvent : InstantActionEvent, ISpeakSpell
{
    [DataField]
    public string? Speech { get; set; }
}
