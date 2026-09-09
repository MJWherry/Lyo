using Lyo.Common.Core.Enums;
using Lyo.Exceptions;

namespace Lyo.Web.Components.LyoType;

/// <summary>
/// Value editors contributed by other packages, keyed by <see cref="LyoTypeEditorKind" />. Resolved optionally by <see cref="LyoTypeValueInput" />, so a host that
/// registers nothing keeps the included editors.
/// </summary>
public sealed class LyoValueEditorCatalog
{
    private readonly Dictionary<LyoTypeEditorKind, Type> _byKind;

    /// <summary>Takes every registered descriptor. When two claim the same kind the first registration wins, which keeps startup order predictable.</summary>
    public LyoValueEditorCatalog(IEnumerable<ILyoValueEditorDescriptor> descriptors)
    {
        ArgumentHelpers.ThrowIfNull(descriptors);
        _byKind = [];
        foreach (var descriptor in descriptors)
            _byKind.TryAdd(descriptor.Kind, descriptor.ComponentType);
    }

    /// <summary>Component registered for <paramref name="kind" />, if any.</summary>
    public bool TryGet(LyoTypeEditorKind kind, out Type componentType) => _byKind.TryGetValue(kind, out componentType!);
}
