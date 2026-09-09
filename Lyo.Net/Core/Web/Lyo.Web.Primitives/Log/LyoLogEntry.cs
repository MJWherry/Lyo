namespace Lyo.Web.Primitives;

/// <summary>One row shown in <see cref="LyoLogViewer" />.</summary>
/// <param name="Timestamp">When the event occurred. Prefer UTC; the viewer formats it locally.</param>
/// <param name="Level">Severity used for colour and the level filter.</param>
/// <param name="Message">Primary line of text.</param>
/// <param name="Source">Optional logger or component name shown before the message.</param>
public sealed record LyoLogEntry(DateTimeOffset Timestamp, LyoLogLevel Level, string Message, string? Source = null);
