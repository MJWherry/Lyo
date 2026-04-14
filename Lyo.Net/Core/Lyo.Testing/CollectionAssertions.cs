using Xunit;

namespace Lyo.Testing;

public static class CollectionAssertions
{
    public static void ContainsAll<T>(IEnumerable<T> collection, IEnumerable<T> expectedItems, IEqualityComparer<T>? comparer = null)
    {
        comparer ??= EqualityComparer<T>.Default;
        var collectionList = collection.ToList();
        var missing = expectedItems.Where(item => !collectionList.Contains(item, comparer)).ToList();
        if (missing.Any())
            Assert.Fail($"Collection does not contain all expected items. Missing: {string.Join(", ", missing)}");
    }

    public static void ContainsNone<T>(IEnumerable<T> collection, IEnumerable<T> excludedItems, IEqualityComparer<T>? comparer = null)
    {
        comparer ??= EqualityComparer<T>.Default;
        var collectionList = collection.ToList();
        var found = excludedItems.Where(item => collectionList.Contains(item, comparer)).ToList();
        if (found.Any())
            Assert.Fail($"Collection contains excluded items. Found: {string.Join(", ", found)}");
    }

    public static void ContainsExactly<T>(IEnumerable<T> collection, int expectedCount, Func<T, bool>? predicate = null)
    {
        var count = predicate != null ? collection.Count(predicate) : collection.Count();
        Assert.Equal(expectedCount, count);
    }

    public static void IsEmpty<T>(IEnumerable<T> collection, string? message = null)
    {
        var isEmpty = !collection.Any();
        Assert.True(isEmpty, message ?? "Collection is not empty");
    }

    public static void IsNotEmpty<T>(IEnumerable<T> collection, string? message = null)
    {
        var isNotEmpty = collection.Any();
        Assert.True(isNotEmpty, message ?? "Collection is empty");
    }

    public static void AllSatisfy<T>(IEnumerable<T> collection, Func<T, bool> predicate, string? message = null)
    {
        var failing = collection.Where(item => !predicate(item)).ToList();
        if (failing.Any())
            Assert.Fail(message ?? $"Not all items satisfy the predicate. Failing items: {string.Join(", ", failing)}");
    }

    public static void AnySatisfies<T>(IEnumerable<T> collection, Func<T, bool> predicate, string? message = null)
    {
        var anySatisfies = collection.Any(predicate);
        Assert.True(anySatisfies, message ?? "No items satisfy the predicate");
    }

    public static void IsOrdered<T>(IEnumerable<T> collection, IComparer<T>? comparer = null)
        where T : IComparable<T>
    {
        comparer ??= Comparer<T>.Default;
        var list = collection.ToList();
        for (var i = 0; i < list.Count - 1; i++) {
            if (comparer.Compare(list[i], list[i + 1]) > 0)
                Assert.Fail($"Collection is not ordered. Item at index {i} ({list[i]}) is greater than item at index {i + 1} ({list[i + 1]})");
        }
    }

    public static void IsOrderedDescending<T>(IEnumerable<T> collection, IComparer<T>? comparer = null)
        where T : IComparable<T>
    {
        comparer ??= Comparer<T>.Default;
        var list = collection.ToList();
        for (var i = 0; i < list.Count - 1; i++) {
            if (comparer.Compare(list[i], list[i + 1]) < 0)
                Assert.Fail($"Collection is not ordered descending. Item at index {i} ({list[i]}) is less than item at index {i + 1} ({list[i + 1]})");
        }
    }

    public static void HasUniqueItems<T>(IEnumerable<T> collection, IEqualityComparer<T>? comparer = null)
    {
        comparer ??= EqualityComparer<T>.Default;
        var list = collection.ToList();
        var duplicates = list.GroupBy(x => x, comparer).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicates.Any())
            Assert.Fail($"Collection contains duplicate items: {string.Join(", ", duplicates)}");
    }
}