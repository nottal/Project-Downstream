// SPDX-FileCopyrightText: 2026
//
// SPDX-License-Identifier: MIT

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent]
public sealed partial class FugitiveHunterCaptureQuotaConditionComponent : Component
{
    [DataField]
    public string TitleLoc = "fugitive-hunter-capture-quota-title";
}
