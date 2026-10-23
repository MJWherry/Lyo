using Lyo.Common.Core.Enums;
using Lyo.Exceptions;

namespace Lyo.Common.Core;

/// <summary>Orders members by <see cref="MemberOrder" />. Defined returns the input sequence.</summary>
public static class MemberOrderSort
{
    /// <summary>
    /// Returns <paramref name="items" /> in write order. Alphabetical sorts by <paramref name="name" />. Specified puts <paramref name="orderNames" /> first, then
    /// <paramref name="specifiedOrder" />, then the remaining items in their current sequence.
    /// </summary>
    public static List<T> Sort<T>(IReadOnlyList<T> items, Func<T, string> name, Func<T, int?> specifiedOrder, MemberOrder order, IReadOnlyList<string> orderNames)
    {
        ArgumentHelpers.ThrowIfNull(items);
        ArgumentHelpers.ThrowIfNull(name);
        ArgumentHelpers.ThrowIfNull(specifiedOrder);
        ArgumentHelpers.ThrowIfNull(orderNames);
        if (order == MemberOrder.Defined || items.Count <= 1)
            return items as List<T> ?? [.. items];

        var listed = new Dictionary<string, int>(orderNames.Count, StringComparer.Ordinal);
        for (var i = 0; i < orderNames.Count; i++) {
            if (!listed.ContainsKey(orderNames[i]))
                listed[orderNames[i]] = i;
        }

        var ranked = new List<Ranked<T>>(items.Count);
        for (var i = 0; i < items.Count; i++) {
            var item = items[i];
            var key = name(item) ?? "";
            int group;
            int rank;
            if (order == MemberOrder.Alphabetical) {
                group = 0;
                rank = 0;
            }
            else if (listed.TryGetValue(key, out var listRank)) {
                group = 0;
                rank = listRank;
            }
            else if (specifiedOrder(item) is int attributeOrder) {
                group = 1;
                rank = attributeOrder;
            }
            else {
                group = 2;
                rank = i;
            }

            ranked.Add(new(item, key, group, rank, i));
        }

        if (order == MemberOrder.Alphabetical) {
            ranked.Sort(static (left, right) => {
                var compared = string.CompareOrdinal(left.Key, right.Key);
                return compared != 0 ? compared : left.Index.CompareTo(right.Index);
            });
        }
        else {
            ranked.Sort(static (left, right) => {
                var compared = left.Group.CompareTo(right.Group);
                if (compared != 0)
                    return compared;

                compared = left.Rank.CompareTo(right.Rank);
                return compared != 0 ? compared : left.Index.CompareTo(right.Index);
            });
        }

        var result = new List<T>(ranked.Count);
        foreach (var entry in ranked)
            result.Add(entry.Item);

        return result;
    }

    private readonly record struct Ranked<T>(T Item, string Key, int Group, int Rank, int Index);
}