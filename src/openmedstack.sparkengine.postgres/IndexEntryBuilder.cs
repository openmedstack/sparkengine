using OpenMedStack.SparkEngine.Search.Model;

namespace OpenMedStack.SparkEngine.Postgres;

using System;
using System.Linq;
using Model;
using Search.ValueExpressionTypes;

internal static class IndexEntryBuilder
{
    public static IndexEntry BuildIndexEntry(this IndexValue indexValue)
    {
        var values = indexValue.IndexValues()
            .Where(x => x.Name != null)
            .ToDictionary(x => x.Name!, x => x.Values.Select(GetValue).Distinct().ToArray());
        var id = values[IndexFieldNames.FOR_RESOURCE][0].ToString()!;
        var resourceType = values[IndexFieldNames.RESOURCE][0].ToString()!;
        var canonicalId = values[IndexFieldNames.ID][0].ToString()!;
        var toRemove = values.Keys
            .Where(x => x[0] == '_' || x.StartsWith("internal_", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        foreach (var key in toRemove)
        {
            values.Remove(key);
        }

        var entry = new IndexEntry(id, canonicalId, resourceType, values);

        return entry;
    }

    public static object GetValue(this Expression expression)
    {
        return expression switch
        {
            StringValue stringValue => stringValue.Value,
            IndexValue indexValue => indexValue.Values.Count == 1
                ? GetValue(indexValue.Values[0])
                : indexValue.Values.Select(GetValue).ToArray(),
            CompositeValue compositeValue =>
                compositeValue.Components.OfType<IndexValue>().All(x => x.Name == "code")
                    ? compositeValue.Components.OfType<IndexValue>()
                        .SelectMany(x => x.Values.Select(GetValue))
                        .First()
                    : compositeValue.Components.OfType<IndexValue>()
                        .Where(x => x.Name != null)
                        .ToDictionary(
                            component => component.Name!,
                            component =>
                            {
                                var a = component.Values.Select(GetValue).ToArray();
                                return a.Length == 1 ? a[0] : a;
                            }),
            _ => expression.ToString() ?? ""
        };
    }

}
