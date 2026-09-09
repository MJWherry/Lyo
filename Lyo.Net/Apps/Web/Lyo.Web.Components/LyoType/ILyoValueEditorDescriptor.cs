using Lyo.Common.Core.Enums;

namespace Lyo.Web.Components.LyoType;

/// <summary>
/// Adds a value editor component that lives outside this package, so <see cref="LyoTypeValueInput" /> can render a richer editor than its included fields for
/// one <see cref="LyoTypeEditorKind" />.
/// </summary>
/// <remarks>
/// This exists to break a reference cycle. The rich editors live in packages that already reference <c>Lyo.Web.Components</c> (the SmartFormat template editor in
/// <c>Lyo.Formatter.Web.Components</c>, for instance), so <see cref="LyoTypeValueInput" /> cannot name them. Instead those packages register a descriptor and the
/// component is resolved through <see cref="LyoValueEditorCatalog" /> and rendered with <c>DynamicComponent</c>. Only the host needs both references.
/// <para>
/// The editor component must accept these parameters: <c>string? Json</c>, <c>EventCallback&lt;string?&gt; JsonChanged</c>, <c>string? Label</c>,
/// <c>bool ReadOnly</c>, <c>bool Required</c>, and <c>object? Context</c>. <c>Json</c> is the same JSON payload the included editors read and write, so an editor
/// working in plain text has to unwrap and re-serialize it.
/// </para>
/// </remarks>
public interface ILyoValueEditorDescriptor
{
    /// <summary>Editor kind this component replaces. One descriptor wins per kind; later registrations do not stack.</summary>
    LyoTypeEditorKind Kind { get; }

    /// <summary>Component type rendered through <c>DynamicComponent</c>.</summary>
    Type ComponentType { get; }
}
