using Content.Server.Explosion.EntitySystems;
using Content.Shared._Lavaland.Spawners;
using Content.Shared.EntityTable;

namespace Content.Server._Lavaland.Spawners;

public sealed class SpawnTableOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpawnTableOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<SpawnTableOnTriggerComponent> ent, ref TriggerEvent args)
    {
        var xform = Transform(ent);
        foreach (var proto in _entityTable.GetSpawns(ent.Comp.Table))
        {
            var coords = xform.Coordinates;
            if (coords.IsValid(EntityManager))
                Spawn(proto, coords);
            else
                Spawn(proto, _transform.GetMapCoordinates(ent, xform));
        }
    }
}
