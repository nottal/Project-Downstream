// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Roudenn <romabond091@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.FixedPoint;
using Content.Shared.Damage;
using JetBrains.Annotations;

namespace Content.Shared._Lavaland.MobPhases;

public sealed class MobPhasesSystem : EntitySystem
{
    private readonly List<KeyValuePair<FixedPoint2, int>> _thresholdScratch = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobPhasesComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<MobPhasesComponent, DamageChangedEvent>(OnDamage);
    }

    private void OnInit(Entity<MobPhasesComponent> ent, ref MapInitEvent args)
        => ent.Comp.PhaseThresholds = new Dictionary<FixedPoint2, int>(ent.Comp.BasePhaseThresholds);

    private void OnDamage(Entity<MobPhasesComponent> ent, ref DamageChangedEvent args)
        => UpdatePhases(ent.Owner);

    /// <summary>
    /// Updates current phase according to its thresholds.
    /// </summary>
    [PublicAPI]
    public void UpdatePhases(Entity<MobPhasesComponent?, DamageableComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp1, ref ent.Comp2, false))
            return;

        var ai = ent.Comp1;
        var damageable = ent.Comp2;
        FixedPoint2? bestThreshold = null;
        var bestPhase = ai.CurrentPhase;

        foreach (var (threshold, phase) in ai.PhaseThresholds)
        {
            if (damageable.TotalDamage < threshold)
                continue;

            if (phase < ai.CurrentPhase
                && !ai.CanSwitchBack)
                continue;

            if (bestThreshold != null && threshold <= bestThreshold.Value)
                continue;

            bestThreshold = threshold;
            bestPhase = phase;
        }

        if (bestThreshold != null)
            ai.CurrentPhase = bestPhase;
    }

    /// <summary>
    /// Scales all phases by one modifier. Doesn't update current phase.
    /// </summary>
    [PublicAPI]
    public void ScaleAllPhaseThresholds(Entity<MobPhasesComponent?> ent, float scale)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        _thresholdScratch.Clear();
        foreach (var threshold in ent.Comp.PhaseThresholds)
            _thresholdScratch.Add(threshold);

        ent.Comp.PhaseThresholds.Clear();
        foreach (var (damageThreshold, state) in _thresholdScratch)
        {
            // State stays the same, damage threshold is scaled.
            ent.Comp.PhaseThresholds[damageThreshold * scale] = state;
        }

        _thresholdScratch.Clear();
    }

    /// <summary>
    /// Sets phase thresholds back to default that were set on MapInit. Doesn't update current phase.
    /// </summary>
    [PublicAPI]
    public void UnscaleAllPhaseThresholds(Entity<MobPhasesComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        ent.Comp.PhaseThresholds = new Dictionary<FixedPoint2, int>(ent.Comp.BasePhaseThresholds);
    }

    [PublicAPI]
    public void SetPhaseThreshold(Entity<MobPhasesComponent?> ent, FixedPoint2 damage, int phase)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        _thresholdScratch.Clear();
        foreach (var threshold in ent.Comp.PhaseThresholds)
            _thresholdScratch.Add(threshold);

        foreach (var (damageThreshold, state) in _thresholdScratch)
        {
            if (state != phase)
                continue;
            ent.Comp.PhaseThresholds.Remove(damageThreshold);
        }

        _thresholdScratch.Clear();
        ent.Comp.PhaseThresholds[damage] = phase;
    }
}
