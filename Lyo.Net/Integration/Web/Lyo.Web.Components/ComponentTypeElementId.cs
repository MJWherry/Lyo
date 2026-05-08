namespace Lyo.Web.Components;

/// <summary>Stable component HTML <c>id</c> prefix (<c>lyo-component-…</c>) with optional appended segments.</summary>
public static class ComponentTypeElementId
{
    public static string FromComponentType<T>(params string[] extraParts) => FromComponentType(typeof(T), extraParts);

    public static string FromComponentType(Type componentType, params string[] extraParts)
    {
        ArgumentNullException.ThrowIfNull(componentType);
        return Build(componentType.Name, extraParts);
    }

    public static string FromComponentType(string? componentTypeName, params string[] extraParts)
        => Build(componentTypeName, extraParts);

    private static string Build(string? componentTypeName, IReadOnlyList<string> extraParts)
    {
        var typeSegment = ElementIdSegmentNormalizer.NormalizeOrDefault(componentTypeName);
        var suffix = BuildSuffix(extraParts);
        return string.IsNullOrEmpty(suffix)
            ? $"lyo-component-{typeSegment}"
            : $"lyo-component-{typeSegment}-{suffix}";
    }

    private static string BuildSuffix(IReadOnlyList<string> extraParts)
    {
        if (extraParts.Count == 0)
            return string.Empty;

        var normalizedParts = new List<string>(extraParts.Count);
        for (var i = 0; i < extraParts.Count; i++) {
            if (string.IsNullOrWhiteSpace(extraParts[i]))
                continue;

            normalizedParts.Add(ElementIdSegmentNormalizer.NormalizeOrDefault(extraParts[i], string.Empty));
        }

        if (normalizedParts.Count == 0)
            return string.Empty;

        return string.Join("-", normalizedParts.Where(static part => !string.IsNullOrEmpty(part)));
    }
}
