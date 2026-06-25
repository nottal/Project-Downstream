// SPDX-FileCopyrightText: 2025 Eris <erisfiregamer1@gmail.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using Robust.Shared.GameStates;

namespace Content.Shared.Changeling;

[RegisterComponent, NetworkedComponent]
public sealed partial class ChangelingInfectionImplantComponent : Component
{
    [DataField]
    public EntityUid? HolderUid;

    [DataField]
    public EntityUid? ImplanterUid;
}
