using Lyo.Diagnostic.StackTrace;
using Lyo.Exceptions;

namespace Lyo.Desktop.Components;

/// <summary>One decoded stack frame shown by <see cref="LogLineView" />.</summary>
/// <param name="Category">User code, system, or test framework.</param>
/// <param name="Method">Method with parameters collapsed.</param>
/// <param name="Location">File and line when the trace has them.</param>
public sealed record LogFrame(string Category, string Method, string? Location);

/// <summary>What <see cref="LogLineView" /> shows for one <see cref="LogLine" />. An exception is decoded with <see cref="StackTraceDecoder" />.</summary>
/// <param name="Message">The log message.</param>
/// <param name="Context">Scope and structured properties.</param>
/// <param name="ExceptionText">Full <see cref="Exception.ToString" /> text, when the line has an exception.</param>
/// <param name="ExceptionMessage">Decoded exception message.</param>
/// <param name="CrashSite">Likely crash site, including file and line when the trace has them.</param>
/// <param name="Confidence">How sure the decoder is about <paramref name="CrashSite" />.</param>
/// <param name="Fingerprint">Stable id for this trace.</param>
/// <param name="Frames">Frames in trace order, outermost call last.</param>
public sealed record LogLineDetail(
    string Message,
    IReadOnlyList<LogField> Context,
    string? ExceptionText,
    string? ExceptionMessage,
    string? CrashSite,
    string? Confidence,
    string? Fingerprint,
    IReadOnlyList<LogFrame> Frames)
{
    /// <summary>Builds the detail for <paramref name="line" />. Lines without an exception have no stack fields.</summary>
    public static LogLineDetail From(LogLine line)
    {
        ArgumentHelpers.ThrowIfNull(line);
        var context = line.Context ?? [];
        if (string.IsNullOrWhiteSpace(line.ExceptionText))
            return new(line.Message, context, null, null, null, null, null, []);

        var decoded = new StackTraceDecoder().Decode(line.ExceptionText);
        var frames = new List<LogFrame>(decoded.AllFrames.Count);
        foreach (var frame in decoded.AllFrames)
            frames.Add(new(frame.Category.ToString(), frame.ShortMethod, frame.LocationSummary));

        return new(
            line.Message,
            context,
            line.ExceptionText,
            BlankToNull(decoded.ExceptionMessage),
            decoded.LikelyCrashSite?.ToString(),
            decoded.CrashSiteConfidence.ToString(),
            BlankToNull(decoded.Fingerprint),
            frames);
    }

    private static string? BlankToNull(string? text) => string.IsNullOrWhiteSpace(text) ? null : text;
}
