using System.ComponentModel;
using System.Runtime.CompilerServices;
using Lyo.Exceptions;
using Lyo.Formatter;

namespace Lyo.Formatter.Web.Components;

/// <summary>
/// Shared live-preview state for <see cref="LyoFormatterTemplateEditor" /> and <see cref="LyoFormatterPreview" />. The parent owns the instance so the two components can sit in
/// different layout regions. Template text updates immediately; annotated preview rebuilds after <see cref="DebounceInterval" />.
/// </summary>
public sealed class LyoFormatterLiveSession : INotifyPropertyChanged, IDisposable
{
    /// <summary>Default pause after typing before the preview rebuilds.</summary>
    public const int DefaultDebounceMs = 300;

    private readonly IFormatterService _formatter;
    private object? _context;
    private int _debounceInterval = DefaultDebounceMs;
    private string? _hoveredPlaceholder;
    private CancellationTokenSource? _previewDebounceCts;
    private IReadOnlyList<FormatterSegment> _previewSegments = [];
    private string _template = string.Empty;
    private IReadOnlyList<FormatterSegment> _templateSegments = [];
    private string? _validationError;

    /// <summary>Creates a session that formats with the given <see cref="IFormatterService" /> (typically from DI).</summary>
    public LyoFormatterLiveSession(IFormatterService formatter)
    {
        ArgumentHelpers.ThrowIfNull(formatter);
        _formatter = formatter;
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Raw template. Setter updates the editor overlay immediately and schedules a debounced preview rebuild.</summary>
    public string Template {
        get => _template;
        set {
            var next = value ?? string.Empty;
            if (_template == next)
                return;

            _template = next;
            RebuildTemplateSegments();
            Notify();
            SchedulePreviewRebuild();
        }
    }

    /// <summary>SmartFormat context (DTO, anonymous object, or dictionary). Changing it rebuilds overlay immediately and the preview after debounce.</summary>
    public object? Context {
        get => _context;
        set {
            if (ReferenceEquals(_context, value))
                return;

            _context = value;
            RebuildTemplateSegments();
            Notify();
            SchedulePreviewRebuild();
        }
    }

    /// <summary>Placeholder key currently hovered in either component. Null when nothing is hovered.</summary>
    public string? HoveredPlaceholder {
        get => _hoveredPlaceholder;
        set {
            if (string.Equals(_hoveredPlaceholder, value, StringComparison.OrdinalIgnoreCase))
                return;

            _hoveredPlaceholder = value;
            Notify();
        }
    }

    /// <summary>Milliseconds to wait after the last template/context change before rebuilding <see cref="PreviewSegments" />. Default 300.</summary>
    public int DebounceInterval {
        get => _debounceInterval;
        set {
            var next = Math.Max(0, value);
            if (_debounceInterval == next)
                return;

            _debounceInterval = next;
            Notify();
        }
    }

    /// <summary>Annotated spans for the current (immediate) template. Overlay uses <see cref="FormatterSegment.RawToken" /> for placeholders.</summary>
    public IReadOnlyList<FormatterSegment> TemplateSegments => _templateSegments;

    /// <summary>Annotated spans for the debounced template. Preview uses <see cref="FormatterSegment.Text" /> replacements.</summary>
    public IReadOnlyList<FormatterSegment> PreviewSegments => _previewSegments;

    /// <summary>Parser error for the current template, or null when the template is empty or valid.</summary>
    public string? ValidationError => _validationError;

    /// <summary>Distinct placeholder keys in the current template, in first-seen order.</summary>
    public IReadOnlyList<string> PlaceholderKeys {
        get {
            var keys = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var segment in _templateSegments) {
                if (string.IsNullOrEmpty(segment.PlaceholderKey) || !seen.Add(segment.PlaceholderKey))
                    continue;

                keys.Add(segment.PlaceholderKey);
            }

            return keys;
        }
    }

    /// <summary>True when <paramref name="key" /> matches <see cref="HoveredPlaceholder" />.</summary>
    public bool IsHovered(string? key)
        => !string.IsNullOrEmpty(key) && string.Equals(_hoveredPlaceholder, key, StringComparison.OrdinalIgnoreCase);

    /// <summary>Rebuilds preview spans immediately, skipping debounce. Call after setting initial <see cref="Template" /> and <see cref="Context" />.</summary>
    public void RefreshPreview() => RebuildPreview();

    /// <inheritdoc />
    public void Dispose()
    {
        _previewDebounceCts?.Cancel();
        _previewDebounceCts?.Dispose();
        _previewDebounceCts = null;
    }

    private void SchedulePreviewRebuild()
    {
        _previewDebounceCts?.Cancel();
        _previewDebounceCts?.Dispose();
        _previewDebounceCts = new();
        var ct = _previewDebounceCts.Token;
        var delay = _debounceInterval;
        _ = RebuildPreviewAsync(delay, ct);
    }

    private async Task RebuildPreviewAsync(int delayMs, CancellationToken ct)
    {
        try {
            if (delayMs > 0)
                await Task.Delay(delayMs, ct).ConfigureAwait(false);

            if (ct.IsCancellationRequested)
                return;

            RebuildPreview();
        }
        catch (OperationCanceledException) {
            // superseded by a later keystroke
        }
    }

    private void RebuildTemplateSegments()
    {
        _templateSegments = SafeSegments(_template);
        _validationError = string.IsNullOrEmpty(_template) || _formatter.TryValidateTemplate(_template, out var error) ? null : error;
        Notify(nameof(TemplateSegments));
        Notify(nameof(ValidationError));
        Notify(nameof(PlaceholderKeys));
    }

    private void RebuildPreview()
    {
        _previewSegments = SafeSegments(_template);
        Notify(nameof(PreviewSegments));
    }

    private IReadOnlyList<FormatterSegment> SafeSegments(string template)
    {
        try {
            return _formatter.FormatSegments(template, _context);
        }
        catch {
            return string.IsNullOrEmpty(template) ? [] : [new(FormatterSegmentKind.Literal, template, null, template)];
        }
    }

    private void Notify([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
}
