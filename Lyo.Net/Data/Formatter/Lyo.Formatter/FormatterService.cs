using System.Globalization;
using System.Text.RegularExpressions;
using Lyo.Exceptions;
using SmartFormat;
using SmartFormat.Core.Settings;

namespace Lyo.Formatter;

/// <summary>Service for formatting text templates using SmartFormat with named placeholders, pluralization, localization, and rich formatting.</summary>
public sealed class FormatterService : IFormatterService
{
    private static readonly Regex PlaceholderRegex = new(@"\{([^{}:|]+)", RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of the <see cref="FormatterService" /> class with the default SmartFormat configuration. FormatErrorAction is set to MaintainTokens so missing
    /// placeholders are left in the output (enables GetUnresolvedPlaceholders to detect them) instead of throwing.
    /// </summary>
    public FormatterService()
    {
        var settings = new SmartSettings();
        settings.Formatter.ErrorAction = FormatErrorAction.MaintainTokens;
        settings.CaseSensitivity = CaseSensitivityType.CaseInsensitive;
        Formatter = Smart.CreateDefaultSmartFormat(settings);
    }

    /// <summary>Initializes a new instance of the <see cref="FormatterService" /> class with a custom SmartFormatter.</summary>
    /// <param name="formatter">The SmartFormatter instance to use. Must not be null.</param>
    public FormatterService(SmartFormatter formatter)
    {
        ArgumentHelpers.ThrowIfNull(formatter, nameof(formatter));
        Formatter = formatter;
    }

    /// <inheritdoc />
    public SmartFormatter Formatter { get; }

    /// <inheritdoc />
    public CultureInfo? Culture { get; set; }

    /// <inheritdoc />
    public string Format(string template, object? context)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(template, nameof(template));
        var provider = Culture ?? CultureInfo.CurrentCulture;
        return context is null ? Formatter.Format(provider, template) : Formatter.Format(provider, template, context);
    }

    /// <inheritdoc />
    public string Format(string template, params object?[] contextItems)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(template, nameof(template));
        ArgumentHelpers.ThrowIfNull(contextItems, nameof(contextItems));
        var provider = Culture ?? CultureInfo.CurrentCulture;
        return Formatter.Format(provider, template, contextItems);
    }

    /// <inheritdoc />
    public string Format(string template, IReadOnlyDictionary<string, object?> context)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(template, nameof(template));
        ArgumentHelpers.ThrowIfNull(context, nameof(context));
        var provider = Culture ?? CultureInfo.CurrentCulture;
        return Formatter.Format(provider, template, context);
    }

    /// <inheritdoc />
    public string Format(string template, Action<IContextBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(template, nameof(template));
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
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
            var provider = Culture ?? CultureInfo.CurrentCulture;
            result = context is null ? Formatter.Format(provider, template) : Formatter.Format(provider, template, context);
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

        var matches = PlaceholderRegex.Matches(template);
        var placeholders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in matches) {
            if (match.Groups.Count > 1 && match.Groups[1].Success)
                placeholders.Add(match.Groups[1].Value.Trim());
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

        var placeholders = GetPlaceholders(template);
        var unresolved = new List<string>();
        foreach (var placeholder in placeholders) {
            if (IsUnresolvedPlaceholder(formattedOutput, placeholder))
                unresolved.Add(placeholder);
        }

        return unresolved;
    }

    /// <inheritdoc />
    public ITemplate CreateTemplate(string template)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(template, nameof(template));
        return new Template(this, template);
    }

    private static bool IsUnresolvedPlaceholder(string output, string placeholder)
    {
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
            ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
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
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(format, nameof(format));
            _target[key] = value is IFormattable formattable ? formattable.ToString(format, _formatProvider) : value?.ToString() ?? string.Empty;
            return this;
        }

        public IContextBuilder Add(string key, object? value, Func<object?, string?> formatter)
        {
            ArgumentHelpers.ThrowIfNull(formatter, nameof(formatter));
            _target[key] = formatter(value);
            return this;
        }

        public IContextBuilder Add<T>(string key, T? value, Func<T?, string?> formatter)
        {
            ArgumentHelpers.ThrowIfNull(formatter, nameof(formatter));
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
                ArgumentHelpers.ThrowIfNullOrWhiteSpace(format, nameof(format));
                _target[key] = value is IFormattable formattable ? formattable.ToString(format, _formatProvider) : value?.ToString() ?? string.Empty;
            }

            return this;
        }

        public IContextBuilder AddWhen(string key, object? value, Func<object?, bool> predicate)
        {
            ArgumentHelpers.ThrowIfNull(predicate, nameof(predicate));
            if (predicate(value))
                _target[key] = value;

            return this;
        }

        public IContextBuilder AddWhen<T>(string key, T? value, Func<T?, bool> predicate)
        {
            ArgumentHelpers.ThrowIfNull(predicate, nameof(predicate));
            if (predicate(value))
                _target[key] = value;

            return this;
        }
    }
}