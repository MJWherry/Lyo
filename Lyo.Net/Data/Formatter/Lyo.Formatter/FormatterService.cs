using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Lyo.Exceptions;
using SmartFormat;
using SmartFormat.Core.Settings;

namespace Lyo.Formatter;

/// <summary>Default <see cref="IFormatterService" />: SmartFormat named placeholders plus C#-like expressions (DateTime, ternary, in-memory LINQ).</summary>
public sealed class FormatterService : IFormatterService
{
    private readonly FormatterExpressionEngine _expressions;

    /// <summary>
    /// Builds a <see cref="FormatterService" /> with the default SmartFormat setup. FormatErrorAction is MaintainTokens so missing placeholders stay in the output (so
    /// GetUnresolvedPlaceholders can see them) instead of throwing.
    /// </summary>
    public FormatterService()
        : this(CreateDefaultFormatter(), static () => DateTimeOffset.Now) { }

    /// <summary>Builds a <see cref="FormatterService" /> around a caller-supplied SmartFormatter.</summary>
    /// <param name="formatter">SmartFormatter to use. Must not be null.</param>
    public FormatterService(SmartFormatter formatter)
        : this(formatter, static () => DateTimeOffset.Now) { }

    /// <summary>Builds a service with a frozen or injectable clock for <c>DateTime.Now</c> / <c>UtcNow</c> in expressions.</summary>
    /// <param name="clock">Returns the current instant. Must not be null.</param>
    public FormatterService(Func<DateTimeOffset> clock)
        : this(CreateDefaultFormatter(), clock) { }

    /// <summary>Builds a service with a caller-supplied SmartFormatter and clock.</summary>
    /// <param name="formatter">SmartFormatter to use. Must not be null.</param>
    /// <param name="clock">Returns the current instant. Must not be null.</param>
    public FormatterService(SmartFormatter formatter, Func<DateTimeOffset> clock)
    {
        ArgumentHelpers.ThrowIfNull(formatter);
        ArgumentHelpers.ThrowIfNull(clock);
        Formatter = formatter;
        _expressions = new(clock);
    }

    /// <inheritdoc />
    public SmartFormatter Formatter { get; }

    /// <inheritdoc />
    public CultureInfo? Culture { get; set; }

    /// <inheritdoc />
    public string Format(string template, object? context)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(template);
        return FormatCore(template, context, extra: null);
    }

    /// <inheritdoc />
    public string Format(string template, params object?[] contextItems)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(template);
        ArgumentHelpers.ThrowIfNull(contextItems);
        if (contextItems.Length == 0)
            return FormatCore(template, null, extra: null);
        if (contextItems.Length == 1)
            return FormatCore(template, contextItems[0], extra: null);

        var bag = new FormatterContextBag();
        foreach (var item in contextItems)
            bag.AddContext(item);
        return FormatCore(template, bag, extra: contextItems);
    }

    /// <inheritdoc />
    public string Format(string template, IReadOnlyDictionary<string, object?> context)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(template);
        ArgumentHelpers.ThrowIfNull(context);
        return FormatCore(template, context, extra: null);
    }

    /// <inheritdoc />
    public string Format(string template, Action<IContextBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(template);
        ArgumentHelpers.ThrowIfNull(configure);
        var context = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var contextBuilder = new ContextBuilder(context, Culture ?? CultureInfo.CurrentCulture);
        configure(contextBuilder);
        return Format(template, context);
    }

    /// <inheritdoc />
    public bool TryFormat(string template, object? context, out string? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(template))
            return false;

        try {
            result = FormatCore(template, context, extra: null);
            return true;
        }
        catch {
            return false;
        }
    }

    /// <inheritdoc />
    public bool ValidateTemplate(string template) => TryValidateTemplate(template, out var _);

    /// <inheritdoc />
    public bool TryValidateTemplate(string template, out string? errorMessage)
    {
        errorMessage = null;
        if (string.IsNullOrWhiteSpace(template)) {
            errorMessage = "Template is null or empty.";
            return false;
        }

        var tokens = FormatterTemplateScanner.Scan(template);
        if (IsUnmatchedBrace(template, tokens)) {
            errorMessage = "Template has unmatched '{' or '}'.";
            return false;
        }

        foreach (var token in tokens) {
            if (token.Kind != FormatterTokenKind.Expression)
                continue;
            var exprError = _expressions.TryValidate(token.Inner);
            if (exprError is null)
                continue;
            errorMessage = exprError;
            return false;
        }

        if (FormatterTemplateScanner.HasExpression(tokens))
            return true;

        try {
            _ = Formatter.Parser.ParseFormat(template);
            return true;
        }
        catch (Exception ex) {
            errorMessage = ex.Message;
            return false;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetPlaceholders(string template)
    {
        if (string.IsNullOrWhiteSpace(template))
            return [];

        var tokens = FormatterTemplateScanner.Scan(template);
        var placeholders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var token in tokens) {
            if (token.Kind == FormatterTokenKind.Literal)
                continue;
            if (token.Kind == FormatterTokenKind.SmartFormat) {
                var key = SmartFormatPlaceholderKey(token.Inner);
                if (key.Length > 0)
                    placeholders.Add(key);
                continue;
            }

            foreach (var id in _expressions.GetIdentifiers(token.Inner))
                placeholders.Add(id);
        }

        return placeholders.ToList();
    }

    /// <inheritdoc />
    public bool AllPlaceholdersResolved(string template, string formattedOutput) => GetUnresolvedPlaceholders(template, formattedOutput).Count == 0;

    /// <inheritdoc />
    public IReadOnlyList<string> GetUnresolvedPlaceholders(string template, string formattedOutput)
    {
        if (string.IsNullOrEmpty(formattedOutput))
            return [];

        var unresolved = new List<string>();
        foreach (var token in FormatterTemplateScanner.Scan(template)) {
            if (token.Kind == FormatterTokenKind.Literal)
                continue;
            if (formattedOutput.IndexOf(token.Raw, StringComparison.Ordinal) < 0)
                continue;
            var key = token.Kind == FormatterTokenKind.Expression ? token.Inner.Trim() : SmartFormatPlaceholderKey(token.Inner);
            if (key.Length == 0)
                key = token.Raw;
            unresolved.Add(key);
        }

        return unresolved;
    }

    /// <inheritdoc />
    public ITemplate CreateTemplate(string template)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(template);
        return new Template(this, template);
    }

    /// <inheritdoc />
    public IReadOnlyList<FormatterSegment> FormatSegments(string template, object? context)
    {
        if (string.IsNullOrEmpty(template))
            return [];

        var tokens = FormatterTemplateScanner.Scan(template);
        if (IsUnmatchedBrace(template, tokens))
            return [new(FormatterSegmentKind.Literal, template, null, template)];

        var segments = new List<FormatterSegment>(tokens.Count);
        foreach (var token in tokens) {
            if (token.Kind == FormatterTokenKind.Literal) {
                var text = UnescapeLiteral(token.Raw);
                if (text.Length == 0)
                    continue;
                segments.Add(new(FormatterSegmentKind.Literal, text, null, token.Raw));
                continue;
            }

            string formatted;
            try {
                formatted = FormatToken(token, context);
            }
            catch {
                formatted = token.Raw;
            }

            var key = token.Kind == FormatterTokenKind.Expression ? token.Inner.Trim() : SmartFormatPlaceholderKey(token.Inner);
            var unresolved = formatted == token.Raw || IsUnresolvedPlaceholder(formatted, key);
            segments.Add(new(unresolved ? FormatterSegmentKind.Unresolved : FormatterSegmentKind.Placeholder, formatted, key, token.Raw));
        }

        return segments;
    }

    /// <inheritdoc />
    public IReadOnlyList<FormatterSegment> FormatSegments(string template, IReadOnlyDictionary<string, object?> context)
    {
        ArgumentHelpers.ThrowIfNull(context);
        return FormatSegments(template, (object?)context);
    }

    private string FormatCore(string template, object? context, object?[]? extra)
    {
        var provider = Culture ?? CultureInfo.CurrentCulture;
        var tokens = FormatterTemplateScanner.Scan(template);
        if (IsUnmatchedBrace(template, tokens))
            return Formatter.Format(provider, template, extra ?? (context is null ? [] : [context]));

        var builder = new StringBuilder(template.Length);
        foreach (var token in tokens) {
            if (token.Kind == FormatterTokenKind.Literal) {
                builder.Append(UnescapeLiteral(token.Raw));
                continue;
            }

            builder.Append(FormatToken(token, context, extra));
        }

        return builder.ToString();
    }

    private string FormatToken(FormatterToken token, object? context, object?[]? extra = null)
    {
        if (string.IsNullOrWhiteSpace(token.Inner))
            return token.Raw;
        if (token.Kind == FormatterTokenKind.SmartFormat) {
            var formatted = FormatSmart(Culture ?? CultureInfo.CurrentCulture, token.Raw, context, extra);
            var key = SmartFormatPlaceholderKey(token.Inner);
            if (formatted != token.Raw && !IsUnresolvedPlaceholder(formatted, key))
                return formatted;

            var bag = context as FormatterContextBag ?? new FormatterContextBag(context);
            if (!bag.TryResolve(key, out var resolved) || resolved is null)
                return formatted;
            return FormatExpressionValue(resolved, SmartFormatFormatSpec(token.Inner));
        }

        var exprBag = context as FormatterContextBag ?? new FormatterContextBag(context);
        if (!_expressions.TryEvaluate(token.Inner, exprBag, out var value, out _))
            return token.Raw;

        return FormatExpressionValue(value, token.FormatSpec);
    }

    private string FormatSmart(IFormatProvider provider, string template, object? context, object?[]? extra)
    {
        if (extra is { Length: > 0 })
            return Formatter.Format(provider, template, extra);
        return context is null ? Formatter.Format(provider, template) : Formatter.Format(provider, template, context);
    }

    private string FormatExpressionValue(object? value, string? spec)
    {
        if (value is null)
            return string.Empty;

        if (!string.IsNullOrEmpty(spec)) {
            const string syntheticKey = "__p";
            var mini = "{" + syntheticKey + ":" + spec + "}";
            var ctx = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { [syntheticKey] = value };
            return Formatter.Format(Culture ?? CultureInfo.CurrentCulture, mini, ctx);
        }

        if (value is string text)
            return text;
        if (value is IFormattable formattable)
            return formattable.ToString(null, Culture ?? CultureInfo.CurrentCulture) ?? string.Empty;
        if (value is System.Collections.IEnumerable enumerable and not string) {
            var parts = new List<string>();
            foreach (var item in enumerable)
                parts.Add(item is null ? string.Empty : Convert.ToString(item, Culture ?? CultureInfo.CurrentCulture) ?? string.Empty);
            return string.Join(", ", parts);
        }

        return Convert.ToString(value, Culture ?? CultureInfo.CurrentCulture) ?? string.Empty;
    }

    private static SmartFormatter CreateDefaultFormatter()
    {
        var settings = new SmartSettings();
        settings.Formatter.ErrorAction = FormatErrorAction.MaintainTokens;
        settings.CaseSensitivity = CaseSensitivityType.CaseInsensitive;
        return Smart.CreateDefaultSmartFormat(settings);
    }

    private static bool IsUnmatchedBrace(string template, IReadOnlyList<FormatterToken> tokens)
        => tokens.Count == 1
           && tokens[0].Kind == FormatterTokenKind.Literal
           && tokens[0].Raw == template
           && template.IndexOf('{') >= 0;

    private static string SmartFormatPlaceholderKey(string inner)
    {
        var spec = inner.IndexOfAny([':', ',', '(', '|']);
        var key = spec >= 0 ? inner[..spec] : inner;
        return key.Trim();
    }

    /// <summary>Standard .NET format specifier after the first colon; null for list/plural formatters.</summary>
    private static string? SmartFormatFormatSpec(string inner)
    {
        var colon = inner.IndexOf(':');
        if (colon < 0)
            return null;
        var spec = inner[(colon + 1)..].Trim();
        if (spec.Length == 0 || spec.IndexOfAny(['{', '|']) >= 0)
            return null;
        if (spec.StartsWith("list", StringComparison.OrdinalIgnoreCase) || spec.StartsWith("plural", StringComparison.OrdinalIgnoreCase))
            return null;
        return spec;
    }

    private static string UnescapeLiteral(string text) => text.Replace("{{", "{").Replace("}}", "}");

    private static bool IsUnresolvedPlaceholder(string output, string placeholder)
    {
        if (string.IsNullOrEmpty(placeholder))
            return output.Contains('{');
        var escaped = Regex.Escape(placeholder);
        return Regex.IsMatch(output, @"\{" + escaped + @"(?:\:[^{}]*)?\}");
    }

    private sealed class Template : ITemplate
    {
        private readonly Dictionary<string, object?> _mergedContext = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<object?> _otherContextItems = [];
        private readonly FormatterService _service;

        public Template(FormatterService service, string template)
        {
            _service = service;
            TemplateString = template;
        }

        public string TemplateString { get; }

        public bool Validate() => _service.ValidateTemplate(TemplateString);

        public bool TryValidate(out string? errorMessage) => _service.TryValidateTemplate(TemplateString, out errorMessage);

        public IReadOnlyList<string> GetPlaceholders() => _service.GetPlaceholders(TemplateString);

        public bool TryValidateContext(out string? errorMessage)
        {
            var placeholders = _service.GetPlaceholders(TemplateString);
            if (placeholders.Count == 0) {
                errorMessage = null;
                return true;
            }

            var contextKeys = new HashSet<string>(_mergedContext.Keys, StringComparer.OrdinalIgnoreCase);
            foreach (var obj in _otherContextItems) {
                if (obj is IReadOnlyDictionary<string, object?> dict) {
                    foreach (var k in dict.Keys)
                        contextKeys.Add(k);
                }
            }

            var missing = new List<string>();
            foreach (var p in placeholders) {
                var satisfied = false;
                foreach (var key in contextKeys) {
                    if (string.Equals(p, key, StringComparison.OrdinalIgnoreCase) || p.StartsWith(key + ".", StringComparison.OrdinalIgnoreCase)) {
                        satisfied = true;
                        break;
                    }
                }

                if (!satisfied)
                    missing.Add(p);
            }

            if (missing.Count == 0) {
                errorMessage = null;
                return true;
            }

            errorMessage = $"Missing context for placeholders: {string.Join(", ", missing)}";
            return false;
        }

        public ITemplate AddContext(Action<IContextBuilder> configure)
        {
            ArgumentHelpers.ThrowIfNull(configure);
            var contextBuilder = new ContextBuilder(_mergedContext, _service.Culture ?? CultureInfo.CurrentCulture);
            configure(contextBuilder);
            return this;
        }

        public ITemplate WithContext(object? context)
        {
            if (context is IReadOnlyDictionary<string, object?> dict) {
                foreach (var kv in dict)
                    _mergedContext[kv.Key] = kv.Value;
            }
            else
                _otherContextItems.Add(context);

            return this;
        }

        public ITemplate WithContext(IReadOnlyDictionary<string, object?> context)
        {
            foreach (var kv in context)
                _mergedContext[kv.Key] = kv.Value;

            return this;
        }

        public ITemplate WithValue(string name, object? value)
        {
            _mergedContext[name] = value;
            return this;
        }

        public string Format()
        {
            if (_mergedContext.Count == 0 && _otherContextItems.Count == 0)
                return _service.Format(TemplateString, (object?)null!);

            if (_otherContextItems.Count == 0)
                return _service.Format(TemplateString, _mergedContext);

            var args = new List<object?>(1 + _otherContextItems.Count);
            if (_mergedContext.Count > 0)
                args.Add(_mergedContext);

            args.AddRange(_otherContextItems);
            return _service.Format(TemplateString, args.ToArray());
        }

        public string Format(object? additionalContext)
        {
            if (additionalContext is IReadOnlyDictionary<string, object?> dict) {
                var combined = new Dictionary<string, object?>(_mergedContext, StringComparer.OrdinalIgnoreCase);
                foreach (var kv in dict)
                    combined[kv.Key] = kv.Value;

                return _otherContextItems.Count == 0
                    ? _service.Format(TemplateString, combined)
                    : _service.Format(TemplateString, new object?[] { combined }.Concat(_otherContextItems).ToArray());
            }

            var allArgs = new List<object?>(1 + _otherContextItems.Count + 1);
            if (_mergedContext.Count > 0)
                allArgs.Add(_mergedContext);

            allArgs.AddRange(_otherContextItems);
            allArgs.Add(additionalContext);
            return _service.Format(TemplateString, allArgs.ToArray());
        }

        public bool AllPlaceholdersResolved(string formattedOutput) => _service.AllPlaceholdersResolved(TemplateString, formattedOutput);

        public IReadOnlyList<string> GetUnresolvedPlaceholders(string formattedOutput) => _service.GetUnresolvedPlaceholders(TemplateString, formattedOutput);
    }

    private sealed class ContextBuilder : IContextBuilder
    {
        private readonly IFormatProvider _formatProvider;
        private readonly Dictionary<string, object?> _target;

        public ContextBuilder(Dictionary<string, object?> target, IFormatProvider formatProvider)
        {
            _target = target;
            _formatProvider = formatProvider;
        }

        public IContextBuilder Add(string key, object? value)
        {
            _target[key] = value;
            return this;
        }

        public IContextBuilder Add(string key, object? value, string format)
        {
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(format);
            _target[key] = value is IFormattable formattable ? formattable.ToString(format, _formatProvider) : value?.ToString() ?? string.Empty;
            return this;
        }

        public IContextBuilder Add(string key, object? value, Func<object?, string?> formatter)
        {
            ArgumentHelpers.ThrowIfNull(formatter);
            _target[key] = formatter(value);
            return this;
        }

        public IContextBuilder Add<T>(string key, T? value, Func<T?, string?> formatter)
        {
            ArgumentHelpers.ThrowIfNull(formatter);
            _target[key] = formatter(value);
            return this;
        }

        public IContextBuilder AddIf(string key, object? value, bool condition)
        {
            if (condition)
                _target[key] = value;

            return this;
        }

        public IContextBuilder AddIf(string key, object? value, string format, bool condition)
        {
            if (condition) {
                ArgumentHelpers.ThrowIfNullOrWhiteSpace(format);
                _target[key] = value is IFormattable formattable ? formattable.ToString(format, _formatProvider) : value?.ToString() ?? string.Empty;
            }

            return this;
        }

        public IContextBuilder AddWhen(string key, object? value, Func<object?, bool> predicate)
        {
            ArgumentHelpers.ThrowIfNull(predicate);
            if (predicate(value))
                _target[key] = value;

            return this;
        }

        public IContextBuilder AddWhen<T>(string key, T? value, Func<T?, bool> predicate)
        {
            ArgumentHelpers.ThrowIfNull(predicate);
            if (predicate(value))
                _target[key] = value;

            return this;
        }
    }
}