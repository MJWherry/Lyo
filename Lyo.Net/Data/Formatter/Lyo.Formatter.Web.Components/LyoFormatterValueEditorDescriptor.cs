using Lyo.Common.Core.Enums;
using Lyo.Web.Components.LyoType;

namespace Lyo.Formatter.Web.Components;

/// <summary>Wires <see cref="LyoFormatterValueEditor" /> as the editor for formatter-typed parameter values.</summary>
public sealed class LyoFormatterValueEditorDescriptor : ILyoValueEditorDescriptor
{
    /// <inheritdoc />
    public LyoTypeEditorKind Kind => LyoTypeEditorKind.Formatter;

    /// <inheritdoc />
    public Type ComponentType => typeof(LyoFormatterValueEditor);
}