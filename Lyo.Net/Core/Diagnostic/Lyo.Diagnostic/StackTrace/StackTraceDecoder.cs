using System.Text;
using System.Text.RegularExpressions;
using Lyo.Common;
using Lyo.Common.Security;
using Lyo.Exceptions;

namespace Lyo.Diagnostic.StackTrace;

/// <summary>Default implementation of <see cref="IStackTraceDecoder" />. Thread-safe after construction. Register as <c>Singleton</c> in DI.</summary>
/// <example>
/// <code>
/// // Program.cs
/// services.AddSingleton&lt;IStackTraceDecoder&gt;(
///     new StackTraceDecoder(new StackTraceDecoderOptions
///     {
///         UserCodePrefixes = ["MyCompany."],
///     }));
/// 
/// // Exception-handling middleware / interceptor:
/// var decoded = _decoder.Decode(exception);
/// return new ErrorResponse
/// {
///     Message        = decoded.ExceptionMessage,
///     CrashSite      = decoded.LikelyCrashSite?.LocationSummary,
///     Confidence     = decoded.CrashSiteConfidence.ToString(),
///     Fingerprint    = decoded.Fingerprint,
///     HasRecursion   = decoded.HasRecursion,
///     UserNamespaces = decoded.UserNamespaces,
/// };
/// </code>
/// </example>
public sealed class StackTraceDecoder : IStackTraceDecoder
{
    /// <summary>Parses a full "at …" frame, optionally with "in file:line N".</summary>
    private static readonly Regex FrameRegex = new(@"^\s*at\s+(.+?)\s*(?:in\s+(.+):line\s+(\d+))?\s*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Splits a stripped method path into ns / type / method segments.</summary>
    private static readonly Regex MethodNameRegex = new(@"^(?<ns>.+?)\.(?<type>[^.]+)\.(?<method>[^.(]+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Detects "   ---> ExceptionType: message" inner-exception separator.</summary>
    private static readonly Regex InnerExceptionSeparator = new(@"^\s*-{3}>\s*.+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly string[] BuiltInSystemPrefixes = [
        "System.", "Microsoft.", "MS.", "Windows.", "mscorlib.", "Interop.", "Internal.", "Newtonsoft.", "AutoMapper.", "Serilog.",
        "NLog.", "log4net.", "FluentValidation.", "MediatR.", "Polly.", "StackExchange.", "RabbitMQ.", "Npgsql.", "MySql.", "MongoDB.",
        "Azure.", "AWSSDK.", "Grpc.", "RestSharp.", "Refit."
    ];

    private static readonly string[] TestFrameworkPrefixes = [
        "NUnit.", "Xunit.", "xUnit.", "MSTest.", "Microsoft.VisualStudio.TestTools.", "Microsoft.VisualStudio.TestPlatform.", "NUnit3."
    ];

    private static readonly string[] AsyncMarkers = ["MoveNext", "AsyncTaskMethodBuilder", "AsyncStateMachine"];
    
    private readonly StackTraceDecoderOptions _options;
    private readonly string[] _systemPrefixes;
    private readonly string[] _userPrefixes;

    public StackTraceDecoder(StackTraceDecoderOptions? options = null)
    {
        _options = options ?? StackTraceDecoderOptions.Default;
        _systemPrefixes = [.. BuiltInSystemPrefixes, .. _options.ExtraSystemPrefixes];
        _userPrefixes = [.. _options.UserCodePrefixes];
    }

    /// <inheritdoc />
    public DecodedStackTrace Decode(string rawTrace)
    {
        ArgumentHelpers.ThrowIfNull(rawTrace);
        return DecodeInternal(rawTrace);
    }

    /// <inheritdoc />
    public DecodedStackTrace Decode(Exception exception)
    {
        ArgumentHelpers.ThrowIfNull(exception);

        var inners = new List<DecodedStackTrace>();
        var inner = exception.InnerException;
        while (inner is not null) {
            inners.Add(DecodeInternal($"{inner.GetType().FullName}: {inner.Message}\n{inner.StackTrace ?? string.Empty}", []));
            inner = inner.InnerException;
        }

        return DecodeInternal($"{exception.GetType().FullName}: {exception.Message}\n{exception.StackTrace ?? string.Empty}", inners);
    }

    private DecodedStackTrace DecodeInternal(string rawTrace, IReadOnlyList<DecodedStackTrace>? prebuiltInners = null)
    {
        SplitTrace(rawTrace, out var messageLines, out var frameLines, out var innerBlocks);
        var allFrames = frameLines.Select(TryParseFrame).OfType<StackFrame>().ToList();
        var analysisFrames = _options.StripAsyncNoise ? allFrames.Where(f => !f.IsCompilerGenerated).ToList() : allFrames;
        var userFrames = analysisFrames.Where(f => f.Category == FrameCategory.UserCode).ToList();
        var systemFrames = analysisFrames.Where(f => f.Category == FrameCategory.SystemOrThirdParty).ToList();
        var testFrames = analysisFrames.Where(f => f.Category == FrameCategory.TestFramework).ToList();
        var asyncFrames = allFrames.Where(f => f.IsAsync).ToList();
        var lambdaFrames = allFrames.Where(f => f.IsLambda).ToList();
        var crashSite = userFrames.Count > 0 ? userFrames[0] : null;
        var deepest = userFrames.Count > 0 ? userFrames[^1] : null;
        var lastSystem = systemFrames.Count > 0 ? systemFrames[0] : null;
        var inners = prebuiltInners ?? innerBlocks.Select(b => DecodeInternal(b, [])).ToList();
        return new(
            string.Join("\\n", messageLines).Trim(), allFrames, userFrames, systemFrames, testFrames, asyncFrames, lambdaFrames, crashSite, ScoreCrashSite(crashSite), deepest,
            lastSystem, BuildGroups(analysisFrames), BuildNamespaces(userFrames), DetectRecursion(analysisFrames), BuildFingerprint(userFrames), inners);
    }

    private static void SplitTrace(string raw, out List<string> messageLines, out List<string> frameLines, out List<string> innerBlocks)
    {
        messageLines = [];
        frameLines = [];
        innerBlocks = [];
        var lines = raw.Split('\n');
        var seenAt = false;
        var innerBuilder = (StringBuilder?)null;
        foreach (var line in lines) {
            var trimmed = line.TrimStart();

            if (InnerExceptionSeparator.IsMatch(line)) {
                if (innerBuilder is not null)
                    innerBlocks.Add(innerBuilder.ToString());

                innerBuilder = new();
                var sepIdx = line.IndexOf('>') + 1;
                innerBuilder.AppendLine(line[sepIdx..].Trim());
                continue;
            }

            if (innerBuilder is not null) {
                if (trimmed.StartsWith("--- End of inner", StringComparison.Ordinal)) {
                    innerBlocks.Add(innerBuilder.ToString());
                    innerBuilder = null;
                }
                else
                    innerBuilder.AppendLine(line);

                continue;
            }

            if (!seenAt && trimmed.StartsWith("at ", StringComparison.Ordinal))
                seenAt = true;

            if (seenAt)
                frameLines.Add(line);
            else
                messageLines.Add(line);
        }

        if (innerBuilder is not null)
            innerBlocks.Add(innerBuilder.ToString());
    }

    private StackFrame? TryParseFrame(string line)
    {
        var match = FrameRegex.Match(line);
        if (!match.Success)
            return null;

        var fullMethod = match.Groups[1].Value.Trim();
        var filePath = match.Groups[2].Success ? match.Groups[2].Value.Trim() : null;
        var lineNum = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : (int?)null;
        var stripped = StripForAnalysis(fullMethod);
        var (ns, typeName, meth) = SplitMethodParts(stripped);
        var isAsync = AsyncMarkers.Any(m => stripped.Contains(m, StringComparison.Ordinal));
        var isLambda = fullMethod.Contains('<') && fullMethod.Contains('>');
        return new(line.Trim(), fullMethod, ns, typeName, meth, filePath, lineNum, ClassifyFrame(stripped), isAsync, isLambda);
    }

    private static string StripForAnalysis(string method)
    {
        var paren = method.IndexOf('(');
        var stripped = paren >= 0 ? method[..paren] : method;
        return Regex.Replace(stripped, @"`\d+", string.Empty).Trim();
    }

    private static (string ns, string typeName, string methodName) SplitMethodParts(string stripped)
    {
        var m = MethodNameRegex.Match(stripped);
        if (m.Success)
            return (m.Groups["ns"].Value, m.Groups["type"].Value, m.Groups["method"].Value);

        var lastDot = stripped.LastIndexOf('.');
        if (lastDot > 0) {
            var secondLast = stripped.LastIndexOf('.', lastDot - 1);
            return secondLast > 0
                ? (stripped[..secondLast], stripped[(secondLast + 1)..lastDot], stripped[(lastDot + 1)..])
                : (string.Empty, stripped[..lastDot], stripped[(lastDot + 1)..]);
        }

        return (string.Empty, string.Empty, stripped);
    }
    
    private FrameCategory ClassifyFrame(string stripped)
    {
        // User-supplied prefixes take priority so internal libs are never
        // misclassified as third-party
        if (_userPrefixes.Length > 0 && _userPrefixes.Any(p => stripped.StartsWith(p, StringComparison.Ordinal)))
            return FrameCategory.UserCode;

        if (TestFrameworkPrefixes.Any(p => stripped.StartsWith(p, StringComparison.Ordinal)))
            return FrameCategory.TestFramework;

        return _systemPrefixes.Any(p => stripped.StartsWith(p, StringComparison.Ordinal)) 
            ? FrameCategory.SystemOrThirdParty 
            : FrameCategory.UserCode;
    }
    
    private static CrashSiteConfidence ScoreCrashSite(StackFrame? frame)
    {
        if (frame is null)
            return CrashSiteConfidence.None;

        if (frame.IsCompilerGenerated)
            return CrashSiteConfidence.Low;

        return (frame.SourceFile is not null, !frame.IsAsync && !frame.IsLambda) switch {
            (true, true) => CrashSiteConfidence.High,
            (true, false) => CrashSiteConfidence.Medium,
            (false, true) => CrashSiteConfidence.Medium,
            (false, false) => CrashSiteConfidence.Low
        };
    }
    
    private static IReadOnlyList<FrameGroup> BuildGroups(IReadOnlyList<StackFrame> frames)
    {
        var groups = new List<FrameGroup>();
        if (frames.Count == 0)
            return groups;

        var current = new List<StackFrame> { frames[0] };
        var currentCat = frames[0].Category;
        foreach (var frame in frames.Skip(1)) {
            if (frame.Category == currentCat)
                current.Add(frame);
            else {
                groups.Add(new(currentCat, current));
                current = [frame];
                currentCat = frame.Category;
            }
        }

        groups.Add(new(currentCat, current));
        return groups;
    }

    private static IReadOnlyList<string> BuildNamespaces(IReadOnlyList<StackFrame> userFrames)
        => userFrames.Select(f => f.Namespace).Where(ns => !string.IsNullOrEmpty(ns)).Distinct(StringComparer.Ordinal).OrderBy(ns => ns, StringComparer.Ordinal).ToList();
    
    private IReadOnlyList<RecursionInfo> DetectRecursion(IReadOnlyList<StackFrame> frames)
    {
        var results = new List<RecursionInfo>();
        var i = 0;
        while (i < frames.Count) {
            var run = 1;
            var pivot = frames[i].FullMethod;
            while (i + run < frames.Count && frames[i + run].FullMethod == pivot)
                run++;

            if (run >= _options.RecursionThreshold)
                results.Add(new(frames[i], run, i));

            i += run;
        }

        return results;
    }

    private static string BuildFingerprint(IReadOnlyList<StackFrame> userFrames)
    {
        // Hash method signatures only (no line numbers) so the fingerprint is
        // stable across cosmetic edits that shift line numbers.
        var key = string.Join("|", userFrames.Select(f => f.FullMethod));
        var hash = Hasher.ComputeSha2(256, Encoding.UTF8.GetBytes(key));
        // First 8 bytes → 16 uppercase hex chars (64-bit prefix of SHA-256).
        return HexEncoding.ToHexString(hash.AsSpan(0, 8));
    }
}