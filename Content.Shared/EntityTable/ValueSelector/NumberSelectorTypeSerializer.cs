using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.EntityTable.ValueSelector;

[TypeSerializer]
public sealed class NumberSelectorTypeSerializer : ITypeReader<NumberSelector, ValueDataNode>
{
    public ValidationNode Validate(ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        return TryRead(node.Value, out _)
            ? new ValidatedValueNode(node)
            : new ErrorNode(node, "Expected an integer or an integer range like '1, 3'.");
    }

    public NumberSelector Read(ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<NumberSelector>? instanceProvider = null)
    {
        if (!TryRead(node.Value, out var selector))
            throw new InvalidOperationException($"Invalid number selector value '{node.Value}'.");

        return selector;
    }

    private static bool TryRead(string value, out NumberSelector selector)
    {
        selector = default!;

        var parts = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1 && int.TryParse(parts[0], out var constant))
        {
            selector = new ConstantNumberSelector(constant);
            return true;
        }

        if (parts.Length == 2
            && int.TryParse(parts[0], out var min)
            && int.TryParse(parts[1], out var max))
        {
            selector = new RangeNumberSelector { Range = new(min, max) };
            return true;
        }

        return false;
    }
}
