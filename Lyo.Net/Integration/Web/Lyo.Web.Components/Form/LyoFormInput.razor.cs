using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.CSharp.RuntimeBinder;
using MudBlazor;

namespace Lyo.Web.Components.Form;

public partial class LyoFormInput<TModel, TValue>
{
    private readonly Type _propertyType = typeof(TValue);

    private TValue? _currentValue;

    private bool _isNullableProperty;

    private TValue? _originalValue;

    private string? _propertyName;

    private Type _underlyingType = typeof(TValue);

    [CascadingParameter(Name = "ChangeTrackingForm")]
    public dynamic? ParentForm { get; set; }

    [Parameter]
    [EditorRequired]
    public TModel? Model { get; set; }

    [Parameter]
    [EditorRequired]
    public Expression<Func<TModel, TValue>>? PropertyExpression { get; set; }

    [Parameter]
    public bool AllowEditing { get; set; }

    [Parameter]
    public string? Label { get; set; }

    [Parameter]
    public Variant Variant { get; set; } = Variant.Outlined;

    [Parameter]
    public Margin Margin { get; set; } = Margin.Dense;

    [Parameter]
    public EventCallback<ValueChangeInfo<TValue>> OnValueChanged { get; set; }

    [Parameter]
    public IReadOnlyList<TValue>? Items { get; set; }

    [Parameter]
    public bool AllowMultiline { get; set; }

    [Parameter]
    public bool UseRichTextEditor { get; set; }

    public bool HasChanged {
        get {
            // For value types and strings, use default equality
            if (_underlyingType.IsValueType || _underlyingType == typeof(string))
                return !EqualityComparer<TValue>.Default.Equals(_originalValue, _currentValue);

            // For reference types, might need custom comparison
            return !EqualityComparer<TValue>.Default.Equals(_originalValue, _currentValue);
        }
    }

    protected override void OnParametersSet()
    {
        if (PropertyExpression == null || Model == null)
            return;

        var func = PropertyExpression.Compile();
        var currentModelValue = func(Model);
        // Only set original value on first initialization
        if (_propertyName == null)
            _originalValue = currentModelValue;

        _currentValue = currentModelValue;
        _propertyName = GetPropertyName(PropertyExpression);
        _underlyingType = Nullable.GetUnderlyingType(_propertyType) ?? _propertyType;
        _isNullableProperty = IsNullableProperty(PropertyExpression);
    }

    private string GetPropertyName(Expression<Func<TModel, TValue>> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
            return memberExpression.Member.Name;

        return "Property";
    }

    private static bool IsNullableProperty(Expression<Func<TModel, TValue>> expression)
    {
        if (expression.Body is not MemberExpression { Member: PropertyInfo propertyInfo })
            return false;

        if (Nullable.GetUnderlyingType(propertyInfo.PropertyType) != null)
            return true;

        if (propertyInfo.PropertyType.IsValueType)
            return false;

        var nullability = new NullabilityInfoContext().Create(propertyInfo);
        return nullability.ReadState == NullabilityState.Nullable;
    }

    public ValueChangeInfo<TValue> GetChangeInfo()
        => new() {
            PropertyName = _propertyName ?? "Unknown",
            OriginalValue = _originalValue,
            CurrentValue = _currentValue,
            HasChanged = HasChanged
        };

    private async Task HandleValueChanged(TValue? newValue)
    {
        _currentValue = newValue;

        // Update the model property directly
        if (PropertyExpression != null && Model != null) {
            var memberExpression = PropertyExpression.Body as MemberExpression;
            if (memberExpression?.Member is PropertyInfo propertyInfo)
                propertyInfo.SetValue(Model, newValue);
        }

        // Notify parent form of change (if it exists and has RegisterChange method)
        if (ParentForm != null) {
            try {
                var changeInfo = GetChangeInfo();
                ParentForm.RegisterChange(changeInfo.PropertyName, changeInfo.OriginalValue, changeInfo.CurrentValue, changeInfo.HasChanged);
            }
            catch (RuntimeBinderException) {
                // ParentForm doesn't have RegisterChange method, ignore
            }
        }

        if (OnValueChanged.HasDelegate)
            await OnValueChanged.InvokeAsync(GetChangeInfo());

        StateHasChanged();
    }

    private bool IsFlagsEnum() => _underlyingType.IsEnum && _underlyingType.GetCustomAttributes(typeof(FlagsAttribute), false).Any();

    private RenderFragment RenderInput()
        => builder => {
            var displayLabel = Label ?? _propertyName ?? "Value";
            var isReadOnly = !AllowEditing;

            // If Items list is provided, render as selection
            if (Items != null && Items.Count > 0)
                RenderItemsSelection(builder, displayLabel, isReadOnly);
            else if (_underlyingType == typeof(bool))
                RenderBooleanInput(builder, displayLabel, isReadOnly);
            else if (_underlyingType.IsEnum)
                RenderEnumInput(builder, displayLabel, isReadOnly);
            else if (IsNumericType(_underlyingType))
                RenderNumericInput(builder, displayLabel, isReadOnly);
            else if (_underlyingType == typeof(DateTime))
                RenderDateTimeInput(builder, displayLabel, isReadOnly);
            else if (_underlyingType == typeof(DateOnly))
                RenderDateOnlyInput(builder, displayLabel, isReadOnly);
            else if (_underlyingType == typeof(TimeOnly))
                RenderTimeOnlyInput(builder, displayLabel, isReadOnly);
            else if (_underlyingType == typeof(Guid))
                RenderGuidInput(builder, displayLabel, isReadOnly);
            else if (_underlyingType == typeof(string))
                RenderStringInput(builder, displayLabel, isReadOnly);
            else
                RenderDefaultInput(builder, displayLabel, isReadOnly);
        };

    private void RenderItemsSelection(RenderTreeBuilder builder, string label, bool isReadOnly)
    {
        // Check if we're dealing with a collection type for multi-select
        var isMultiSelect = _propertyType.IsGenericType && (typeof(IEnumerable<>).IsAssignableFrom(_propertyType.GetGenericTypeDefinition()) ||
            _propertyType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)));

        if (isMultiSelect)
            RenderMultiSelectList(builder, label, isReadOnly);
        else
            RenderSingleSelectList(builder, label, isReadOnly);
    }

    private void RenderSingleSelectList(RenderTreeBuilder builder, string label, bool isReadOnly)
    {
        if (isReadOnly) {
            builder.OpenComponent<MudTextField<string>>(0);
            builder.AddAttribute(1, "Label", label);
            builder.AddAttribute(2, "Value", _currentValue?.ToString() ?? string.Empty);
            builder.AddAttribute(3, "ReadOnly", true);
            builder.AddAttribute(4, "Variant", Variant);
            builder.AddAttribute(5, "Margin", Margin);
            builder.CloseComponent();
        }
        else {
            builder.OpenComponent(0, typeof(MudSelect<TValue>));
            builder.AddAttribute(1, "Label", label);
            builder.AddAttribute(2, "Value", _currentValue);
            builder.AddAttribute(3, "Variant", Variant);
            builder.AddAttribute(4, "Margin", Margin);
            builder.AddAttribute(5, "ValueChanged", EventCallback.Factory.Create<TValue>(this, HandleValueChanged));
            builder.AddAttribute(
                6, "ChildContent", (RenderFragment)(childBuilder => {
                    var index = 0;
                    foreach (var item in Items!) {
                        childBuilder.OpenComponent(index++, typeof(MudSelectItem<TValue>));
                        childBuilder.AddAttribute(index++, "Value", item);
                        childBuilder.AddComponentReferenceCapture(index++, _ => { });
                        childBuilder.CloseComponent();
                    }
                }));

            builder.CloseComponent();
        }
    }

    private void RenderMultiSelectList(RenderTreeBuilder builder, string label, bool isReadOnly)
    {
        // Extract the actual collection from TValue
        var collection = _currentValue as IEnumerable<object>;
        var selectedItems = collection?.ToHashSet() ?? [];
        builder.OpenComponent(0, typeof(MudList<>).MakeGenericType(_underlyingType));
        builder.AddAttribute(1, "T", _underlyingType);
        builder.AddAttribute(2, "Clickable", !isReadOnly);
        builder.AddAttribute(3, "Dense", true);
        builder.AddAttribute(
            4, "ChildContent", (RenderFragment)(childBuilder => {
                var index = 0;
                foreach (var item in Items!) {
                    var isSelected = selectedItems.Contains(item!);
                    childBuilder.OpenComponent(index++, typeof(MudListItem<>).MakeGenericType(_underlyingType));
                    childBuilder.AddAttribute(index++, "Text", item?.ToString());
                    childBuilder.AddAttribute(
                        index++, "OnClick", EventCallback.Factory.Create(
                            this, async () => {
                                if (!isReadOnly) {
                                    if (isSelected)
                                        selectedItems.Remove(item!);
                                    else
                                        selectedItems.Add(item!);

                                    // Convert back to appropriate collection type
                                    var newValue = (TValue)Activator.CreateInstance(typeof(List<>).MakeGenericType(_underlyingType), selectedItems)!;
                                    await HandleValueChanged(newValue);
                                }
                            }));

                    if (isSelected)
                        childBuilder.AddAttribute(index++, "Icon", Icons.Material.Filled.CheckBox);

                    childBuilder.CloseComponent();
                }
            }));

        builder.CloseComponent();
    }

    private void RenderBooleanInput(RenderTreeBuilder builder, string label, bool isReadOnly)
    {
        builder.OpenComponent<MudSwitch<bool>>(0);
        builder.AddAttribute(1, "Label", label);
        builder.AddAttribute(2, "Checked", _currentValue != null && (bool)(object)_currentValue);
        builder.AddAttribute(3, "ReadOnly", isReadOnly);
        builder.AddAttribute(4, "Disabled", isReadOnly);
        builder.AddAttribute(5, "Color", Color.Primary);
        if (!isReadOnly) {
            builder.AddAttribute(
                6, "CheckedChanged", EventCallback.Factory.Create<bool>(
                    this, async newValue => {
                        await HandleValueChanged((TValue)(object)newValue);
                    }));
        }

        builder.CloseComponent();
    }

    private void RenderEnumInput(RenderTreeBuilder builder, string label, bool isReadOnly)
    {
        if (IsFlagsEnum()) {
            RenderFlagsEnumInput(builder, label, isReadOnly);
            return;
        }

        if (isReadOnly) {
            builder.OpenComponent<MudTextField<string>>(0);
            builder.AddAttribute(1, "Label", label);
            builder.AddAttribute(2, "Value", _currentValue?.ToString() ?? string.Empty);
            builder.AddAttribute(3, "ReadOnly", true);
            builder.AddAttribute(4, "Variant", Variant);
            builder.AddAttribute(5, "Margin", Margin);
            builder.CloseComponent();
        }
        else {
            builder.OpenComponent(0, typeof(MudSelect<>).MakeGenericType(_underlyingType));
            builder.AddAttribute(1, "Label", label);
            builder.AddAttribute(2, "Value", _currentValue);
            builder.AddAttribute(3, "Variant", Variant);
            builder.AddAttribute(4, "Margin", Margin);

            // Use reflection to create the properly typed EventCallback
            var eventCallbackFactoryMethod = typeof(EventCallbackFactory).GetMethods()
                .First(m => m.Name == "Create" && m.GetParameters().Length == 2 && m.IsGenericMethod)
                .MakeGenericMethod(_underlyingType);

            var handleMethod = GetType().GetMethod(nameof(HandleEnumValueChangedAsync), BindingFlags.NonPublic | BindingFlags.Instance)!.MakeGenericMethod(_underlyingType);
            var delegateType = typeof(Func<,>).MakeGenericType(_underlyingType, typeof(Task));
            var callback = Delegate.CreateDelegate(delegateType, this, handleMethod);
            var eventCallback = eventCallbackFactoryMethod.Invoke(EventCallback.Factory, [this, callback]);
            builder.AddAttribute(5, "ValueChanged", eventCallback);
            builder.AddAttribute(
                6, "ChildContent", (RenderFragment)(childBuilder => {
                    var enumValues = Enum.GetValues(_underlyingType);
                    var index = 0;
                    foreach (Enum enumValue in enumValues) {
                        childBuilder.OpenComponent(index++, typeof(MudSelectItem<>).MakeGenericType(_underlyingType));
                        childBuilder.AddAttribute(index++, "Value", enumValue);
                        childBuilder.CloseComponent();
                    }
                }));

            builder.CloseComponent();
        }
    }

    private async Task HandleEnumValueChangedAsync<TEnum>(TEnum newValue) => await HandleValueChanged((TValue)(object)newValue!);

    private void RenderFlagsEnumInput(RenderTreeBuilder builder, string label, bool isReadOnly)
    {
        if (isReadOnly) {
            var currentValue = _currentValue != null ? Convert.ToInt64(_currentValue) : 0L;
            var enumValues = Enum.GetValues(_underlyingType).Cast<object>().ToList();

            // Get selected flags for display
            var selectedFlags = enumValues.Where(ev => {
                    var flagValue = Convert.ToInt64(ev);
                    return flagValue != 0 && (currentValue & flagValue) == flagValue;
                })
                .Select(ev => ev.ToString())
                .ToList();

            var displayText = selectedFlags.Any() ? string.Join(", ", selectedFlags) : "None";
            builder.OpenComponent<MudTextField<string>>(0);
            builder.AddAttribute(1, "Label", label);
            builder.AddAttribute(2, "Value", displayText);
            builder.AddAttribute(3, "ReadOnly", true);
            builder.AddAttribute(4, "Variant", Variant);
            builder.AddAttribute(5, "Margin", Margin);
            builder.CloseComponent();
        }
        else {
            // Get current selected flags
            var currentValue = _currentValue != null ? Convert.ToInt64(_currentValue) : 0L;
            var enumValues = Enum.GetValues(_underlyingType).Cast<object>().ToList();
            var selectedFlags = enumValues.Where(ev => {
                    var flagValue = Convert.ToInt64(ev);
                    return flagValue != 0 && (currentValue & flagValue) == flagValue;
                })
                .ToList();

            var displayText = selectedFlags.Count switch {
                0 => "None selected",
                1 => selectedFlags[0].ToString() ?? "",
                var _ => $"{selectedFlags.Count} selected"
            };

            // Use MudMenu for dropdown behavior
            builder.OpenComponent<MudMenu>(0);
            builder.AddAttribute(1, "Dense", true);
            builder.AddAttribute(2, "FullWidth", true);
            // Activator - the button that shows current selection (MudBlazor 9: must explicitly wire OnClick)
            builder.AddAttribute(
                3, "ActivatorContent", (RenderFragment<MenuContext>)(context => activatorBuilder => {
                    activatorBuilder.OpenComponent<MudTextField<string>>(0);
                    activatorBuilder.AddAttribute(1, "Label", label);
                    activatorBuilder.AddAttribute(2, "Value", displayText);
                    activatorBuilder.AddAttribute(3, "ReadOnly", true);
                    activatorBuilder.AddAttribute(4, "Variant", Variant);
                    activatorBuilder.AddAttribute(5, "Margin", Margin);
                    activatorBuilder.AddAttribute(6, "Adornment", Adornment.End);
                    activatorBuilder.AddAttribute(7, "AdornmentIcon", Icons.Material.Filled.ArrowDropDown);
                    activatorBuilder.AddAttribute(8, "OnAdornmentClick", EventCallback.Factory.Create(this, () => context.ToggleAsync()));
                    activatorBuilder.CloseComponent();
                }));

            // Dropdown content with checkboxes
            builder.AddAttribute(
                4, "ChildContent", (RenderFragment)(menuBuilder => {
                    var menuIndex = 0;
                    // Create a list to hold all menu items
                    var menuItems = new List<(string name, long value)>();
                    foreach (var enumValue in enumValues) {
                        var flagValue = Convert.ToInt64(enumValue);
                        if (flagValue == 0)
                            continue;

                        menuItems.Add((enumValue.ToString() ?? "", flagValue));
                    }

                    foreach (var (enumName, flagValue) in menuItems) {
                        // Create a unique key for this iteration
                        var itemKey = $"flag_{flagValue}_{menuIndex}";
                        menuBuilder.OpenComponent<MudMenuItem>(menuIndex++);
                        // OnClick handler
                        menuBuilder.AddAttribute(
                            menuIndex++, "OnClick", EventCallback.Factory.Create(
                                this, async (MouseEventArgs args) => {
                                    // Read the current value fresh from _currentValue
                                    var currentVal = _currentValue != null ? Convert.ToInt64(_currentValue) : 0L;
                                    var isCurrentlyChecked = (currentVal & flagValue) == flagValue;
                                    var newValue = isCurrentlyChecked
                                        ? currentVal & ~flagValue // Remove flag
                                        : currentVal | flagValue; // Add flag

                                    var convertedValue = (TValue)Enum.ToObject(_underlyingType, newValue);
                                    await HandleValueChanged(convertedValue);
                                }));

                        // Child content with checkbox
                        menuBuilder.AddAttribute(
                            menuIndex++, "ChildContent", (RenderFragment)(itemBuilder => {
                                // Re-check the current state
                                var currentVal = _currentValue != null ? Convert.ToInt64(_currentValue) : 0L;
                                var isChecked = (currentVal & flagValue) == flagValue;
                                itemBuilder.OpenElement(0, "div");
                                itemBuilder.AddAttribute(1, "class", "d-flex align-center");
                                itemBuilder.AddAttribute(2, "style", "min-width: 200px;");
                                itemBuilder.OpenComponent<MudCheckBox<bool>>(3);
                                itemBuilder.AddAttribute(4, "Checked", isChecked);
                                itemBuilder.AddAttribute(5, "Dense", true);
                                itemBuilder.AddAttribute(6, "ReadOnly", false); // Changed from true
                                // Remove the pointer-events: none style
                                itemBuilder.CloseComponent();
                                itemBuilder.OpenElement(8, "span");
                                itemBuilder.AddAttribute(9, "class", "ml-2");
                                itemBuilder.AddContent(10, enumName);
                                itemBuilder.CloseElement();
                                itemBuilder.CloseElement();
                            }));

                        menuBuilder.CloseComponent();
                    }
                }));

            builder.CloseComponent();
        }
    }

    private void RenderNumericInput(RenderTreeBuilder builder, string label, bool isReadOnly)
    {
        builder.OpenComponent<MudNumericField<double?>>(0);
        builder.AddAttribute(1, "Label", label);
        builder.AddAttribute(2, "Value", _currentValue != null ? Convert.ToDouble(_currentValue) : null);
        builder.AddAttribute(3, "ReadOnly", isReadOnly);
        builder.AddAttribute(4, "Disabled", isReadOnly);
        builder.AddAttribute(5, "Variant", Variant);
        builder.AddAttribute(6, "Margin", Margin);
        if (!isReadOnly) {
            builder.AddAttribute(
                7, "ValueChanged", EventCallback.Factory.Create<double?>(
                    this, async newValue => {
                        if (newValue.HasValue) {
                            var convertedValue = (TValue)Convert.ChangeType(newValue.Value, _underlyingType);
                            await HandleValueChanged(convertedValue);
                        }
                        else
                            await HandleValueChanged(default);
                    }));
        }

        builder.CloseComponent();
    }

    private void RenderDateTimeInput(RenderTreeBuilder builder, string label, bool isReadOnly)
    {
        var dateTimeValue = _currentValue != null ? (DateTime)(object)_currentValue : (DateTime?)null;
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "d-flex flex-column gap-2");
        // Date picker
        builder.OpenComponent<MudDatePicker>(2);
        builder.AddAttribute(3, "Label", $"{label} (Date)");
        builder.AddAttribute(4, "Date", dateTimeValue);
        builder.AddAttribute(5, "ReadOnly", isReadOnly);
        builder.AddAttribute(6, "Disabled", isReadOnly);
        builder.AddAttribute(7, "Variant", Variant);
        builder.AddAttribute(8, "Margin", Margin);
        builder.AddAttribute(9, "Editable", !isReadOnly);
        if (!isReadOnly) {
            builder.AddAttribute(
                10, "DateChanged", EventCallback.Factory.Create<DateTime?>(
                    this, async newDate => {
                        if (newDate.HasValue) {
                            var currentTime = dateTimeValue?.TimeOfDay ?? TimeSpan.Zero;
                            var newDateTime = newDate.Value.Date.Add(currentTime);
                            await HandleValueChanged((TValue)(object)newDateTime);
                        }
                    }));
        }

        builder.CloseComponent();
        // Time picker
        builder.OpenComponent<MudTimePicker>(11);
        builder.AddAttribute(12, "Label", $"{label} (Time)");
        builder.AddAttribute(13, "Time", dateTimeValue?.TimeOfDay);
        builder.AddAttribute(14, "ReadOnly", isReadOnly);
        builder.AddAttribute(15, "Disabled", isReadOnly);
        builder.AddAttribute(16, "Variant", Variant);
        builder.AddAttribute(17, "Margin", Margin);
        builder.AddAttribute(18, "Editable", !isReadOnly);
        if (!isReadOnly) {
            builder.AddAttribute(
                19, "TimeChanged", EventCallback.Factory.Create<TimeSpan?>(
                    this, async newTime => {
                        if (newTime.HasValue) {
                            var currentDate = dateTimeValue?.Date ?? DateTime.Today;
                            var newDateTime = currentDate.Add(newTime.Value);
                            await HandleValueChanged((TValue)(object)newDateTime);
                        }
                    }));
        }

        builder.CloseComponent();
        builder.CloseElement();
    }

    private void RenderDateOnlyInput(RenderTreeBuilder builder, string label, bool isReadOnly)
    {
        var dateOnlyValue = _currentValue != null ? (DateOnly)(object)_currentValue : (DateOnly?)null;
        var dateTimeValue = dateOnlyValue?.ToDateTime(TimeOnly.MinValue);
        builder.OpenComponent<MudDatePicker>(0);
        builder.AddAttribute(1, "Label", label);
        builder.AddAttribute(2, "Date", dateTimeValue);
        builder.AddAttribute(3, "ReadOnly", isReadOnly);
        builder.AddAttribute(4, "Disabled", isReadOnly);
        builder.AddAttribute(5, "Variant", Variant);
        builder.AddAttribute(6, "Margin", Margin);
        builder.AddAttribute(7, "Editable", !isReadOnly);
        if (!isReadOnly) {
            builder.AddAttribute(
                8, "DateChanged", EventCallback.Factory.Create<DateTime?>(
                    this, async newDate => {
                        if (newDate.HasValue) {
                            var newDateOnly = DateOnly.FromDateTime(newDate.Value);
                            await HandleValueChanged((TValue)(object)newDateOnly);
                        }
                        else
                            await HandleValueChanged(default);
                    }));
        }

        builder.CloseComponent();
    }

    private void RenderTimeOnlyInput(RenderTreeBuilder builder, string label, bool isReadOnly)
    {
        var timeOnlyValue = _currentValue != null ? (TimeOnly)(object)_currentValue : (TimeOnly?)null;
        var timeSpanValue = timeOnlyValue?.ToTimeSpan();
        builder.OpenComponent<MudTimePicker>(0);
        builder.AddAttribute(1, "Label", label);
        builder.AddAttribute(2, "Time", timeSpanValue);
        builder.AddAttribute(3, "ReadOnly", isReadOnly);
        builder.AddAttribute(4, "Disabled", isReadOnly);
        builder.AddAttribute(5, "Variant", Variant);
        builder.AddAttribute(6, "Margin", Margin);
        builder.AddAttribute(7, "Editable", !isReadOnly);
        if (!isReadOnly) {
            builder.AddAttribute(
                8, "TimeChanged", EventCallback.Factory.Create<TimeSpan?>(
                    this, async newTime => {
                        if (newTime.HasValue) {
                            var newTimeOnly = TimeOnly.FromTimeSpan(newTime.Value);
                            await HandleValueChanged((TValue)(object)newTimeOnly);
                        }
                        else
                            await HandleValueChanged(default);
                    }));
        }

        builder.CloseComponent();
    }

    private void RenderGuidInput(RenderTreeBuilder builder, string label, bool isReadOnly)
    {
        builder.OpenComponent<MudTextField<string>>(0);
        builder.AddAttribute(1, "Label", label);
        builder.AddAttribute(2, "Value", _currentValue?.ToString() ?? string.Empty);
        builder.AddAttribute(3, "ReadOnly", isReadOnly);
        builder.AddAttribute(4, "Disabled", isReadOnly);
        builder.AddAttribute(5, "Variant", Variant);
        builder.AddAttribute(6, "Margin", Margin);
        builder.AddAttribute(7, "Adornment", Adornment.Start);
        builder.AddAttribute(8, "AdornmentIcon", Icons.Material.Filled.Key);
        if (!isReadOnly && _isNullableProperty) {
            builder.AddAttribute(9, "Adornment", Adornment.End);
            builder.AddAttribute(10, "AdornmentIcon", Icons.Material.Filled.Clear);
            builder.AddAttribute(
                11, "OnAdornmentClick", EventCallback.Factory.Create<MouseEventArgs>(
                    this, async _ => {
                        await HandleValueChanged(default);
                    }));
        }

        if (!isReadOnly) {
            builder.AddAttribute(
                12, "ValueChanged", EventCallback.Factory.Create<string>(
                    this, async newValue => {
                        if (string.IsNullOrWhiteSpace(newValue) && _isNullableProperty)
                            await HandleValueChanged(default);
                        else if (Guid.TryParse(newValue, out var guid))
                            await HandleValueChanged((TValue)(object)guid);
                    }));
        }

        builder.CloseComponent();
    }

    private void RenderStringInput(RenderTreeBuilder builder, string label, bool isReadOnly)
    {
        var stringValue = _currentValue?.ToString() ?? string.Empty;
        var shouldUseMultiline = AllowMultiline || stringValue.Length > 100 || stringValue.Contains('\n');
        if (_isNullableProperty && !shouldUseMultiline && !UseRichTextEditor) {
            builder.OpenComponent<LyoNullableTextField>(0);
            builder.AddAttribute(1, "Label", label);
            builder.AddAttribute(2, "Value", (string?)(object?)_currentValue);
            builder.AddAttribute(3, "ReadOnly", isReadOnly);
            builder.AddAttribute(4, "Disabled", isReadOnly);
            builder.AddAttribute(5, "Variant", Variant);
            builder.AddAttribute(6, "Margin", Margin);
            if (!isReadOnly) {
                builder.AddAttribute(
                    7, "ValueChanged", EventCallback.Factory.Create<string?>(
                        this, async newValue => {
                            await HandleValueChanged((TValue?)(object?)newValue);
                        }));
            }

            builder.CloseComponent();
            return;
        }

        if (UseRichTextEditor && !isReadOnly) {
            // Use MudRichTextEdit if available
            builder.OpenComponent<MudTextField<string>>(0);
            builder.AddAttribute(1, "Label", label);
            builder.AddAttribute(2, "Value", stringValue);
            builder.AddAttribute(3, "ReadOnly", isReadOnly);
            builder.AddAttribute(4, "Disabled", isReadOnly);
            builder.AddAttribute(5, "Variant", Variant);
            builder.AddAttribute(6, "Margin", Margin);
            builder.AddAttribute(7, "Lines", 10);
            builder.AddAttribute(8, "MaxLines", 20);
            builder.AddAttribute(9, "Placeholder", "Enter rich text...");
            builder.AddAttribute(
                10, "ValueChanged", EventCallback.Factory.Create<string>(
                    this, async newValue => {
                        await HandleValueChanged((TValue)(object)newValue);
                    }));

            builder.CloseComponent();
        }
        else {
            builder.OpenComponent<MudTextField<string>>(0);
            builder.AddAttribute(1, "Label", label);
            builder.AddAttribute(2, "Value", stringValue);
            builder.AddAttribute(3, "ReadOnly", isReadOnly);
            builder.AddAttribute(4, "Disabled", isReadOnly);
            builder.AddAttribute(5, "Variant", Variant);
            builder.AddAttribute(6, "Margin", Margin);
            if (shouldUseMultiline) {
                builder.AddAttribute(7, "Lines", 8);
                builder.AddAttribute(8, "MaxLines", 15);
            }

            if (!isReadOnly) {
                builder.AddAttribute(
                    9, "ValueChanged", EventCallback.Factory.Create<string>(
                        this, async newValue => {
                            await HandleValueChanged((TValue)(object)newValue);
                        }));
            }

            builder.CloseComponent();
        }
    }

    private void RenderDefaultInput(RenderTreeBuilder builder, string label, bool isReadOnly)
    {
        builder.OpenComponent<MudTextField<string>>(0);
        builder.AddAttribute(1, "Label", label);
        builder.AddAttribute(2, "Value", _currentValue?.ToString() ?? string.Empty);
        builder.AddAttribute(3, "ReadOnly", true);
        builder.AddAttribute(4, "Disabled", true);
        builder.AddAttribute(5, "Variant", Variant);
        builder.AddAttribute(6, "Margin", Margin);
        builder.CloseComponent();
    }

    private bool IsNumericType(Type type)
        => type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte) || type == typeof(uint) || type == typeof(ulong) ||
            type == typeof(ushort) || type == typeof(sbyte) || type == typeof(float) || type == typeof(double) || type == typeof(decimal);

    public class ValueChangeInfo<T>
    {
        public string PropertyName { get; set; } = string.Empty;

        public T? OriginalValue { get; set; }

        public T? CurrentValue { get; set; }

        public bool HasChanged { get; set; }
    }
}