using System.Collections;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Lyo.Web.Components.JsonEditor;

public partial class JsonTreeView : IAsyncDisposable
{
    private const int TreeRowItemSize = 28;

    private readonly HashSet<string> _expanded = new(StringComparer.Ordinal) { "" };
    private string? _activeSearchPath;
    private int _activeSearchRowIndex = -1;
    private string? _copiedPath;
    private string? _editingKeyPath;
    private string? _editingPath;
    private string _editKey = "";
    private string _editValue = "";
    private JsonEditorJsInterop? _jsInterop;
    private int _lastNotifiedCurrent = -1;
    private int _lastNotifiedTotal = -1;
    private string _lastSearchText = "";
    private int _searchIndex = -1;
    private ElementReference _treeContainerRef;
    private List<TreeRow>? _visibleRowsCache;
    private JsonNode? _lastVisibleRowsRoot;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    [Parameter]
    public JsonNode? Root { get; set; }

    [Parameter]
    public Type? RootType { get; set; }

    [Parameter]
    public bool Editable { get; set; }

    [Parameter]
    public EventCallback<JsonNode?> RootChanged { get; set; }

    [Parameter]
    public string FontSize { get; set; } = "0.8125rem";

    [Parameter]
    public string SearchText { get; set; } = "";

    [Parameter]
    public EventCallback<(int Current, int Total)> SearchMatchChanged { get; set; }

    public async ValueTask DisposeAsync()
    {
        if (_jsInterop != null) {
            try {
                await _jsInterop.DisposeAsync();
            }
            catch (JSDisconnectedException) { }
        }
    }

    protected override void OnInitialized() => _jsInterop = new(JsRuntime);

    public void ExpandAll()
    {
        if (Root == null)
            return;

        _expanded.Clear();
        _expanded.Add("");
        AddExpandablePaths(Root, "");
        InvalidateVisibleRowsCache();
        InvokeAsync(StateHasChanged);
    }

    public void CollapseAll()
    {
        _expanded.Clear();
        _expanded.Add("");
        InvalidateVisibleRowsCache();
        InvokeAsync(StateHasChanged);
    }

    public void NextSearchMatch() => MoveSearchMatch(1);

    public void PreviousSearchMatch() => MoveSearchMatch(-1);

    public void FocusFirstSearchMatch() => RefreshSearchSelection(true);

    private Task ToggleExpandedAsync(string pathId)
    {
        if (_expanded.Contains(pathId))
            _expanded.Remove(pathId);
        else
            _expanded.Add(pathId);

        InvalidateVisibleRowsCache();
        return InvokeAsync(StateHasChanged);
    }

    private List<TreeRow> GetVisibleRows() => _visibleRowsCache ??= BuildVisibleRows();

    private void InvalidateVisibleRowsCache() => _visibleRowsCache = null;

    private List<TreeRow> BuildVisibleRows()
    {
        var list = new List<TreeRow>();
        if (Root == null)
            return list;

        AddRows(Root, "", "", 0, false, list);
        return list;
    }

    private void AddRows(JsonNode? node, string key, string pathId, int depth, bool canEditKey, List<TreeRow> list)
    {
        var isContainer = node is JsonObject or JsonArray;
        var expandable = node switch {
            JsonObject objectNode => objectNode.Count > 0,
            JsonArray arrayNode => arrayNode.Count > 0,
            var _ => false
        };

        var isExpanded = expandable && _expanded.Contains(pathId);
        var valueKind = GetValueKind(node);
        var display = GetRowDisplayValue(node, valueKind);
        var editValue = GetEditValue(node, valueKind);
        var resolvedType = ResolveTypeAtPath(pathId);
        var enumOptions = GetEnumOptions(pathId, key, valueKind, resolvedType);
        var canSetNull = IsNullableRow(valueKind, resolvedType);
        list.Add(
            new(
                pathId, Sanitize(key), Sanitize(display), editValue, GetValueCss(valueKind), depth, expandable, isExpanded, isContainer, canEditKey, enumOptions, valueKind,
                canSetNull));

        if (!expandable || !isExpanded || node == null)
            return;

        if (node is JsonObject obj) {
            var keys = obj.Select(x => x.Key).ToList();
            for (var i = 0; i < keys.Count; i++)
                AddRows(obj[keys[i]], keys[i], AppendPath(pathId, i), depth + 1, true, list);
        }
        else if (node is JsonArray arr) {
            for (var i = 0; i < arr.Count; i++)
                AddRows(arr[i], $"[{i}]", AppendPath(pathId, i), depth + 1, false, list);
        }
    }

    private static string AppendPath(string path, int childIndex) => string.IsNullOrEmpty(path) ? childIndex.ToString() : $"{path}.{childIndex}";

    private void StartEdit(string pathId, string rawValue)
    {
        if (!Editable)
            return;

        _editingKeyPath = null;
        _editingPath = pathId;
        _editValue = rawValue;
    }

    private void StartEditKey(string pathId, string key)
    {
        if (!Editable || string.IsNullOrEmpty(pathId))
            return;

        _editingPath = null;
        _editingKeyPath = pathId;
        _editKey = key;
    }

    private async Task CopyByPath(string pathId)
    {
        var node = GetNodeByPath(pathId);
        if (node == null) {
            if (_jsInterop != null)
                await _jsInterop.SendToClipboardAsync("null");

            await MarkCopied(pathId);
            return;
        }

        try {
            if (node is JsonValue strVal && strVal.GetValueKind() == JsonValueKind.String)
                await (_jsInterop?.SendToClipboardAsync(strVal.GetValue<string>() ?? "") ?? Task.CompletedTask);
            else
                await (_jsInterop?.SendToClipboardAsync(node.ToJsonString(new() { WriteIndented = true })) ?? Task.CompletedTask);

            await MarkCopied(pathId);
        }
        catch {
            if (_jsInterop != null)
                await _jsInterop.SendToClipboardAsync("?");

            await MarkCopied(pathId);
        }
    }

    private void OnInlineInputChanged(ChangeEventArgs e) => _editValue = e.Value?.ToString() ?? "";

    private async Task OnEnumChanged(ChangeEventArgs e, string pathId)
    {
        _editValue = e.Value?.ToString() ?? "";
        await CommitEdit(pathId);
    }

    private async Task OnBooleanChanged(ChangeEventArgs e, string pathId)
    {
        _editValue = e.Value is bool b && b ? "true" : "false";
        await CommitEdit(pathId);
    }

    private void OnKeyInputChanged(ChangeEventArgs e) => _editKey = e.Value?.ToString() ?? "";

    private async Task OnInlineInputKeyDown(KeyboardEventArgs e, string pathId)
    {
        switch (e.Key) {
            case "Enter":
                await CommitEdit(pathId);
                break;
            case "Escape":
                _editingPath = null;
                break;
        }
    }

    private async Task OnKeyInputKeyDown(KeyboardEventArgs e, string pathId)
    {
        switch (e.Key) {
            case "Enter":
                await CommitKeyEdit(pathId);
                break;
            case "Escape":
                _editingKeyPath = null;
                break;
        }
    }

    private EventCallback<KeyboardEventArgs> KeyDownKeyEdit(string pathId) => EventCallback.Factory.Create<KeyboardEventArgs>(this, async e => await OnKeyInputKeyDown(e, pathId));

    private EventCallback<KeyboardEventArgs> KeyDownInline(string pathId)
        => EventCallback.Factory.Create<KeyboardEventArgs>(this, async e => await OnInlineInputKeyDown(e, pathId));

    private EventCallback<ChangeEventArgs> EnumChange(string pathId) => EventCallback.Factory.Create<ChangeEventArgs>(this, async e => await OnEnumChanged(e, pathId));

    private EventCallback<ChangeEventArgs> BooleanChange(string pathId) => EventCallback.Factory.Create<ChangeEventArgs>(this, async e => await OnBooleanChanged(e, pathId));

    private async Task CommitEdit(string pathId)
    {
        if (_editingPath != pathId || Root == null)
            return;

        var nextNode = ParseInputNode(_editValue);
        if (string.IsNullOrEmpty(pathId))
            Root = nextNode;
        else
            SetNodeByPath(pathId, nextNode);

        _editingPath = null;
        InvalidateVisibleRowsCache();
        await RootChanged.InvokeAsync(Root);
    }

    private async Task CommitKeyEdit(string pathId)
    {
        if (_editingKeyPath != pathId || Root == null)
            return;

        if (!TryGetParentAndIndex(pathId, out var parent, out var index) || parent is not JsonObject objParent) {
            _editingKeyPath = null;
            return;
        }

        var keys = objParent.Select(x => x.Key).ToList();
        if (index < 0 || index >= keys.Count) {
            _editingKeyPath = null;
            return;
        }

        var oldKey = keys[index];
        var requested = (_editKey ?? "").Trim();
        if (string.IsNullOrEmpty(requested)) {
            _editingKeyPath = null;
            return;
        }

        var newKey = EnsureUniqueObjectKey(objParent, requested, oldKey);
        if (!string.Equals(oldKey, newKey, StringComparison.Ordinal)) {
            var ordered = objParent.Select(kvp => new KeyValuePair<string, JsonNode?>(kvp.Key, kvp.Value)).ToList();
            var existingValue = ordered[index].Value;
            ordered[index] = new(newKey, existingValue);
            objParent.Clear();
            foreach (var kvp in ordered)
                objParent[kvp.Key] = kvp.Value;
        }

        _editingKeyPath = null;
        InvalidateVisibleRowsCache();
        await RootChanged.InvokeAsync(Root);
    }

    private async Task AddChild(string pathId)
    {
        var container = GetNodeByPath(pathId);
        if (container is JsonArray arr) {
            arr.Add(InferArrayElementDefault(pathId, arr));
            _expanded.Add(pathId);
            InvalidateVisibleRowsCache();
            await RootChanged.InvokeAsync(Root);
            return;
        }

        if (container is JsonObject obj) {
            var newKey = EnsureUniqueObjectKey(obj, "newField");
            obj[newKey] = null;
            _expanded.Add(pathId);
            InvalidateVisibleRowsCache();
            await RootChanged.InvokeAsync(Root);
        }
    }

    private async Task RemoveByPath(string pathId)
    {
        if (Root == null || string.IsNullOrEmpty(pathId))
            return;

        if (!TryGetParentAndIndex(pathId, out var parent, out var index))
            return;

        if (parent is JsonArray arr) {
            if (index >= 0 && index < arr.Count)
                arr.RemoveAt(index);
        }
        else if (parent is JsonObject obj) {
            var keys = obj.Select(x => x.Key).ToList();
            if (index >= 0 && index < keys.Count)
                obj.Remove(keys[index]);
        }

        _editingPath = null;
        _editingKeyPath = null;
        InvalidateVisibleRowsCache();
        await RootChanged.InvokeAsync(Root);
    }

    private bool TryGetParentAndIndex(string pathId, out JsonNode? parent, out int index)
    {
        parent = null;
        index = -1;
        if (string.IsNullOrEmpty(pathId) || Root == null)
            return false;

        var parts = pathId.Split('.');
        if (!int.TryParse(parts[^1], out index))
            return false;

        if (parts.Length == 1) {
            parent = Root;
            return true;
        }

        var parentPath = string.Join(".", parts.Take(parts.Length - 1));
        parent = GetNodeByPath(parentPath);
        return parent != null;
    }

    private static string EnsureUniqueObjectKey(JsonObject obj, string requested, string? excluding = null)
    {
        var baseKey = string.IsNullOrWhiteSpace(requested) ? "newField" : requested.Trim();
        var candidate = baseKey;
        var suffix = 1;
        while (obj.ContainsKey(candidate) && !string.Equals(candidate, excluding, StringComparison.Ordinal)) {
            candidate = $"{baseKey}_{suffix}";
            suffix++;
        }

        return candidate;
    }

    private async Task MarkCopied(string pathId)
    {
        _copiedPath = pathId;
        await InvokeAsync(StateHasChanged);
        await Task.Delay(1200);
        if (_copiedPath == pathId) {
            _copiedPath = null;
            await InvokeAsync(StateHasChanged);
        }
    }

    private void MoveSearchMatch(int delta)
    {
        var search = SearchText?.Trim() ?? "";
        if (string.IsNullOrEmpty(search)) {
            _activeSearchPath = null;
            _searchIndex = -1;
            _activeSearchRowIndex = -1;
            NotifySearchMatchChanged(0, 0);
            InvokeAsync(StateHasChanged);
            return;
        }

        var rows = GetVisibleRows();
        var matches = rows.Where(r => IsSearchMatch(r, search)).ToList();
        if (matches.Count == 0) {
            _activeSearchPath = null;
            _searchIndex = -1;
            _activeSearchRowIndex = -1;
            NotifySearchMatchChanged(0, 0);
            InvokeAsync(StateHasChanged);
            return;
        }

        if (!string.Equals(_lastSearchText, search, StringComparison.Ordinal)) {
            _searchIndex = delta >= 0 ? 0 : matches.Count - 1;
            _lastSearchText = search;
        }
        else {
            _searchIndex = (_searchIndex + delta + matches.Count) % matches.Count;
            if (_searchIndex < 0)
                _searchIndex = 0;
        }

        var selected = matches[_searchIndex];
        _activeSearchPath = selected.PathId;
        _activeSearchRowIndex = rows.FindIndex(r => r.PathId == selected.PathId);
        NotifySearchMatchChanged(_searchIndex + 1, matches.Count);
        _ = ScrollActiveMatchIntoViewAsync();
        InvokeAsync(StateHasChanged);
    }

    private static bool IsSearchMatch(TreeRow row, string search)
        => (!string.IsNullOrEmpty(row.Key) && row.Key.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(row.DisplayValue) && row.DisplayValue.Contains(search, StringComparison.OrdinalIgnoreCase));

    private void AddExpandablePaths(JsonNode? node, string pathId)
    {
        if (node is JsonObject obj) {
            if (obj.Count > 0)
                _expanded.Add(pathId);

            var keys = obj.Select(x => x.Key).ToList();
            for (var i = 0; i < keys.Count; i++) {
                var childPath = AppendPath(pathId, i);
                AddExpandablePaths(obj[keys[i]], childPath);
            }

            return;
        }

        if (node is JsonArray arr) {
            if (arr.Count > 0)
                _expanded.Add(pathId);

            for (var i = 0; i < arr.Count; i++) {
                var childPath = AppendPath(pathId, i);
                AddExpandablePaths(arr[i], childPath);
            }
        }
    }

    protected override void OnParametersSet()
    {
        if (!ReferenceEquals(Root, _lastVisibleRowsRoot)) {
            _lastVisibleRowsRoot = Root;
            InvalidateVisibleRowsCache();
        }

        var search = SearchText?.Trim() ?? "";
        if (string.IsNullOrEmpty(search)) {
            _activeSearchPath = null;
            _searchIndex = -1;
            _activeSearchRowIndex = -1;
            _lastSearchText = "";
            NotifySearchMatchChanged(0, 0);
            return;
        }

        if (string.Equals(_lastSearchText, search, StringComparison.Ordinal))
            return;

        RefreshSearchSelection(true, false);
    }

    private void RefreshSearchSelection(bool resetToFirst, bool triggerRender = true)
    {
        var search = SearchText?.Trim() ?? "";
        _lastSearchText = search;
        if (string.IsNullOrEmpty(search)) {
            _activeSearchPath = null;
            _searchIndex = -1;
            _activeSearchRowIndex = -1;
            NotifySearchMatchChanged(0, 0);
            if (triggerRender)
                InvokeAsync(StateHasChanged);

            return;
        }

        var rows = GetVisibleRows();
        var matches = rows.Where(r => IsSearchMatch(r, search)).ToList();
        if (matches.Count == 0) {
            _activeSearchPath = null;
            _searchIndex = -1;
            _activeSearchRowIndex = -1;
            NotifySearchMatchChanged(0, 0);
            if (triggerRender)
                InvokeAsync(StateHasChanged);

            return;
        }

        if (resetToFirst || _searchIndex < 0 || _searchIndex >= matches.Count)
            _searchIndex = 0;

        var selected = matches[_searchIndex];
        _activeSearchPath = selected.PathId;
        _activeSearchRowIndex = rows.FindIndex(r => r.PathId == selected.PathId);
        NotifySearchMatchChanged(_searchIndex + 1, matches.Count);
        _ = ScrollActiveMatchIntoViewAsync();
        if (triggerRender)
            InvokeAsync(StateHasChanged);
    }

    private void NotifySearchMatchChanged(int current, int total)
    {
        if (_lastNotifiedCurrent == current && _lastNotifiedTotal == total)
            return;

        _lastNotifiedCurrent = current;
        _lastNotifiedTotal = total;
        if (SearchMatchChanged.HasDelegate)
            _ = SearchMatchChanged.InvokeAsync((current, total));
    }

    private async Task ScrollActiveMatchIntoViewAsync()
    {
        if (_activeSearchRowIndex < 0 || string.IsNullOrEmpty(_activeSearchPath))
            return;

        try {
            if (_jsInterop != null)
                await _jsInterop.ScrollVirtualRowIntoViewAsync(_treeContainerRef, _activeSearchRowIndex, TreeRowItemSize, _activeSearchPath);
        }
        catch {
            // Best effort only; keep navigation functional even if JS isn't available.
        }
    }

    private JsonNode? InferArrayElementDefault(string pathId, JsonArray targetArray)
    {
        // If existing elements are present, infer from the first non-null element shape/type.
        var exemplar = targetArray.FirstOrDefault(x => x != null);
        if (exemplar is not null)
            return CreateDefaultFromExemplar(exemplar);

        // If empty, infer from the CLR model type passed from JsonEditor<T>.
        var arrayType = ResolveTypeAtPath(pathId);
        var elementType = GetCollectionElementType(arrayType);
        if (elementType is null)
            return null;

        return CreateDefaultFromType(elementType);
    }

    private bool IsNullableRow(JsonValueKind? valueKind, Type? resolvedType)
    {
        if (valueKind == JsonValueKind.Null)
            return true;

        if (resolvedType == null)
            return true;

        if (!resolvedType.IsValueType)
            return true;

        return Nullable.GetUnderlyingType(resolvedType) != null;
    }

    private Type? ResolveTypeAtPath(string pathId)
    {
        if (RootType is null)
            return null;

        if (string.IsNullOrEmpty(pathId))
            return RootType;

        var currentType = RootType;
        var currentNode = Root;
        var parts = pathId.Split('.', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts) {
            if (!int.TryParse(part, out var index))
                return null;

            if (currentNode is JsonObject objNode) {
                var keys = objNode.Select(x => x.Key).ToList();
                if (index < 0 || index >= keys.Count)
                    return null;

                var key = keys[index];
                currentType = GetPropertyTypeByJsonName(currentType, key);
                currentNode = objNode[key];
                continue;
            }

            if (currentNode is JsonArray arrNode) {
                currentType = GetCollectionElementType(currentType);
                if (index < 0 || index >= arrNode.Count) {
                    currentNode = null;
                    continue;
                }

                currentNode = arrNode[index];
                continue;
            }

            return null;
        }

        return currentType;
    }

    private static Type? GetPropertyTypeByJsonName(Type? ownerType, string jsonName)
    {
        if (ownerType == null)
            return null;

        var type = Nullable.GetUnderlyingType(ownerType) ?? ownerType;
        if (type.IsArray || IsEnumerableButNotString(type))
            return null;

        var prop = type.GetProperties().FirstOrDefault(p => string.Equals(GetJsonPropertyName(p), jsonName, StringComparison.OrdinalIgnoreCase));
        return prop?.PropertyType;
    }

    private static string GetJsonPropertyName(PropertyInfo prop)
    {
        var attr = prop.GetCustomAttributes(typeof(JsonPropertyNameAttribute), true).Cast<JsonPropertyNameAttribute>().FirstOrDefault();
        return attr?.Name ?? prop.Name;
    }

    private static Type? GetCollectionElementType(Type? type)
    {
        if (type == null)
            return null;

        var t = Nullable.GetUnderlyingType(type) ?? type;
        if (t == typeof(string))
            return null;

        if (t.IsArray)
            return t.GetElementType();

        if (t.IsGenericType && t.GetGenericArguments().Length == 1 && typeof(IEnumerable).IsAssignableFrom(t))
            return t.GetGenericArguments()[0];

        var ienum = t.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        return ienum?.GetGenericArguments()[0];
    }

    private static bool IsEnumerableButNotString(Type t) => t != typeof(string) && typeof(IEnumerable).IsAssignableFrom(t);

    private static JsonNode? CreateDefaultFromType(Type type) => CreateDefaultFromType(type, new());

    private static JsonNode? CreateDefaultFromType(Type type, HashSet<Type> visiting)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        if (!t.IsValueType && visiting.Contains(t))
            return null;

        if (t == typeof(string))
            return JsonValue.Create("");

        if (t == typeof(bool))
            return JsonValue.Create(false);

        if (t == typeof(byte) || t == typeof(sbyte) || t == typeof(short) || t == typeof(ushort) || t == typeof(int) || t == typeof(uint) || t == typeof(long) ||
            t == typeof(ulong))
            return JsonValue.Create(0);

        if (t == typeof(float) || t == typeof(double) || t == typeof(decimal))
            return JsonValue.Create(0);

        if (t == typeof(DateTime) || t == typeof(DateTimeOffset))
            return JsonValue.Create(DateTime.UtcNow.ToString("O"));

        if (t == typeof(Guid))
            return JsonValue.Create(Guid.Empty.ToString());

        if (t.IsEnum)
            return JsonValue.Create(Enum.GetNames(t).FirstOrDefault() ?? "0");

        if (t.IsArray || IsEnumerableButNotString(t))
            return new JsonArray();

        if (!t.IsClass && !t.IsValueType)
            return null;

        visiting.Add(t);
        try {
            var obj = new JsonObject();
            foreach (var prop in t.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
                if (!prop.CanRead)
                    continue;

                var propName = GetJsonPropertyName(prop);
                obj[propName] = CreateDefaultFromType(prop.PropertyType, visiting);
            }

            return obj;
        }
        finally {
            visiting.Remove(t);
        }
    }

    private string[] GetEnumOptions(string pathId, string key, JsonValueKind? valueKind, Type? resolvedType)
    {
        if (valueKind != JsonValueKind.String)
            return [];

        var t = Nullable.GetUnderlyingType(resolvedType ?? typeof(string)) ?? resolvedType;
        if (t != null && t.IsEnum)
            return Enum.GetNames(t);

        if (string.Equals(key, "$type", StringComparison.Ordinal) && TryResolveParentType(pathId, out var parentType)) {
            var opts = parentType.GetCustomAttributes(typeof(JsonDerivedTypeAttribute), true)
                .Cast<JsonDerivedTypeAttribute>()
                .Select(a => a.TypeDiscriminator?.ToString())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Cast<string>()
                .ToArray();

            if (opts.Length > 0)
                return opts;
        }

        return [];
    }

    private bool TryResolveParentType(string pathId, out Type parentType)
    {
        parentType = typeof(object);
        if (string.IsNullOrEmpty(pathId))
            return false;

        var lastDot = pathId.LastIndexOf('.');
        var parentPath = lastDot >= 0 ? pathId[..lastDot] : "";
        var resolved = ResolveTypeAtPath(parentPath);
        if (resolved == null)
            return false;

        parentType = Nullable.GetUnderlyingType(resolved) ?? resolved;
        return true;
    }

    private static JsonNode? CreateDefaultFromExemplar(JsonNode exemplar)
    {
        if (exemplar is JsonArray arr) {
            // Preserve "array-ness" for nested collections (e.g. Keys item is object[]).
            if (arr.Count == 0)
                return new JsonArray();

            var first = arr.FirstOrDefault(x => x != null);
            return first is null ? new() : new JsonArray(CreateDefaultFromExemplar(first));
        }

        if (exemplar is JsonObject obj) {
            var clone = new JsonObject();
            foreach (var (key, value) in obj)
                clone[key] = value is null ? null : CreateDefaultFromExemplar(value);

            return clone;
        }

        if (exemplar is not JsonValue jv)
            return null;

        try {
            var kind = jv.GetValueKind();
            return kind switch {
                JsonValueKind.String => JsonValue.Create(""),
                JsonValueKind.Number => JsonValue.Create(0),
                JsonValueKind.True or JsonValueKind.False => JsonValue.Create(false),
                JsonValueKind.Null => null,
                var _ => null
            };
        }
        catch {
            return null;
        }
    }

    private void SetNodeByPath(string pathId, JsonNode? value)
    {
        if (Root == null)
            return;

        var parts = pathId.Split('.');
        var current = Root;
        for (var i = 0; i < parts.Length - 1; i++) {
            if (!int.TryParse(parts[i], out var index) || current == null)
                return;

            if (current is JsonObject obj) {
                var keys = obj.Select(x => x.Key).ToList();
                if (index < 0 || index >= keys.Count)
                    return;

                current = obj[keys[index]];
            }
            else if (current is JsonArray arr) {
                if (index < 0 || index >= arr.Count)
                    return;

                current = arr[index];
            }
            else
                return;
        }

        if (!int.TryParse(parts[^1], out var lastIndex) || current == null)
            return;

        if (current is JsonObject objParent) {
            var keys = objParent.Select(x => x.Key).ToList();
            if (lastIndex < 0 || lastIndex >= keys.Count)
                return;

            objParent[keys[lastIndex]] = value;
        }
        else if (current is JsonArray arrParent) {
            if (lastIndex < 0 || lastIndex >= arrParent.Count)
                return;

            arrParent[lastIndex] = value;
        }
    }

    private static JsonNode? ParseInputNode(string input)
    {
        var trimmed = input?.Trim() ?? "";
        if (string.Equals(trimmed, "null", StringComparison.OrdinalIgnoreCase))
            return null;

        try {
            return JsonNode.Parse(trimmed);
        }
        catch {
            return JsonValue.Create(input ?? "");
        }
    }

    private async Task SetNull(string pathId)
    {
        if (Root == null)
            return;

        if (string.IsNullOrEmpty(pathId))
            Root = null;
        else
            SetNodeByPath(pathId, null);

        _editingPath = null;
        InvalidateVisibleRowsCache();
        await RootChanged.InvokeAsync(Root);
    }

    private JsonNode? GetNodeByPath(string pathId)
    {
        if (Root == null || string.IsNullOrEmpty(pathId))
            return Root;

        var parts = pathId.Split('.');
        var current = Root;
        foreach (var part in parts) {
            if (current == null || !int.TryParse(part, out var index))
                return null;

            if (current is JsonObject obj) {
                var keys = obj.Select(x => x.Key).ToList();
                if (index < 0 || index >= keys.Count)
                    return null;

                current = obj[keys[index]];
            }
            else if (current is JsonArray arr) {
                if (index < 0 || index >= arr.Count)
                    return null;

                current = arr[index];
            }
            else
                return null;
        }

        return current;
    }

    private static JsonValueKind? GetValueKind(JsonNode? node)
    {
        if (node is not JsonValue value)
            return null;

        try {
            return value.GetValueKind();
        }
        catch {
            return null;
        }
    }

    private static string GetRowDisplayValue(JsonNode? node, JsonValueKind? kind)
    {
        if (node is JsonObject obj)
            return $"{{ {obj.Count} {(obj.Count == 1 ? "property" : "properties")} }}";

        if (node is JsonArray arr)
            return $"[ {arr.Count} {(arr.Count == 1 ? "item" : "items")} ]";

        if (node == null)
            return "null";

        try {
            if (kind == JsonValueKind.String && node is JsonValue strVal)
                return "\"" + (strVal.GetValue<string>() ?? "") + "\"";

            if (kind == JsonValueKind.Null)
                return "null";

            return node.ToJsonString();
        }
        catch {
            return "?";
        }
    }

    private static string GetEditValue(JsonNode? node, JsonValueKind? kind)
    {
        if (node == null)
            return "null";

        try {
            if (kind == JsonValueKind.String && node is JsonValue strVal)
                return strVal.GetValue<string>() ?? "";

            return node.ToJsonString();
        }
        catch {
            return "";
        }
    }

    private static string GetValueCss(JsonValueKind? kind)
        => kind switch {
            JsonValueKind.String => "tree-string",
            JsonValueKind.Number => "tree-number",
            JsonValueKind.True or JsonValueKind.False => "tree-bool",
            JsonValueKind.Null => "tree-null",
            var _ => string.Empty
        };

    private static string Sanitize(string s)
    {
        if (string.IsNullOrEmpty(s))
            return "";

        var sb = new StringBuilder(s.Length);
        foreach (var c in s) {
            if (char.IsControl(c) && c is not ('\t' or '\n' or '\r'))
                sb.Append('?');
            else
                sb.Append(c);
        }

        return sb.ToString();
    }

    private record TreeRow(
        string PathId,
        string Key,
        string DisplayValue,
        string EditValue,
        string ValueCss,
        int Depth,
        bool Expandable,
        bool IsExpanded,
        bool IsContainer,
        bool CanEditKey,
        string[] EnumOptions,
        JsonValueKind? ValueKind,
        bool CanSetNull);
}