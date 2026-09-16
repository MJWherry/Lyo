using Lyo.Api.Models.Common.Response;
using Lyo.Query.Models.Common.Request;
using Lyo.Recipe.Models;
using Lyo.Recipe.Models.Enums;

namespace Lyo.Recipe.Web.Components;

/// <summary>Walks one saved recipe: ingredient overview, then one step at a time with its photos and a countdown.</summary>
public partial class RecipeCookView : IAsyncDisposable
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    /// <summary>HTTP client for attachment query.</summary>
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    /// <summary>Typed client for lines and steps.</summary>
    [Parameter]
    [EditorRequired]
    public IRecipeClient RecipeClient { get; set; } = null!;

    /// <summary>Recipe route prefix.</summary>
    [Parameter]
    public string BaseRoute { get; set; } = "Recipe";

    /// <summary>FileStorage MapGroup prefix.</summary>
    [Parameter]
    public string FileStorageApiRoutePrefix { get; set; } = "FileStorage";

    /// <summary>Recipe being cooked.</summary>
    [Parameter]
    [EditorRequired]
    public RecipeRes Recipe { get; set; } = null!;

    private bool _loading = true;
    private int _index = -1;
    private int? _cookingFor;
    private Dictionary<Guid, string> _ingredientNames = [];
    private List<RecipeIngredientLineRes> _lines = [];
    private List<RecipeStepRes> _steps = [];
    private List<RecipeAttachmentRes> _recipeFiles = [];
    private Dictionary<Guid, List<RecipeAttachmentRes>> _stepFiles = [];
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _timerCts;
    private bool _timerRunning;
    private int _remainingSeconds;

    private string ApiOrigin
    {
        get
        {
            if (Uri.TryCreate(BaseRoute, UriKind.Absolute, out var uri))
                return uri.GetLeftPart(UriPartial.Authority);
            return "";
        }
    }

    protected override async Task OnInitializedAsync()
    {
        _cookingFor = Recipe.Servings is > 0 ? Recipe.Servings : 1;
        var ingredients = await ApiClient.QueryConcreteAsync<QueryRes<IngredientRes>>($"{BaseRoute.TrimEnd('/')}/Ingredients", new QueryConcreteReq { Amount = 500 });
        _ingredientNames = (ingredients?.Items ?? []).ToDictionary(i => i.Id, i => i.Name);
        _lines = (await RecipeClient.GetIngredientLinesAsync(Recipe.Id)).ToList();
        _steps = (await RecipeClient.GetStepsAsync(Recipe.Id)).OrderBy(s => s.SortOrder).ThenBy(s => s.Id).ToList();
        var attachments = await ApiClient.QueryConcreteAsync<QueryRes<RecipeAttachmentRes>>(
            $"{BaseRoute.TrimEnd('/')}/Attachments", new QueryConcreteReq { Amount = 500 });
        var rows = attachments?.Items ?? [];
        _recipeFiles = rows.Where(a => a.EntityType == RecipeEntityTypes.Recipe && a.EntityId == Recipe.Id).ToList();
        _stepFiles = rows.Where(a => a.EntityType == RecipeEntityTypes.RecipeStep)
            .GroupBy(a => a.EntityId)
            .ToDictionary(g => g.Key, g => g.ToList());
        _loading = false;
    }

    private void Close() => MudDialog.Close(DialogResult.Ok(true));

    private void Back()
    {
        if (_index >= 0)
            _index--;
        StopTimer();
    }

    private void Next()
    {
        if (_index < _steps.Count - 1)
            _index++;
        StopTimer();
    }

    private RecipeStepRes? CurrentStep => _index >= 0 && _index < _steps.Count ? _steps[_index] : null;

    private IEnumerable<RecipeAttachmentRes> OtherImages(Guid stepId)
    {
        var files = FilesFor(stepId);
        var primary = PrimaryImage(files);
        return files.Where(f => f.Role == RecipeAttachmentRole.Image && f.Id != primary?.Id);
    }

    private IEnumerable<RecipeIngredientLineRes> LinesFor(Guid stepId)
        => _lines.Where(l => l.StepIds.Contains(stepId));

    private IReadOnlyList<int> StepNumbers(RecipeIngredientLineRes line)
        => line.StepIds.Select(id => _steps.FindIndex(s => s.Id == id)).Where(i => i >= 0).Select(i => i + 1).ToList();

    private IReadOnlyList<RecipeAttachmentRes> FilesFor(Guid stepId)
        => _stepFiles.TryGetValue(stepId, out var files) ? files : [];

    private static RecipeAttachmentRes? PrimaryImage(IEnumerable<RecipeAttachmentRes> files)
    {
        var images = files.Where(f => f.Role == RecipeAttachmentRole.Image).ToList();
        return images.FirstOrDefault(f => f.IsPrimary) ?? images.FirstOrDefault();
    }

    private string Download(Guid fileId) => RecipeFileLinks.DownloadUrl(FileStorageApiRoutePrefix, ApiOrigin, fileId, true);

    private string FormatLine(RecipeIngredientLineRes line)
    {
        var name = line.IngredientText;
        if (line.IngredientId is Guid ingredientId && _ingredientNames.TryGetValue(ingredientId, out var catalogName))
            name = catalogName;
        if (string.IsNullOrWhiteSpace(name))
            name = "Ingredient";
        var qty = RecipeQuantityScale.Scale(line.Quantity, Recipe.Servings, _cookingFor);
        var amount = qty is null ? "" : $"{qty:0.##} {line.Unit}".Trim();
        var text = string.IsNullOrWhiteSpace(amount) ? name : $"{amount} {name}";
        if (!string.IsNullOrWhiteSpace(line.Preparation))
            text += $", {line.Preparation}";
        if (line.IsOptional)
            text += " (optional)";
        return text;
    }

    private string FormatRemaining()
    {
        var minutes = _remainingSeconds / 60;
        var seconds = _remainingSeconds % 60;
        return $"{minutes}:{seconds:00}";
    }

    private async Task StartTimer(int minutes)
    {
        StopTimer();
        _remainingSeconds = minutes * 60;
        _timerRunning = true;
        _timerCts = new CancellationTokenSource();
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        var token = _timerCts.Token;
        try {
            while (await _timer.WaitForNextTickAsync(token)) {
                _remainingSeconds--;
                if (_remainingSeconds <= 0) {
                    _timerRunning = false;
                    await InvokeAsync(StateHasChanged);
                    break;
                }

                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException) {
            // Pause or leaving the step stops the countdown.
        }
    }

    private void PauseTimer() => StopTimer();

    private void StopTimer()
    {
        _timerRunning = false;
        _timerCts?.Cancel();
        _timer?.Dispose();
        _timer = null;
        _timerCts?.Dispose();
        _timerCts = null;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        StopTimer();
        return ValueTask.CompletedTask;
    }
}
