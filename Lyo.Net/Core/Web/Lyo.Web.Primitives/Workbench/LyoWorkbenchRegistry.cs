using Lyo.Exceptions;

namespace Lyo.Web.Primitives;

/// <summary>
/// The workbenches a host has registered. <see cref="LyoNavMenu" /> and <see cref="LyoWorkbenchHost" /> read this; packages write it through
/// <c>AddLyoWorkbench</c>.
/// </summary>
public sealed class LyoWorkbenchRegistry
{
    /// <summary>Builds a registry over the descriptors registered in DI.</summary>
    public LyoWorkbenchRegistry(IEnumerable<ILyoWorkbenchDescriptor> descriptors)
    {
        ArgumentHelpers.ThrowIfNull(descriptors);
        All = descriptors
            .Where(item => !string.IsNullOrWhiteSpace(item.Title) && item.ComponentType is not null)
            .OrderBy(item => item.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>Every registered workbench, grouped later by the nav menu.</summary>
    public IReadOnlyList<ILyoWorkbenchDescriptor> All { get; }

    /// <summary>Finds a workbench by <see cref="ILyoWorkbenchDescriptor.Slug" />, or null when none match.</summary>
    public ILyoWorkbenchDescriptor? Find(string? slug)
        => string.IsNullOrWhiteSpace(slug)
            ? null
            : All.FirstOrDefault(item => string.Equals(item.Slug, slug, StringComparison.OrdinalIgnoreCase));
}
