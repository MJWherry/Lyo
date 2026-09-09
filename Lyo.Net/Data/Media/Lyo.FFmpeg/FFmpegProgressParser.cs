using System.Globalization;
using Lyo.Common.Core.Conversion;
using Lyo.Media.Models;

namespace Lyo.FFmpeg;

/// <summary>
/// Parses ffmpeg <c>-progress</c> <c>key=value</c> lines into <see cref="MediaProgress" />.
/// Prefers <c>out_time_us</c>. Treats <c>out_time_ms</c> as microseconds (ffmpeg 8 stores µs in that key). Falls back to <c>out_time</c>.
/// </summary>
internal sealed class FFmpegProgressParser(IProgress<MediaProgress>? progress, TimeSpan? knownDuration)
{
    private long? _frame;
    private bool _hasMicroseconds;
    private TimeSpan _outTime;
    private double? _speed;

    public void IngestLine(string line)
    {
        if (progress == null || string.IsNullOrWhiteSpace(line))
            return;

        var span = line.AsSpan().Trim();
        var eq = span.IndexOf('=');
        if (eq <= 0)
            return;

        var key = span[..eq];
        var value = span[(eq + 1)..].Trim();
        if (key.Equals("out_time_us", StringComparison.Ordinal))
            TrySetMicroseconds(value);
        else if (key.Equals("out_time_ms", StringComparison.Ordinal))
            TrySetMicroseconds(value);
        else if (key.Equals("out_time", StringComparison.Ordinal))
            TrySetOutTime(value);
        else if (key.Equals("frame", StringComparison.Ordinal)) {
            if (TypeConversion.TryConvertTo<long>(value.ToString(), out var frame) && frame is >= 0)
                _frame = frame;
        }
        else if (key.Equals("speed", StringComparison.Ordinal)) {
            var text = value.ToString();
            if (text.EndsWith('x') || text.EndsWith('X'))
                text = text[..^1];

            if (TypeConversion.TryConvertTo<double>(text, out var speed))
                _speed = speed;
        }
        else if (key.Equals("progress", StringComparison.Ordinal)) {
            Report();
        }
    }

    private void TrySetMicroseconds(ReadOnlySpan<char> value)
    {
        var text = value.ToString();
        if (text.Equals("N/A", StringComparison.OrdinalIgnoreCase))
            return;

        if (!TypeConversion.TryConvertTo<long>(text, out var us) || us < 0)
            return;

        _outTime = TimeSpan.FromTicks(us * 10);
        _hasMicroseconds = true;
    }

    private void TrySetOutTime(ReadOnlySpan<char> value)
    {
        if (_hasMicroseconds)
            return;

        var text = value.ToString();
        if (text.Equals("N/A", StringComparison.OrdinalIgnoreCase))
            return;

        if (TimeSpan.TryParseExact(text, @"hh\:mm\:ss\.ffffff", CultureInfo.InvariantCulture, out var parsed) ||
            TimeSpan.TryParseExact(text, @"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture, out parsed) ||
            TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out parsed))
            _outTime = parsed;
    }

    private void Report()
    {
        if (progress == null)
            return;

        progress.Report(
            new() {
                OutTime = _outTime,
                Duration = knownDuration,
                Speed = _speed,
                Frame = _frame
            });
    }
}
