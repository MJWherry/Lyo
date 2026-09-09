namespace Lyo.Web.Primitives;

/// <summary>
/// One trail entry for <see cref="LyoBreadcrumbs" />. A null or blank <paramref name="Href" /> renders plain text, which is how the current page appears.
/// </summary>
/// <param name="Text">Label written in the trail.</param>
/// <param name="Href">Target route, or null for the current page.</param>
/// <param name="Icon">Optional Material icon placed before the label.</param>
public sealed record LyoBreadcrumb(string Text, string? Href = null, string? Icon = null);
