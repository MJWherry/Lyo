namespace Lyo.Web.Primitives;

/// <summary>Default <see cref="ILyoWorkbenchDescriptor" /> minted by <c>AddLyoWorkbench</c>.</summary>
public sealed record LyoWorkbenchDescriptor(string Title, string Icon, string Category, string Route, Type ComponentType, string? SlugOverride = null)
    : ILyoWorkbenchDescriptor
{
    /// <inheritdoc />
    public string Slug => string.IsNullOrWhiteSpace(SlugOverride) ? LastSegment(Route) : SlugOverride.Trim();

    private static string LastSegment(string route)
    {
        var trimmed = route.Trim('/');
        var slash = trimmed.LastIndexOf('/');
        return slash < 0 ? trimmed : trimmed[(slash + 1)..];
    }
}
