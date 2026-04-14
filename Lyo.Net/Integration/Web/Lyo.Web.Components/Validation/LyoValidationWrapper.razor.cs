using Lyo.Common;
using Lyo.Validation;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lyo.Web.Components.Validation;

public partial class LyoValidationWrapper<T> : ComponentBase
{
    private T? _previousValue;
    private bool _hasValidated;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    [Parameter]
    public T? Value { get; set; }

    [Parameter]
    public IValidator<T>? Validator { get; set; }

    [Parameter]
    public Func<T, Result<T>>? ValidationFunc { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public bool ShowSnackbar { get; set; }

    [Parameter]
    public bool ValidateOnChange { get; set; } = true;

    [Parameter]
    public EventCallback<bool> IsValidChanged { get; set; }

    [Parameter]
    public EventCallback<IReadOnlyList<Error>> ErrorsChanged { get; set; }

    public bool IsValid { get; private set; } = true;

    public IReadOnlyList<Error> Errors { get; private set; } = [];

    protected override void OnParametersSet()
    {
        if (!ValidateOnChange)
            return;

        if (!_hasValidated && EqualityComparer<T>.Default.Equals(Value, default))
            return;

        if (_hasValidated && EqualityComparer<T>.Default.Equals(Value, _previousValue))
            return;

        _previousValue = Value;
        _hasValidated = true;
        RunValidation();
    }

    public void Validate()
    {
        _hasValidated = true;
        RunValidation();
    }

    public void Reset()
    {
        _hasValidated = false;
        _previousValue = default;
        SetResult(true, []);
        StateHasChanged();
    }

    private void RunValidation()
    {
        if (Value is null) {
            SetResult(true, []);
            return;
        }

        Result<T>? result = null;

        if (Validator is not null)
            result = Validator.Validate(Value);
        else if (ValidationFunc is not null)
            result = ValidationFunc(Value);

        if (result is null) {
            SetResult(true, []);
            return;
        }

        var errors = result.Errors ?? [];
        SetResult(result.IsSuccess, errors);

        if (!result.IsSuccess && ShowSnackbar) {
            foreach (var error in errors)
                Snackbar.Add(error.Message, Severity.Error);
        }
    }

    private void SetResult(bool isValid, IReadOnlyList<Error> errors)
    {
        var validChanged = IsValid != isValid;
        var errorsChanged = !ReferenceEquals(Errors, errors);

        IsValid = isValid;
        Errors = errors;

        if (validChanged && IsValidChanged.HasDelegate)
            _ = IsValidChanged.InvokeAsync(isValid);

        if (errorsChanged && ErrorsChanged.HasDelegate)
            _ = ErrorsChanged.InvokeAsync(errors);
    }
}
