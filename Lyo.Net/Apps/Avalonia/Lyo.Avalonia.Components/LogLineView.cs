using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Lyo.Desktop.Components;

/// <summary>Detail for one log line. Shows the message, scope and structured context, and for an exception the decoded crash site, frames, and raw text.</summary>
public class LogLineView : UserControl
{
    private static readonly FontFamily Mono = new("Consolas, Menlo, DejaVu Sans Mono");
    private static readonly IBrush Dim = new SolidColorBrush(Color.FromRgb(0x9A, 0xA4, 0xB2));

    /// <summary>The line to show.</summary>
    public static readonly StyledProperty<LogLine?> LineProperty = AvaloniaProperty.Register<LogLineView, LogLine?>(nameof(Line));

    private readonly StackPanel _body = new() { Spacing = 12 };

    /// <summary>The line to show. Null draws nothing.</summary>
    public LogLine? Line {
        get => GetValue(LineProperty);
        set => SetValue(LineProperty, value);
    }

    public LogLineView() => Content = new ScrollViewer { Content = _body, Padding = new(4) };

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == LineProperty)
            Rebuild();
    }

    private void Rebuild()
    {
        _body.Children.Clear();
        if (Line is not { } line)
            return;

        var detail = LogLineDetail.From(line);
        var spec = LogLine.LevelChip(line.Level);
        var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        header.Children.Add(
            new TextBlock {
                Text = line.Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff"),
                FontFamily = Mono,
                FontSize = 12,
                Foreground = Dim,
                VerticalAlignment = VerticalAlignment.Center
            });

        header.Children.Add(new Chip { Text = spec.Label, Hue = spec.Hue, VerticalAlignment = VerticalAlignment.Center });
        if (!string.IsNullOrWhiteSpace(line.Source)) {
            header.Children.Add(
                new TextBlock {
                    Text = line.Source,
                    FontFamily = Mono,
                    FontSize = 12,
                    Foreground = Dim,
                    VerticalAlignment = VerticalAlignment.Center
                });
        }

        _body.Children.Add(header);
        _body.Children.Add(Heading("Message"));
        _body.Children.Add(Values([KeyValueEntry.FromText("", detail.Message)]));
        if (detail.Context.Count > 0) {
            _body.Children.Add(Heading("Context"));
            _body.Children.Add(Values(detail.Context.Select(field => KeyValueEntry.FromText(field.Name, field.Value)).ToArray()));
        }

        if (string.IsNullOrWhiteSpace(detail.ExceptionText))
            return;

        var facts = new List<KeyValueEntry>();
        AddFact(facts, "Message", detail.ExceptionMessage);
        AddFact(facts, "Crash site", detail.CrashSite);
        AddFact(facts, "Confidence", detail.Confidence);
        AddFact(facts, "Fingerprint", detail.Fingerprint);
        if (detail.Frames.Count > 0) {
            foreach (var frame in detail.Frames)
                facts.Add(Frame(frame));
        }

        AddFact(facts, "Raw", detail.ExceptionText);
        if (facts.Count > 0) {
            _body.Children.Add(Heading("Exception"));
            _body.Children.Add(Values(facts));
        }
    }

    private static KeyValueEntry Frame(LogFrame frame)
    {
        var hue = frame.Category switch {
            "UserCode" => ChipHues.Coral,
            "TestFramework" => ChipHues.Green,
            var _ => -1
        };

        var where = string.IsNullOrWhiteSpace(frame.Location) ? frame.Method : frame.Method + "  " + frame.Location;
        return KeyValueEntry.FromChip(where, frame.Category, hue, where);
    }

    private static void AddFact(List<KeyValueEntry> rows, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            rows.Add(KeyValueEntry.FromText(name, value));
    }

    private static KeyValueView Values(IReadOnlyList<KeyValueEntry> entries) => new() { Entries = entries };

    private static TextBlock Heading(string text) => new() { Text = text, FontWeight = FontWeight.SemiBold, FontSize = 13 };
}