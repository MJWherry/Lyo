using System.Text.Json;
using Lyo.Json;
using Microsoft.JSInterop;

namespace Lyo.Story.Web.Components;

/// <summary>Authors one story: visual graph, the graph request JSON, and a play tab.</summary>
public partial class StoryBuilderWorkbench : IDisposable
{
    private const int SaveDebounceMs = 450;

    private static readonly JsonSerializerOptions JsonOptions = LyoJsonSerializerOptions.Create(options => options.WriteIndented = true);

    private readonly JsonSerializerOptions _json = JsonOptions;
    private StoryGraphReq _request = new();
    private string _csharp = "";
    private string? _selectedKey;
    private int _choiceIndex;
    private List<string> _issues = [];
    private IReadOnlyList<FilterPropertyDefinition> _fields = [];
    private bool _saving;
    private CancellationTokenSource? _saveDebounceCts;

    /// <summary>Client that saves and loads the graph.</summary>
    [Parameter]
    [EditorRequired]
    public IStoryClient StoryClient { get; set; } = null!;

    /// <summary>Route prefix shown to the host. The client already carries its own prefix.</summary>
    [Parameter]
    public string BaseRoute { get; set; } = "Story";

    /// <summary>Graph being edited.</summary>
    [Parameter]
    [EditorRequired]
    public StoryDraft Draft { get; set; } = new();

    /// <summary>Raised after a successful save, with the stored graph.</summary>
    [Parameter]
    public EventCallback<StoryGraphRes> Saved { get; set; }

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    private IJSRuntime Js { get; set; } = null!;

    [Inject]
    private ClientStore ClientStore { get; set; } = null!;

    [Inject]
    private ILogger<StoryBuilderWorkbench> Logger { get; set; } = null!;

    private StoryNodeDraft? Selected => Draft.Nodes.FirstOrDefault(node => node.Key == _selectedKey);

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        try {
            var json = await ClientStore.GetStoryBuilderStateAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(json))
                return;
            var stored = JsonSerializer.Deserialize<StoryGraphReq>(json, JsonOptions);
            if (stored is null || stored.Id != Draft.Id)
                return;
            await ApplyRequest(stored).ConfigureAwait(false);
        }
        catch (Exception ex) {
            Logger.LogDebug(ex, "Story builder: could not load persisted state.");
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet() => Rebuild();

    /// <inheritdoc />
    public void Dispose()
    {
        _saveDebounceCts?.Cancel();
        _saveDebounceCts?.Dispose();
    }

    private void Rebuild()
    {
        _issues = [.. StoryGraphChecks.Check(Draft)];
        _fields = StoryConditionCatalog.Fields(Draft);
        _request = StoryDraftMap.ToRequest(Draft);
        _csharp = StoryGraphCsharp.Format(Draft);
    }

    private Task Touch()
    {
        Rebuild();
        SchedulePersist();
        return Task.CompletedTask;
    }

    private Task OnTitle(string value)
    {
        Draft.Title = value;
        return Touch();
    }

    private Task OnSlug(string value)
    {
        Draft.Slug = value;
        return Touch();
    }

    private Task OnStatus(StoryStatus status)
    {
        Draft.Status = status;
        return Touch();
    }

    private Task OnLanguage(string? value)
    {
        Draft.Language = value;
        return Touch();
    }

    private Task OnDescription(string? value)
    {
        Draft.Description = value;
        return Touch();
    }

    private Task SelectNode(string key)
    {
        _selectedKey = key;
        _choiceIndex = 0;
        return Task.CompletedTask;
    }

    private Task SelectChoice(int index)
    {
        _choiceIndex = index;
        return Task.CompletedTask;
    }

    private Task AddNode()
    {
        var key = "node_" + (Draft.Nodes.Count + 1);
        Draft.Nodes.Add(new() { Key = key });
        Draft.StartNodeKey ??= key;
        _selectedKey = key;
        return Touch();
    }

    private Task AddChoice()
    {
        if (Selected is not { } node)
            return Task.CompletedTask;
        var key = "choice_" + (node.Choices.Count + 1);
        node.Choices.Add(new() { Key = key, Prompt = key });
        _choiceIndex = node.Choices.Count - 1;
        return Touch();
    }

    private Task RemoveChoice()
    {
        if (Selected is not { } node || _choiceIndex < 0 || _choiceIndex >= node.Choices.Count)
            return Task.CompletedTask;
        node.Choices.RemoveAt(_choiceIndex);
        _choiceIndex = Math.Max(0, _choiceIndex - 1);
        return Touch();
    }

    private Task RemoveNode()
    {
        if (Selected is not { } node)
            return Task.CompletedTask;
        Draft.Nodes.Remove(node);
        if (Draft.StartNodeKey == node.Key)
            Draft.StartNodeKey = Draft.Nodes.FirstOrDefault()?.Key;
        _selectedKey = Draft.StartNodeKey;
        return Touch();
    }

    private Task MakeStart()
    {
        if (Selected is { } node)
            Draft.StartNodeKey = node.Key;
        return Touch();
    }

    private Task AddScene()
    {
        var key = "scene_" + (Draft.Scenes.Count + 1);
        Draft.Scenes.Add(new() { Key = key, Title = key, SortOrder = Draft.Scenes.Count });
        return Touch();
    }

    private Task ApplyRequest(StoryGraphReq? request)
    {
        if (request is null)
            return Task.CompletedTask;
        var id = Draft.Id;
        var tenant = Draft.TenantId;
        var game = Draft.GameKey;
        var next = StoryDraftMap.FromRequest(request);
        next.Id = request.Id == Guid.Empty ? id : request.Id;
        next.TenantId ??= tenant;
        next.GameKey ??= game;
        Draft.Title = next.Title;
        Draft.Slug = next.Slug;
        Draft.Description = next.Description;
        Draft.Language = next.Language;
        Draft.Status = next.Status;
        Draft.StartNodeKey = next.StartNodeKey;
        Draft.CoverImageRef = next.CoverImageRef;
        Draft.Scenes = next.Scenes;
        Draft.Nodes = next.Nodes;
        Draft.Variables = next.Variables;
        Draft.Actions = next.Actions;
        Draft.Reactions = next.Reactions;
        return Touch();
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Draft.Slug) && !string.IsNullOrWhiteSpace(Draft.Title))
            Draft.Slug = Slugify(Draft.Title);
        Rebuild();
        if (_issues.Count > 0)
            return;

        _saving = true;
        try {
            var saved = await StoryClient.SaveGraphAsync(_request);
            Draft.Id = saved.Id;
            Snackbar.Add("Story saved.", Severity.Success);
            await Saved.InvokeAsync(saved);
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _saving = false;
        }
    }

    private async Task CopyCsharpAsync()
    {
        try {
            await Js.InvokeVoidAsync("navigator.clipboard.writeText", _csharp);
            Snackbar.Add("Copied the StoryBuilder snippet.", Severity.Success);
        }
        catch (Exception ex) {
            Logger.LogDebug(ex, "Story builder: clipboard copy failed.");
            Snackbar.Add("Could not copy. Select the snippet instead.", Severity.Warning);
        }
    }

    private void SchedulePersist()
    {
        _saveDebounceCts?.Cancel();
        _saveDebounceCts?.Dispose();
        _saveDebounceCts = new();
        _ = DebounceSaveAsync(_saveDebounceCts.Token);
    }

    private async Task DebounceSaveAsync(CancellationToken ct)
    {
        try {
            await Task.Delay(SaveDebounceMs, ct);
            await ClientStore.SetStoryBuilderStateAsync(JsonSerializer.Serialize(_request, JsonOptions));
        }
        catch (OperationCanceledException) {
            // Replaced by a later edit.
        }
        catch (Exception ex) {
            Logger.LogDebug(ex, "Story builder: could not save state.");
        }
    }

    private static string Slugify(string title)
    {
        var chars = title.Trim().ToLowerInvariant().Select(character => char.IsLetterOrDigit(character) ? character : '-').ToArray();
        return new string(chars).Trim('-');
    }
}
