using System;
using System.Collections.Generic;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace MagicEngine.Engine.Base.DataDefinitionSystem;

/// <summary>
/// A YamlDotNet node-type resolver that maps read-only collection interfaces to
/// their concrete mutable counterparts so that the deserializer can instantiate them.
///
/// Without this, deserializing <c>IReadOnlyList&lt;T&gt;</c> or
/// <c>IReadOnlyDictionary&lt;TKey,TValue&gt;</c> throws a "No node deserializer was
/// able to deserialize the node…" exception.
/// </summary>
internal sealed class ReadOnlyCollectionTypeResolver : INodeTypeResolver
{
    public bool Resolve(NodeEvent? nodeEvent, ref Type currentType)
    {
        if (!currentType.IsGenericType) return false;

        var def = currentType.GetGenericTypeDefinition();
        var args = currentType.GetGenericArguments();

        // IReadOnlyList<T>  →  List<T>
        if (def == typeof(IReadOnlyList<>) || def == typeof(IReadOnlyCollection<>))
        {
            currentType = typeof(List<>).MakeGenericType(args);
            return true;
        }

        // IReadOnlyDictionary<TKey,TValue>  →  Dictionary<TKey,TValue>
        if (def == typeof(IReadOnlyDictionary<,>))
        {
            currentType = typeof(Dictionary<,>).MakeGenericType(args);
            return true;
        }

        return false;
    }
}
