using System.Text;
using Lyo.Exceptions;
using Microsoft.Extensions.Logging;

namespace Lyo.Desktop.Components;

/// <summary>One named value from a log scope or a structured message property.</summary>
/// <param name="Name">Property name. <c>{OriginalFormat}</c> is not stored.</param>
/// <param name="Value">Printed value.</param>
public sealed record LogField(string Name, string Value);

/// <summary>One row in a <see cref="LogView" />. <paramref name="Timestamp" /> is UTC when the line comes from <see cref="LogSink" />.</summary>
/// <param name="Timestamp">When the line was recorded.</param>
/// <param name="Level">Severity used for the chip and the level filter.</param>
/// <param name="Message">Primary text.</param>
/// <param name="Source">Logger or component name. Blank is omitted from the row and from <see cref="Format" />.</param>
/// <param name="Context">Scope and message properties. Null when the line has none.</param>
/// <param name="ExceptionText"><see cref="Exception.ToString" /> when the line was logged with an exception.</param>
public sealed record LogLine(
    DateTimeOffset Timestamp,
    LogLevel Level,
    string Message,
    string? Source = null,
    IReadOnlyList<LogField>? Context = null,
    string? ExceptionText = null)
{
    /// <summary>True when <paramref name="line" /> is at least <paramref name="minLevel" /> and its message or source contains <paramref name="search" />.</summary>
    /// <remarks>A blank search matches every line that passes the level check. Comparison is ordinal and case-insensitive.</remarks>
    public static bool Matches(LogLine line, LogLevel minLevel, string? search)
    {
        ArgumentHelpers.ThrowIfNull(line);
        if (line.Level < minLevel)
            return false;
        if (string.IsNullOrWhiteSpace(search))
            return true;
        var term = search.Trim();
        return line.Message.Contains(term, StringComparison.OrdinalIgnoreCase)
            || (line.Source?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    /// <summary>Clipboard and export text. Each line is the round-trip timestamp, the level name, the source when it has text, and the message.</summary>
    public static string Format(IEnumerable<LogLine> lines)
    {
        ArgumentHelpers.ThrowIfNull(lines);
        var builder = new StringBuilder();
        foreach (var line in lines) {
            builder.Append(line.Timestamp.ToString("O")).Append(' ').Append(line.Level);
            if (!string.IsNullOrWhiteSpace(line.Source))
                builder.Append(' ').Append(line.Source);
            builder.Append(' ').AppendLine(line.Message);
        }

        return builder.ToString();
    }

    /// <summary>Short label and hue for <paramref name="level" />. Trace, and any level below it, is a neutral chip.</summary>
    public static ChipSpec LevelChip(LogLevel level) => level switch {
        LogLevel.Debug => new("Debug", ChipHues.Cyan),
        LogLevel.Information => new("Info", ChipHues.Blue),
        LogLevel.Warning => new("Warn", ChipHues.Amber),
        LogLevel.Error => new("Error", ChipHues.Coral),
        LogLevel.Critical => new("Crit", ChipHues.Rose),
        _ => new("Trace", -1)
    };
}
