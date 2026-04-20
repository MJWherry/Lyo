using System.Diagnostics;
using System.Linq;
using System.Text.Json.Serialization;
using Lyo.Exceptions;

namespace Lyo.Web.Automation.Models;

/// <summary>
/// Ordered locator path from the current browsing context (page or iframe) to a target element:
/// each segment is resolved relative to the previous one (Selenium: nested <c>FindElement</c>; Playwright: chained <c>ILocator</c>).
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ElementLocatorChain
{
    /// <summary>JSON / deserialization constructor.</summary>
    [JsonConstructor]
    public ElementLocatorChain(IReadOnlyList<ElementLocator> segments)
    {
        ValidateSegments(segments);
        Segments = Copy(segments);
    }

    /// <summary>Code convenience: <c>new ElementLocatorChain(a, b, c)</c>.</summary>
    public ElementLocatorChain(params ElementLocator[] segments)
        : this((IReadOnlyList<ElementLocator>)(segments ?? throw new ArgumentNullException(nameof(segments)))) { }

    /// <summary>Wraps a single segment (same as <see cref="ElementLocator.Then" /> for the first step).</summary>
    public static implicit operator ElementLocatorChain(ElementLocator locator)
    {
        ArgumentHelpers.ThrowIfNull(locator, nameof(locator));
        return new ElementLocatorChain(locator);
    }

    /// <summary>Appends a segment (fluent: <c>loc.Then(...).Then(...)</c>).</summary>
    public ElementLocatorChain Then(ElementLocator next)
    {
        ArgumentHelpers.ThrowIfNull(next, nameof(next));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(next.Value, nameof(next));
        var merged = new ElementLocator[Segments.Count + 1];
        for (var i = 0; i < Segments.Count; i++)
            merged[i] = Segments[i]!;

        merged[Segments.Count] = next;
        return new ElementLocatorChain(merged);
    }

    /// <summary>Locator segments from outermost (within the current document / frame) to innermost.</summary>
    public IReadOnlyList<ElementLocator> Segments { get; }

    /// <inheritdoc />
    public override string ToString()
        => string.Join(" -> ", Segments.Select(static s => s.ToString()));

    private static void ValidateSegments(IReadOnlyList<ElementLocator> segments)
    {
        ArgumentHelpers.ThrowIfNull(segments, nameof(segments));
        if (segments.Count < 1)
            throw new ArgumentException("At least one locator segment is required.", nameof(segments));

        foreach (var s in segments) {
            ArgumentHelpers.ThrowIfNull(s, nameof(segments));
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(s.Value, nameof(segments));
        }
    }

    private static ElementLocator[] Copy(IReadOnlyList<ElementLocator> segments)
    {
        var copy = new ElementLocator[segments.Count];
        for (var i = 0; i < segments.Count; i++)
            copy[i] = segments[i]!;

        return copy;
    }
}
