using Content.Server._Funkystation.Genetics.Mutations.Components;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Robust.Shared.Random;

namespace Content.Server._Funkystation.Genetics.Mutations.Systems;

public sealed class MutationSpeechOverloadSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MutationSpeechOverloadComponent, EntitySpokeEvent>(OnEntitySpoke);
    }

    private void OnEntitySpoke(Entity<MutationSpeechOverloadComponent> ent, ref EntitySpokeEvent args)
    {
        if (args.Source != ent.Owner || !_random.Prob(ent.Comp.GibChance))
            return;

        _body.GibBody(ent.Owner);
    }
}
