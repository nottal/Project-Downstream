// SPDX-FileCopyrightText: 2023 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <metalgearsloth@gmail.com>
// SPDX-FileCopyrightText: 2024 Aiden <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2024 Ed <96445749+TheShuEd@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2024 Tadeo <td12233a@gmail.com>
// SPDX-FileCopyrightText: 2024 slarticodefast <161409025+slarticodefast@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: MIT

using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Weather;
using Robust.Shared.Console;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using System.Linq;

namespace Content.Server.Weather;

public sealed class WeatherSystem : SharedWeatherSystem
{
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;

    private readonly Dictionary<(EntityUid MapUid, string Weather), TimeSpan> _nextDamageTick = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WeatherComponent, ComponentGetState>(OnWeatherGetState);
        _console.RegisterCommand("weather",
            Loc.GetString("cmd-weather-desc"),
            Loc.GetString("cmd-weather-help"),
            WeatherTwo,
            WeatherCompletion);
    }

    private void OnWeatherGetState(EntityUid uid, WeatherComponent component, ref ComponentGetState args)
    {
        args.State = new WeatherComponentState(component.Weather);
    }

    protected override void Run(EntityUid uid, WeatherData weather, WeatherPrototype weatherProto, float frameTime)
    {
        if (weatherProto.Damage == null ||
            weather.State is WeatherState.Invalid or WeatherState.Ending)
        {
            return;
        }

        var curTime = Timing.CurTime;
        var key = (uid, weatherProto.ID);
        if (_nextDamageTick.TryGetValue(key, out var nextTick) && nextTick > curTime)
            return;

        var interval = MathF.Max(0.1f, weatherProto.DamageInterval);
        _nextDamageTick[key] = curTime + TimeSpan.FromSeconds(interval);

        var percent = GetPercent(weather, uid);
        if (percent <= 0f)
            return;

        var damage = percent >= 1f
            ? weatherProto.Damage
            : weatherProto.Damage * percent;

        var query = EntityQueryEnumerator<MobStateComponent, DamageableComponent, TransformComponent>();
        while (query.MoveNext(out var mob, out _, out var damageable, out var xform))
        {
            if (xform.MapUid != uid || xform.GridUid == null)
                continue;

            var gridUid = xform.GridUid.Value;
            if (!TryComp<MapGridComponent>(gridUid, out var grid) ||
                !_mapSystem.TryGetTileRef(gridUid, grid, xform.Coordinates, out var tileRef) ||
                !CanWeatherAffect(gridUid, grid, tileRef))
            {
                continue;
            }

            _damage.TryChangeDamage(mob, damage, interruptsDoAfters: false, damageable: damageable);
        }
    }

    protected override void EndWeather(EntityUid uid, WeatherComponent component, string proto)
    {
        _nextDamageTick.Remove((uid, proto));
        base.EndWeather(uid, component, proto);
    }

    [AdminCommand(AdminFlags.Fun)]
    private void WeatherTwo(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteError(Loc.GetString("cmd-weather-error-no-arguments"));
            return;
        }

        if (!int.TryParse(args[0], out var mapInt))
            return;

        var mapId = new MapId(mapInt);

        if (!MapManager.MapExists(mapId))
            return;

        if (!_mapSystem.TryGetMap(mapId, out var mapUid))
            return;

        var weatherComp = EnsureComp<WeatherComponent>(mapUid.Value);

        //Weather Proto parsing
        WeatherPrototype? weather = null;
        if (!args[1].Equals("null"))
        {
            if (!ProtoMan.TryIndex(args[1], out weather))
            {
                shell.WriteError(Loc.GetString("cmd-weather-error-unknown-proto"));
                return;
            }
        }

        //Time parsing
        TimeSpan? endTime = null;
        if (args.Length == 3)
        {
            var curTime = Timing.CurTime;
            if (int.TryParse(args[2], out var durationInt))
            {
                endTime = curTime + TimeSpan.FromSeconds(durationInt);
            }
            else
            {
                shell.WriteError(Loc.GetString("cmd-weather-error-wrong-time"));
            }
        }

        SetWeather(mapId, weather, endTime);
    }

    private CompletionResult WeatherCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHintOptions(CompletionHelper.MapIds(EntityManager), "Map Id");

        var a = CompletionHelper.PrototypeIDs<WeatherPrototype>(true, ProtoMan);
        var b = a.Concat(new[] { new CompletionOption("null", Loc.GetString("cmd-weather-null")) });
        return CompletionResult.FromHintOptions(b, Loc.GetString("cmd-weather-hint"));
    }
}
