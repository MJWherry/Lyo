using Microsoft.AspNetCore.Components;

namespace Lyo.TestGateway.Components.TestGateway;

public partial class RichTextEditorWorkbench
{
    private bool _readOnly;

    private void Clear() => _html = string.Empty;

    private void ToggleReadOnly() => _readOnly = !_readOnly;
}
