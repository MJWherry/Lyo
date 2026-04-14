using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lyo.Web.Components.Form;

public partial class LyoForm<TModel>
{
    public enum ChangeType
    {
        Create,
        Update,
        Delete
    }

    private readonly Dictionary<string, PropertyChange> _changes = new();
    private readonly List<OperationChange> _operations = [];

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public TModel? Model { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public EventCallback<SubmitContext> OnSubmit { get; set; }

    [Parameter]
    public EventCallback<bool> HasChangesChanged { get; set; }

    [Parameter]
    public EventCallback<int> ChangeCountChanged { get; set; }

    [Parameter]
    public EventCallback OnReset { get; set; }

    public bool HasChanges { get; private set; }

    public int ChangeCount { get; private set; }

    private void UpdateCache()
    {
        ChangeCount = _changes.Values.Count(change => change.HasChanged) + _operations.Count;
        HasChanges = ChangeCount > 0;
    }

    public void RegisterChange(string propertyName, object? originalValue, object? currentValue, bool hasChanged)
    {
        var previousHasChanges = HasChanges;
        var previousChangeCount = ChangeCount;
        if (hasChanged) {
            _changes[propertyName] = new() {
                PropertyName = propertyName,
                OriginalValue = originalValue,
                CurrentValue = currentValue,
                HasChanged = true
            };
        }
        else
            _changes.Remove(propertyName);

        UpdateCache();
        NotifyFormStateChanged(previousHasChanges, previousChangeCount);
        StateHasChanged();
    }

    public void AddChange(string propertyName, object? originalValue, object? currentValue)
    {
        var previousHasChanges = HasChanges;
        var previousChangeCount = ChangeCount;
        if (originalValue?.Equals(currentValue) == true)
            _changes.Remove(propertyName);
        else {
            _changes[propertyName] = new() {
                PropertyName = propertyName,
                OriginalValue = originalValue,
                CurrentValue = currentValue,
                HasChanged = true
            };
        }

        UpdateCache();
        NotifyFormStateChanged(previousHasChanges, previousChangeCount);
        StateHasChanged();
    }

    public void RemoveChange(string propertyName)
    {
        var previousHasChanges = HasChanges;
        var previousChangeCount = ChangeCount;
        if (_changes.Remove(propertyName)) {
            UpdateCache();
            NotifyFormStateChanged(previousHasChanges, previousChangeCount);
            StateHasChanged();
        }
    }

    public Dictionary<string, PropertyChange> GetChanges() => _changes.Where(entry => entry.Value.HasChanged).ToDictionary(entry => entry.Key, entry => entry.Value);

    public string AddOperation(ChangeType changeType, string description, Func<TModel?, Task> operation)
    {
        var previousHasChanges = HasChanges;
        var previousChangeCount = ChangeCount;
        var id = Guid.NewGuid().ToString();
        _operations.Add(
            new() {
                Id = id,
                ChangeType = changeType,
                Description = description,
                Operation = operation
            });

        UpdateCache();
        NotifyFormStateChanged(previousHasChanges, previousChangeCount);
        StateHasChanged();
        return id;
    }

    public bool RemoveOperation(string operationId)
    {
        var previousHasChanges = HasChanges;
        var previousChangeCount = ChangeCount;
        var removed = _operations.RemoveAll(operation => operation.Id == operationId) > 0;
        if (removed) {
            UpdateCache();
            NotifyFormStateChanged(previousHasChanges, previousChangeCount);
            StateHasChanged();
        }

        return removed;
    }

    public void ResetChanges()
    {
        var previousHasChanges = HasChanges;
        var previousChangeCount = ChangeCount;
        _changes.Clear();
        _operations.Clear();
        UpdateCache();
        NotifyFormStateChanged(previousHasChanges, previousChangeCount);
        if (OnReset.HasDelegate)
            _ = OnReset.InvokeAsync();

        StateHasChanged();
    }

    public List<OperationChange> GetOperations() => _operations.ToList();

    private async Task HandleSubmit()
    {
        if (!OnSubmit.HasDelegate)
            return;

        var changes = GetChanges();
        var operations = GetOperations();
        var message = BuildConfirmationMessage(changes, operations);
        var result = await DialogService.ShowMessageBoxAsync("Confirm Save", (MarkupString)message, "Save", cancelText: "Cancel");
        if (result == true)
            await OnSubmit.InvokeAsync(new() { PropertyChanges = changes, Operations = operations });
    }

    private static string BuildConfirmationMessage(Dictionary<string, PropertyChange> changes, List<OperationChange> operations)
    {
        var message = """
                      <p>Are you sure you want to make the following changes?</p>
                      <ul style='list-style-type:none; padding-left:0;'>
                      """;

        foreach (var change in changes) {
            message += $"""
                            <li style='margin-bottom:8px;'>
                                <b>{change.Key}</b>:
                                <span style='color:gray;'>'{change.Value.OriginalValue}'</span> &rarr;
                                <span style='color:green;'>'{change.Value.CurrentValue}'</span>
                            </li>
                        """;
        }

        foreach (var operation in operations) {
            var color = operation.ChangeType switch {
                ChangeType.Create => "green",
                ChangeType.Update => "blue",
                ChangeType.Delete => "red",
                var _ => "gray"
            };

            var icon = operation.ChangeType switch {
                ChangeType.Create => "+",
                ChangeType.Update => "~",
                ChangeType.Delete => "-",
                var _ => "?"
            };

            message += $"""
                            <li style='margin-bottom:8px;'>
                                <span style='color:{color}; font-weight:bold;'>[{icon}] {operation.ChangeType}</span>:
                                <b>{operation.Description}</b>
                            </li>
                        """;
        }

        return message + "</ul>";
    }

    private void NotifyFormStateChanged(bool previousHasChanges, int previousChangeCount)
    {
        if (previousHasChanges != HasChanges && HasChangesChanged.HasDelegate)
            _ = HasChangesChanged.InvokeAsync(HasChanges);

        if (previousChangeCount != ChangeCount && ChangeCountChanged.HasDelegate)
            _ = ChangeCountChanged.InvokeAsync(ChangeCount);
    }

    public class PropertyChange
    {
        public string PropertyName { get; set; } = string.Empty;

        public object? OriginalValue { get; set; }

        public object? CurrentValue { get; set; }

        public bool HasChanged { get; set; }
    }

    public class OperationChange
    {
        public string Id { get; set; } = string.Empty;

        public ChangeType ChangeType { get; set; }

        public string Description { get; set; } = string.Empty;

        public Func<TModel?, Task> Operation { get; set; } = null!;
    }

    public class SubmitContext
    {
        public Dictionary<string, PropertyChange> PropertyChanges { get; set; } = new();

        public List<OperationChange> Operations { get; set; } = new();
    }
}