using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Lyo.Testing;

public static class Extensions
{
    public static void ShouldBeCloseTo(this DateTime actual, DateTime expected, TimeSpan tolerance)
    {
        var difference = (actual - expected).Duration();
        Assert.True(difference <= tolerance, $"Expected {actual} to be within {tolerance} of {expected}, but difference was {difference}");
    }

    public static void ShouldBeCloseTo(this TimeSpan actual, TimeSpan expected, TimeSpan tolerance)
    {
        var difference = (actual - expected).Duration();
        Assert.True(difference <= tolerance, $"Expected {actual} to be within {tolerance} of {expected}, but difference was {difference}");
    }

    public static void ShouldBeNull<T>(this T? actual)
        where T : class
        => Assert.Null(actual);

    public static void ShouldNotBeNull<T>([NotNull] this T? actual)
        where T : class
        => Assert.NotNull(actual);

    public static void ShouldBeNull<T>(this T? actual)
        where T : struct
        => Assert.Null((object?)actual);

    public static void ShouldNotBeNull<T>([NotNull] this T? actual)
        where T : struct
        => Assert.NotNull((object?)actual);

    extension<T>(IEnumerable<T> collection)
    {
        public void ShouldBeEmpty(string? message = null) => CollectionAssertions.IsEmpty(collection, message);

        public void ShouldNotBeEmpty(string? message = null) => CollectionAssertions.IsNotEmpty(collection, message);

        public void ShouldContain(T expected, IEqualityComparer<T>? comparer = null)
        {
            comparer ??= EqualityComparer<T>.Default;
            Assert.Contains(expected, collection, comparer);
        }

        public void ShouldNotContain(T unexpected, IEqualityComparer<T>? comparer = null)
        {
            comparer ??= EqualityComparer<T>.Default;
            Assert.DoesNotContain(unexpected, collection, comparer);
        }

        public void ShouldContainAll(IEnumerable<T> expectedItems, IEqualityComparer<T>? comparer = null) => CollectionAssertions.ContainsAll(collection, expectedItems, comparer);

        public void ShouldContainNone(IEnumerable<T> excludedItems, IEqualityComparer<T>? comparer = null)
            => CollectionAssertions.ContainsNone(collection, excludedItems, comparer);

        public void ShouldHaveCount(int expectedCount) => Assert.Equal(expectedCount, collection.Count());

        public void ShouldHaveCount(int expectedCount, Func<T, bool> predicate) => CollectionAssertions.ContainsExactly(collection, expectedCount, predicate);

        public void ShouldAllSatisfy(Func<T, bool> predicate, string? message = null) => CollectionAssertions.AllSatisfy(collection, predicate, message);

        public void ShouldAnySatisfy(Func<T, bool> predicate, string? message = null) => CollectionAssertions.AnySatisfies(collection, predicate, message);
    }

    extension<T>(IEnumerable<T> collection)
        where T : IComparable<T>
    {
        public void ShouldBeOrdered(IComparer<T>? comparer = null) => CollectionAssertions.IsOrdered(collection, comparer);

        public void ShouldBeOrderedDescending(IComparer<T>? comparer = null) => CollectionAssertions.IsOrderedDescending(collection, comparer);
    }

    extension<T>(IEnumerable<T> collection)
    {
        public void ShouldHaveUniqueItems(IEqualityComparer<T>? comparer = null) => CollectionAssertions.HasUniqueItems(collection, comparer);

        public void ShouldBeEquivalentTo(IEnumerable<T> expected, IEqualityComparer<T>? comparer = null)
        {
            comparer ??= EqualityComparer<T>.Default;
            var actualList = collection.ToList();
            var expectedList = expected.ToList();
            if (actualList.Count != expectedList.Count)
                Assert.Fail($"Collections have different counts. Expected: {expectedList.Count}, Actual: {actualList.Count}");

            var missing = expectedList.Except(actualList, comparer).ToList();
            var extra = actualList.Except(expectedList, comparer).ToList();
            if (missing.Any() || extra.Any()) {
                var message = "Collections are not equivalent.";
                if (missing.Any())
                    message += $" Missing: {string.Join(", ", missing)}";

                if (extra.Any())
                    message += $" Extra: {string.Join(", ", extra)}";

                Assert.Fail(message);
            }
        }
    }

    extension<T>(T actual)
        where T : IComparable<T>
    {
        public void ShouldBeGreaterThan(T expected, IComparer<T>? comparer = null)
        {
            comparer ??= Comparer<T>.Default;
            Assert.True(comparer.Compare(actual, expected) > 0, $"Expected {actual} to be greater than {expected}");
        }

        public void ShouldBeGreaterThanOrEqualTo(T expected, IComparer<T>? comparer = null)
        {
            comparer ??= Comparer<T>.Default;
            Assert.True(comparer.Compare(actual, expected) >= 0, $"Expected {actual} to be greater than or equal to {expected}");
        }

        public void ShouldBeLessThan(T expected, IComparer<T>? comparer = null)
        {
            comparer ??= Comparer<T>.Default;
            Assert.True(comparer.Compare(actual, expected) < 0, $"Expected {actual} to be less than {expected}");
        }

        public void ShouldBeLessThanOrEqualTo(T expected, IComparer<T>? comparer = null)
        {
            comparer ??= Comparer<T>.Default;
            Assert.True(comparer.Compare(actual, expected) <= 0, $"Expected {actual} to be less than or equal to {expected}");
        }

        public void ShouldBeBetween(T minimum, T maximum, IComparer<T>? comparer = null)
        {
            comparer ??= Comparer<T>.Default;
            Assert.True(comparer.Compare(actual, minimum) >= 0 && comparer.Compare(actual, maximum) <= 0, $"Expected {actual} to be between {minimum} and {maximum}");
        }
    }

    extension<T>(T actual)
    {
        public void ShouldBe(T expected) => Assert.Equal(expected, actual);

        public void ShouldNotBe(T unexpected) => Assert.NotEqual(unexpected, actual);
    }

    extension(bool actual)
    {
        public void ShouldBeTrue(string? message = null) => Assert.True(actual, message);

        public void ShouldBeFalse(string? message = null) => Assert.False(actual, message);
    }

    extension<T>(T actual)
        where T : class
    {
        public void ShouldBeSameAs(T expected) => Assert.Same(expected, actual);

        public void ShouldNotBeSameAs(T expected) => Assert.NotSame(expected, actual);
    }

    extension(object? actual)
    {
        public void ShouldBeAssignableTo<T>() => Assert.IsAssignableFrom<T>(actual);

        public void ShouldBeOfType<T>() => Assert.IsType<T>(actual);
    }

    extension(string? actual)
    {
        public void ShouldStartWith(string expected) => Assert.StartsWith(expected, actual);

        public void ShouldEndWith(string expected) => Assert.EndsWith(expected, actual);

        public void ShouldContain(string expectedSubstring) => Assert.Contains(expectedSubstring, actual);

        public void ShouldNotContain(string unexpectedSubstring) => Assert.DoesNotContain(unexpectedSubstring, actual);

        public void ShouldMatch(string pattern) => Assert.Matches(pattern, actual);

        public void ShouldNotMatch(string pattern) => Assert.DoesNotMatch(pattern, actual);

        public void ShouldBeEmpty(string? message = null) => Assert.Empty(actual);

        public void ShouldNotBeEmpty(string? message = null) => Assert.NotEmpty(actual);
    }
}