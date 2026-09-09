using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lyo.Common.Core.Conversion;
using Lyo.Common.Core.Enums;
using Lyo.Common.Metadata.Records;
using Lyo.Parameters;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Web.Components.LyoType;

public partial class LyoTypeValueInput
{
    private enum EditorMode
    {
        Scalar,
        Collection,
        JsonTree
    }

    /// <summary>Stored CLR FullName for the JSON payload.</summary>
    [Parameter]
    [EditorRequired]
    public string TypeName { get; set; } = "";

    /// <summary>JSON text (<c>42</c>, <c>"hello"</c>, <c>true</c>, <c>[]</c>, <c>{}</c>).</summary>
    [Parameter]
    public string? Json { get; set; }

    /// <summary>Fired after the JSON string changes.</summary>
    [Parameter]
    public EventCallback<string?> JsonChanged { get; set; }

    [Parameter]
    public string? Label { get; set; }

    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>If true, scalar text uses a password-style input. The client still edits plaintext; the API encrypts at rest.</summary>
    [Parameter]
    public bool IsEncrypted { get; set; }

    [Parameter]
    public bool Required { get; set; }

    /// <summary>
    /// If true, empty JSON stays unset (definition defaults). Numeric/date/chip editors can clear back to null instead of type zeros / <c>[]</c>.
    /// </summary>
    [Parameter]
    public bool AllowUnset { get; set; }

    /// <summary>Height of the JSON editor surface when the type needs a JSON tree.</summary>
    [Parameter]
    public string EditorSurfaceHeight { get; set; } = "180px";

    /// <summary>Fired when the JSON editor parse state changes. True means the current JSON is invalid.</summary>
    [Parameter]
    public EventCallback<bool> ParseErrorChanged { get; set; }

    /// <summary>
    /// Sample data a registered formatter editor uses for autocomplete and live preview: a dictionary or DTO whose paths mirror what the template will resolve
    /// against at run time. Ignored when the type is not formatter-typed or no formatter editor is registered.
    /// </summary>
    [Parameter]
    public object? FormatterContext { get; set; }

    /// <summary>Resolved with <c>GetService</c> so hosts that register no value editors keep the included fields.</summary>
    [Inject]
    private IServiceProvider Services { get; set; } = null!;

    private static readonly JsonSerializerOptions EditorJsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    private JsonNode? _node;
    private string? _lastJson;
    private string? _lastType;
    private int _editorKey;
    private bool _hasParseError;
    private EditorMode _mode = EditorMode.Scalar;
    private LyoTypeEditorKind _editorKind = LyoTypeEditorKind.Text;
    private bool _boolValue;
    private long? _longValue;
    private decimal? _decimalValue;
    private DateTime? _dateValue;
    private TimeSpan? _timeValue;
    private string _textValue = "";
    private IReadOnlyList<string> _chips = [];
    private string? _chipPattern;
    private string? _chipMessage;
    private string? _scalarError;
    private Type? _enumType;
    private string[] _enumNames = [];
    private string? _enumHelper;

    /// <summary>True when the JSON editor currently has a parse error.</summary>
    public bool HasParseError => _hasParseError;

    private string ResolvedLabel => string.IsNullOrWhiteSpace(Label) ? "Value" : Label;

    private Type? FormatterEditorType
        => _editorKind == LyoTypeEditorKind.Formatter
            && Services.GetService<LyoValueEditorCatalog>() is { } catalog
            && catalog.TryGet(LyoTypeEditorKind.Formatter, out var component)
                ? component
                : null;

    /// <summary>Contract documented on <see cref="ILyoValueEditorDescriptor" />. Rebuilt per draw, which is what <c>DynamicComponent</c> expects.</summary>
    private Dictionary<string, object?> FormatterEditorParameters
        => new(StringComparer.Ordinal) {
            ["Json"] = Json,
            ["JsonChanged"] = EventCallback.Factory.Create<string?>(this, CommitJson),
            ["Label"] = ResolvedLabel,
            ["ReadOnly"] = ReadOnly,
            ["Required"] = Required,
            ["Context"] = FormatterContext
        };

    /// <summary>Default JSON payload for a CLR type name (catalog <see cref="LyoTypeInfo.DefaultJson" />, or <c>null</c> JSON for unknown types).</summary>
    public static string DefaultJsonForType(string? typeName) => LyoTypeUi.DefaultJson(typeName);

    protected override void OnParametersSet()
    {
        if (string.Equals(_lastJson, Json, StringComparison.Ordinal) && string.Equals(_lastType, TypeName, StringComparison.Ordinal))
            return;

        _lastJson = Json;
        _lastType = TypeName;
        BindFromValue();
    }

    private void BindFromValue()
    {
        var known = ResolveKnown(TypeName);
        _editorKind = known.EditorKind;
        (_chipPattern, _chipMessage) = LyoTypeUi.ChipValidation(TypeName);
        _enumType = null;
        _enumNames = [];
        _enumHelper = null;
        _scalarError = null;
        if (known.EditorKind == LyoTypeEditorKind.Collection) {
            _mode = EditorMode.Collection;
            _chips = ParameterListJson.Parse(Json);
            return;
        }

        if (known.EditorKind is LyoTypeEditorKind.JsonObject or LyoTypeEditorKind.JsonArray or LyoTypeEditorKind.Binary || known == LyoTypeInfo.Unknown) {
            _mode = EditorMode.JsonTree;
            _node = TryParse(Json);
            _editorKey++;
            return;
        }

        if (known.EditorKind == LyoTypeEditorKind.Enum) {
            _mode = EditorMode.Scalar;
            BindEnum(TypeName);
            LoadScalar(_enumType ?? typeof(string));
            return;
        }

        _mode = EditorMode.Scalar;
        LoadScalar(ResolveRuntimeType() ?? known.Type);
    }

    private static LyoTypeInfo ResolveKnown(string? typeName)
    {
        if (LyoTypeInfo.TryFromName(typeName, out var known))
            return known;

        if (string.IsNullOrWhiteSpace(typeName))
            return LyoTypeInfo.Unknown;

        var type = LyoTypeInfo.TryResolveClrType(typeName);
        return type == null ? LyoTypeInfo.Unknown : LyoTypeInfo.FromType(type);
    }

    private Type? ResolveRuntimeType()
    {
        var resolved = LyoTypeInfo.TryResolveClrType(TypeName);
        if (resolved != null)
            return resolved;

        return LyoTypeInfo.TryFromName(TypeName, out var known) && known != LyoTypeInfo.Enum && known != LyoTypeInfo.Unknown ? known.Type : null;
    }

    private void BindEnum(string? typeName)
    {
        var runtime = LyoTypeInfo.TryResolveClrType(typeName);
        _enumType = runtime?.IsEnum == true ? runtime : null;
        _enumNames = _enumType != null ? System.Enum.GetNames(_enumType) : [];
        _enumHelper = _enumType != null
            ? null
            : string.Equals(typeName?.Trim(), LyoTypeInfo.Enum.FullName, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(typeName)
                ? "Enter a concrete enum CLR FullName to pick a member."
                : "Type is not loaded in this app. Enter a member name.";
    }

    private void LoadScalar(Type type)
    {
        if (AllowUnset && string.IsNullOrWhiteSpace(Json)) {
            _boolValue = false;
            _longValue = null;
            _decimalValue = null;
            _dateValue = null;
            _timeValue = null;
            _textValue = "";
            _scalarError = null;
            return;
        }

        try {
            var json = string.IsNullOrWhiteSpace(Json) ? LyoTypeUi.DefaultJson(TypeName) : Json;
            var deserialized = JsonSerializer.Deserialize(json, type, EditorJsonOptions);
            switch (_editorKind) {
                case LyoTypeEditorKind.Boolean:
                    _boolValue = deserialized is true;
                    break;
                case LyoTypeEditorKind.Integer:
                    _longValue = deserialized is IConvertible c ? c.ToInt64(CultureInfo.InvariantCulture) : null;
                    break;
                case LyoTypeEditorKind.Decimal:
                    _decimalValue = deserialized is IConvertible d ? d.ToDecimal(CultureInfo.InvariantCulture) : null;
                    break;
                case LyoTypeEditorKind.DateTime:
                    if (deserialized is DateTime dt) {
                        _dateValue = dt;
                        _timeValue = dt.TimeOfDay;
                    }
                    else if (deserialized is DateTimeOffset dto) {
                        _dateValue = dto.UtcDateTime;
                        _timeValue = dto.UtcDateTime.TimeOfDay;
                    }
                    else {
                        _dateValue = null;
                        _timeValue = null;
                    }

                    break;
                case LyoTypeEditorKind.DateOnly:
                    _dateValue = deserialized is DateOnly dateOnly ? dateOnly.ToDateTime(TimeOnly.MinValue) : null;
                    break;
                case LyoTypeEditorKind.TimeOnly:
                    _timeValue = deserialized is TimeOnly timeOnly ? timeOnly.ToTimeSpan() : null;
                    break;
                case LyoTypeEditorKind.Enum:
                    _textValue = deserialized != null && type.IsEnum
                        ? System.Enum.GetName(type, deserialized) ?? deserialized.ToString() ?? ""
                        : UnwrapJsonString(json);
                    break;
                case LyoTypeEditorKind.Guid:
                    _textValue = deserialized is Guid g ? g.ToString() : UnwrapJsonString(json);
                    if (!AllowUnset || !string.IsNullOrWhiteSpace(_textValue))
                        _scalarError = LyoTypeInfo.IsValidGuidText(_textValue) && !string.IsNullOrWhiteSpace(_textValue) ? null : "Enter a GUID.";
                    break;
                case LyoTypeEditorKind.Uri:
                    _textValue = deserialized as Uri is { } uri ? uri.ToString() : UnwrapJsonString(json);
                    if (!AllowUnset || !string.IsNullOrWhiteSpace(_textValue))
                        _scalarError = LyoTypeInfo.IsValidUriText(_textValue) && !string.IsNullOrWhiteSpace(_textValue) ? null : "Enter an absolute URI or a path with /.";
                    break;
                default:
                    _textValue = deserialized as string ?? deserialized?.ToString() ?? UnwrapJsonString(json);
                    break;
            }
        }
        catch (JsonException) {
            _textValue = UnwrapJsonString(Json);
            if (_editorKind == LyoTypeEditorKind.Guid)
                _scalarError = "Enter a GUID.";
            else if (_editorKind == LyoTypeEditorKind.Uri)
                _scalarError = "Enter an absolute URI or a path with /.";
        }

        _hasParseError = !string.IsNullOrEmpty(_scalarError);
    }

    private static string UnwrapJsonString(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return "";

        try {
            return JsonSerializer.Deserialize<string>(json) ?? json.Trim().Trim('"');
        }
        catch (JsonException) {
            return json.Trim().Trim('"');
        }
    }

    private static JsonNode? TryParse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "null")
            return null;

        try {
            return JsonNode.Parse(json);
        }
        catch (JsonException) {
            return null;
        }
    }

    private async Task CommitJson(string? json)
    {
        _lastJson = json;
        await JsonChanged.InvokeAsync(json);
    }

    private Task CommitUnsetOrNullJson() => CommitJson(AllowUnset ? null : "null");

    private async Task SetScalarError(string? error)
    {
        var hasError = !string.IsNullOrEmpty(error);
        _scalarError = error;
        if (_hasParseError == hasError)
            return;

        _hasParseError = hasError;
        await ParseErrorChanged.InvokeAsync(hasError);
    }

    private Task OnBoolChanged(bool value) => CommitJson(JsonSerializer.Serialize(value, EditorJsonOptions));

    private Task OnLongChanged(long? value)
    {
        if (value is null)
            return CommitUnsetOrNullJson();

        var type = ResolveRuntimeType() ?? typeof(long);
        object boxed = type == typeof(byte) ? (byte)value.Value : type == typeof(short) ? (short)value.Value : type == typeof(int) ? (int)value.Value : value.Value;
        return CommitJson(JsonSerializer.Serialize(boxed, type, EditorJsonOptions));
    }

    private Task OnDecimalChanged(decimal? value)
    {
        if (value is null)
            return CommitUnsetOrNullJson();

        var type = ResolveRuntimeType() ?? typeof(decimal);
        object boxed = type == typeof(float) ? (float)value.Value : type == typeof(double) ? (double)value.Value : value.Value;
        return CommitJson(JsonSerializer.Serialize(boxed, type, EditorJsonOptions));
    }

    private async Task OnTextChanged(string value)
    {
        _textValue = value;
        if (AllowUnset && string.IsNullOrWhiteSpace(value)) {
            await SetScalarError(null);
            await CommitJson(null);
            return;
        }

        if (_editorKind == LyoTypeEditorKind.Guid) {
            if (!Guid.TryParse(value, out var guid)) {
                await SetScalarError("Enter a GUID.");
                return;
            }

            await SetScalarError(null);
            await CommitJson(JsonSerializer.Serialize(guid, EditorJsonOptions));
            return;
        }

        if (_editorKind == LyoTypeEditorKind.Uri) {
            if (string.IsNullOrWhiteSpace(value) || !LyoTypeInfo.IsValidUriText(value) || !Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out var uri)) {
                await SetScalarError("Enter an absolute URI or a path with /.");
                return;
            }

            await SetScalarError(null);
            await CommitJson(JsonSerializer.Serialize(uri, EditorJsonOptions));
            return;
        }

        var type = ResolveRuntimeType() ?? typeof(string);
        if (type == typeof(TimeSpan) && TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var ts))
            await CommitJson(JsonSerializer.Serialize(ts, EditorJsonOptions));
        else if (type == typeof(string) || _editorKind is LyoTypeEditorKind.Regex or LyoTypeEditorKind.Xml or LyoTypeEditorKind.Formatter)
            await CommitJson(JsonSerializer.Serialize(value ?? "", EditorJsonOptions));
        else if (type.IsEnum)
            await CommitEnumAsync(type, value);
        else
            await CommitJson(JsonSerializer.Serialize(value ?? "", EditorJsonOptions));
    }

    private Task OnEnumNameChanged(string? value)
    {
        _textValue = value ?? "";
        if (AllowUnset && string.IsNullOrWhiteSpace(value))
            return CommitJson(null);

        return _enumType != null ? CommitEnumAsync(_enumType, value) : CommitJson(JsonSerializer.Serialize(value, EditorJsonOptions));
    }

    private async Task CommitEnumAsync(Type enumType, string? value)
    {
        // TypeConversion accepts either a member name or its numeric value, so both editor spellings land on the same enum member.
        if (TypeConversion.TryConvertTo(value, enumType, out var parsed) && parsed != null)
            await CommitJson(JsonSerializer.Serialize(parsed, enumType, EditorJsonOptions));
        else
            await CommitJson(JsonSerializer.Serialize(value, EditorJsonOptions));
    }

    private async Task OnDateChanged(DateTime? date)
    {
        _dateValue = date;
        await CommitDateTimeAsync();
    }

    private async Task OnTimeChanged(TimeSpan? time)
    {
        _timeValue = time;
        await CommitDateTimeAsync();
    }

    private async Task CommitDateTimeAsync()
    {
        if (_dateValue == null) {
            await CommitUnsetOrNullJson();
            return;
        }

        var combined = _dateValue.Value.Date.Add(_timeValue ?? TimeSpan.Zero);
        var type = ResolveRuntimeType();
        if (type == typeof(DateTimeOffset))
            await CommitJson(JsonSerializer.Serialize(new DateTimeOffset(combined, TimeSpan.Zero), EditorJsonOptions));
        else
            await CommitJson(JsonSerializer.Serialize(DateTime.SpecifyKind(combined, DateTimeKind.Utc), EditorJsonOptions));
    }

    private Task OnDateOnlyChanged(DateTime? date)
    {
        _dateValue = date;
        return date == null
            ? CommitUnsetOrNullJson()
            : CommitJson(JsonSerializer.Serialize(DateOnly.FromDateTime(date.Value), EditorJsonOptions));
    }

    private Task OnTimeOnlyChanged(TimeSpan? time)
    {
        _timeValue = time;
        return time == null
            ? CommitUnsetOrNullJson()
            : CommitJson(JsonSerializer.Serialize(TimeOnly.FromTimeSpan(time.Value), EditorJsonOptions));
    }

    private async Task OnNodeChanged(JsonNode? node)
    {
        _node = node;
        await CommitJson(node == null ? (AllowUnset ? null : "null") : node.ToJsonString(EditorJsonOptions));
    }

    private Task OnChipsChanged(IEnumerable<string> values)
    {
        _chips = values.ToList();
        return CommitJson(ParameterListJson.Serialize(_chips, LyoTypeUi.ToListKind(TypeName)) ?? (AllowUnset ? null : "[]"));
    }

    private async Task OnParseErrorChanged(string? error)
    {
        var hasError = !string.IsNullOrWhiteSpace(error);
        if (_hasParseError == hasError)
            return;

        _hasParseError = hasError;
        await ParseErrorChanged.InvokeAsync(hasError);
    }
}
