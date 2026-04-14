using Lyo.Web.Components.RichTextEditor;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.Web.Components;

public partial class LyoRichTextEditor : IAsyncDisposable
{
    private static readonly IReadOnlyList<SelectOption> FontFamilyOptions = [
        new("Arial, Helvetica, sans-serif", "Sans"), new("'Times New Roman', Times, serif", "Serif"), new("Georgia, serif", "Georgia"),
        new("'Courier New', Courier, monospace", "Monospace"), new("Verdana, Geneva, sans-serif", "Verdana")
    ];

    private static readonly IReadOnlyList<SelectOption> FontSizeOptions = [
        new("12px", "12"), new("14px", "14"), new("16px", "16"), new("18px", "18"), new("24px", "24"), new("32px", "32")
    ];

    private static readonly IReadOnlyList<SelectOption> CodeLanguageOptions = [
        new("plaintext", "Plain Text"), new("csharp", "C#"), new("javascript", "JavaScript"), new("typescript", "TypeScript"), new("json", "JSON"), new("sql", "SQL"),
        new("html", "HTML"), new("css", "CSS"), new("bash", "Bash"), new("python", "Python"), new("xml", "XML"), new("yaml", "YAML")
    ];

    private static readonly LyoRichTextEditorToolbarState DefaultToolbarState = new();
    private LyoRichTextEditorController? _editorController;

    private ElementReference _editorRef;
    private ElementReference _rootRef;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    [Parameter]
    public string? Value { get; set; }

    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    [Parameter]
    public string Placeholder { get; set; } = "Start typing...";

    [Parameter]
    public string? Label { get; set; }

    [Parameter]
    public bool ReadOnly { get; set; }

    [Parameter]
    public bool ShowToolbar { get; set; } = true;

    [Parameter]
    public int MinHeightPx { get; set; } = 240;

    private LyoRichTextEditorToolbarState ToolbarState => _editorController?.ToolbarState ?? DefaultToolbarState;

    private bool IsQuoteBlockActive => string.Equals(ToolbarState.BlockTag, "blockquote", StringComparison.Ordinal);

    private bool IsCodeBlockActive => string.Equals(ToolbarState.BlockTag, "pre", StringComparison.Ordinal);

    private bool DisableFontFamilyControl => IsCodeBlockActive;

    private bool DisableUnsupportedCodeBlockControls => IsCodeBlockActive;

    public async ValueTask DisposeAsync()
    {
        if (_editorController != null) {
            try {
                await _editorController.DisposeAsync();
            }
            catch (JSDisconnectedException) {
                // Ignore disposal during circuit teardown.
            }
        }
    }

    protected override void OnInitialized()
        => _editorController = new(JsRuntime) { HtmlChangedAsync = HandleEditorHtmlChangedAsync, StateChangedAsync = HandleEditorStateChangedAsync };

    protected override void OnParametersSet() => _editorController?.UpdateValue(Value ?? string.Empty);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_editorController == null)
            return;

        if (firstRender) {
            await _editorController.InitializeAsync(_rootRef, _editorRef);
            return;
        }

        await _editorController.SyncPendingHtmlAsync();
    }

    private Task ToggleBoldAsync() => WithEditorControllerAsync(controller => controller.BoldAsync());

    private Task ToggleItalicAsync() => WithEditorControllerAsync(controller => controller.ItalicAsync());

    private Task ToggleUnderlineAsync() => WithEditorControllerAsync(controller => controller.UnderlineAsync());

    private Task ToggleStrikeThroughAsync() => WithEditorControllerAsync(controller => controller.StrikeThroughAsync());

    private Task ApplyQuoteBlockAsync() => WithEditorControllerAsync(controller => controller.ApplyQuoteBlockAsync());

    private Task ApplyCodeBlockAsync() => WithEditorControllerAsync(controller => controller.ApplyCodeBlockAsync());

    private Task AlignLeftAsync() => WithEditorControllerAsync(controller => controller.AlignLeftAsync());

    private Task AlignCenterAsync() => WithEditorControllerAsync(controller => controller.AlignCenterAsync());

    private Task AlignRightAsync() => WithEditorControllerAsync(controller => controller.AlignRightAsync());

    private Task ToggleUnorderedListAsync() => WithEditorControllerAsync(controller => controller.InsertUnorderedListAsync());

    private Task ToggleOrderedListAsync() => WithEditorControllerAsync(controller => controller.InsertOrderedListAsync());

    private Task OutdentAsync() => WithEditorControllerAsync(controller => controller.OutdentAsync());

    private Task IndentAsync() => WithEditorControllerAsync(controller => controller.IndentAsync());

    private Task RemoveLinkAsync() => WithEditorControllerAsync(controller => controller.RemoveLinkAsync());

    private Task InsertHorizontalRuleAsync() => WithEditorControllerAsync(controller => controller.InsertHorizontalRuleAsync());

    private Task UndoAsync() => WithEditorControllerAsync(controller => controller.UndoAsync());

    private Task RedoAsync() => WithEditorControllerAsync(controller => controller.RedoAsync());

    private Task ClearFormattingAsync() => WithEditorControllerAsync(controller => controller.ClearFormattingAsync());

    private Task PromptForLinkAsync() => WithEditorControllerAsync(controller => controller.PromptForLinkAsync());

    private Task PromptForImageAsync() => WithEditorControllerAsync(controller => controller.PromptForImageAsync());

    private Task WithEditorControllerAsync(Func<LyoRichTextEditorController, Task> action)
        => ReadOnly || _editorController == null ? Task.CompletedTask : action(_editorController);

    private Task HandleEditorStateChangedAsync() => InvokeAsync(StateHasChanged);

    private Task HandleEditorHtmlChangedAsync(string html)
        => string.Equals(Value, html, StringComparison.Ordinal) ? Task.CompletedTask : InvokeAsync(() => ValueChanged.InvokeAsync(html));

    private static Color GetButtonColor(bool isActive) => isActive ? Color.Primary : Color.Default;

    private sealed record SelectOption(string Value, string Label);
}