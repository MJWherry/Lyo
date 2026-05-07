using System.Text.RegularExpressions;
using Lyo.Diagnostic.Context;
using Lyo.Diagnostic.StackTrace;
using Lyo.Exceptions;

namespace Lyo.Diagnostic.Sanitisation;

/// <summary>Strips PII-sensitive content (file paths, machine names, server paths) from decoded stack traces before they are exposed in API responses or logs. Thread-safe singleton.</summary>
public sealed class TraceSanitiser : ITraceSanitiser
{
    // Matches Windows absolute paths: C:\Users\..., \\server\share\...
    private static readonly Regex WindowsPathRegex = new(@"[A-Za-z]:\\[^\s]+|\\\\[^\s]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // Matches Unix absolute paths: /home/user/..., /var/app/...
    private static readonly Regex UnixPathRegex = new(@"\/(?:[a-zA-Z0-9_\-\.]+\/)+[^\s]*", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly Regex[] _customPatterns;

    private readonly TraceSanitiserOptions _options;

    public TraceSanitiser(TraceSanitiserOptions? options = null)
    {
        _options = options ?? TraceSanitiserOptions.Default;
        _customPatterns = _options.AdditionalRedactionPatterns
            .Select(p => new Regex(Regex.Escape(p), RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
            .ToArray();
    }

    /// <inheritdoc />
    public SanitisedStackTrace Sanitise(DiagnosticContext context)
    {
        ArgumentHelpers.ThrowIfNull(context);
        return Sanitise(context.Trace);
    }

    /// <inheritdoc />
    public SanitisedStackTrace Sanitise(DecodedStackTrace trace)
    {
        ArgumentHelpers.ThrowIfNull(trace);
        return SanitiseInternal(trace);
    }

    private SanitisedStackTrace SanitiseInternal(DecodedStackTrace trace)
    {
        var frames = trace.AllFrames.Where(FrameIsAllowed).Select(SanitiseFrame).ToList();
        var crashSite = trace.LikelyCrashSite is { } cs && FrameIsAllowed(cs) ? SanitiseLocationSummary(cs) ?? SanitiseCrashSiteWithoutLocation(cs) : null;
        var message = _options.IncludeExceptionMessage ? SanitiseText(trace.ExceptionMessage) : null;
        var inners = trace.InnerExceptions.Select(SanitiseInternal).ToList();
        return new(message, frames, crashSite, trace.CrashSiteConfidence, trace.Fingerprint, trace.UserNamespaces, trace.HasRecursion, inners);
    }

    private bool FrameIsAllowed(StackFrame frame)
        => (!_options.RemoveSystemFrames || frame.Category == FrameCategory.UserCode) && (!_options.RemoveCompilerGeneratedFrames || !frame.IsCompilerGenerated);

    private string? SanitiseCrashSiteWithoutLocation(StackFrame frame)
    {
        // Innermost frame often lacks "in file:line" (release build, stripped PDBs, or truncated dumps) even though we still
        // know the method — keep sanitised output useful in those cases.
        var method = frame.ShortMethod;
        return string.IsNullOrWhiteSpace(method) ? null : SanitiseText(method);
    }

    private SanitisedFrame SanitiseFrame(StackFrame frame) => new(frame.ShortMethod, SanitiseLocationSummary(frame), frame.Category, frame.IsAsync, frame.IsLambda);

    private string? SanitiseLocationSummary(StackFrame frame)
    {
        if (frame.SourceFile is null)
            return null;

        if (_options.StripFilePaths)
            return frame.LocationSummary;

        var path = _options.StripMachinePaths ? RedactPaths(frame.SourceFile) : frame.SourceFile;
        path = ApplyCustomRedactions(path);
        return $"{path}:{frame.SourceLine}";
    }

    private string SanitiseText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var result = _options.StripMachinePaths ? RedactPaths(text) : text;
        return ApplyCustomRedactions(result);
    }

    private string RedactPaths(string input)
    {
        var result = WindowsPathRegex.Replace(input, _options.RedactionPlaceholder);
        result = UnixPathRegex.Replace(result, _options.RedactionPlaceholder);
        return result;
    }

    private string ApplyCustomRedactions(string input) => _customPatterns.Aggregate(input, (current, pattern) => pattern.Replace(current, _options.RedactionPlaceholder));
}