// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Visne <39844191+Visne@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <metalgearsloth@gmail.com>
// SPDX-FileCopyrightText: 2025 Tay <td12233a@gmail.com>
// SPDX-FileCopyrightText: 2025 pa.pecherskij <pa.pecherskij@interfax.ru>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.Audio;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Weather;

[Prototype]
public sealed partial class WeatherPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [ViewVariables(VVAccess.ReadWrite), DataField("sprite", required: true)]
    public SpriteSpecifier Sprite = default!;

    [ViewVariables(VVAccess.ReadWrite), DataField("color")]
    public Color? Color;

    /// <summary>
    /// Sound to play on the affected areas.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("sound")]
    public SoundSpecifier? Sound;

    /// <summary>
    /// Optional damage applied by this weather while an entity is on an affected tile.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("damage")]
    public DamageSpecifier? Damage;

    /// <summary>
    /// Delay in seconds between weather damage ticks.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("damageInterval")]
    public float DamageInterval = 1f;
}
