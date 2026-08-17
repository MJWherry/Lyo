using System.ComponentModel;
using Microsoft.AspNetCore.Components;

namespace Lyo.Formatter.Web.Components;

/// <summary>Live annotated preview of a SmartFormat template. Pair with <see cref="LyoFormatterTemplateEditor" /> via <see cref="LyoFormatterLiveSession" />.</summary>
public partial class LyoFormatterPreview : IDisposable
{
    private LyoFormatterLiveSession? _bound;

    /// <summary>Shared session. Wins over a cascaded session when both are set.</summary>
    [Parameter]
    public LyoFormatterLiveSession? Session { get; set; }

    /// <summary>Optional cascaded session so a parent can wrap distant editor/preview pairs once.</summary>
    [CascadingParameter]
    public LyoFormatterLiveSession? CascadedSession { get; set; }

    /// <summary>Field label. Empty hides the caption.</summary>
    [Parameter]
    public string Label { get; set; } = "Preview";

    /// <summary>Shown when the debounced template is empty.</summary>
    [Parameter]
    public string EmptyText { get; set; } = "Type a template to preview replacements.";

    private LyoFormatterLiveSession Resolved => _bound ?? throw new InvalidOperationException("LyoFormatterPreview requires a Session parameter or a cascaded LyoFormatterLiveSession.");

    /// <inheritdoc />
    public void Dispose()
    {
        Unbind();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        var next = Session ?? CascadedSession;
        if (next == null)
            throw new InvalidOperationException("LyoFormatterPreview requires a Session parameter or a cascaded LyoFormatterLiveSession.");
        if (!ReferenceEquals(_bound, next)) {
            Unbind();
            _bound = next;
            _bound.PropertyChanged += OnSessionChanged;
        }
    }

    private void OnSessionChanged(object? sender, PropertyChangedEventArgs e) => _ = InvokeAsync(StateHasChanged);

    private void Unbind()
    {
        if (_bound == null)
            return;

        _bound.PropertyChanged -= OnSessionChanged;
        _bound = null;
    }
}
