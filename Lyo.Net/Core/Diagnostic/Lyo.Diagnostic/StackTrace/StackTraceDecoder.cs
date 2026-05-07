using System.Text;
using System.Text.RegularExpressions;
using Lyo.Exceptions;
using Lyo.Hashing;
using PackageMetadataModel = Lyo.PackageMetadata.PackageMetadata;

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
    private const string SyncDecodeBlockedWhenPackageStoreConfigured =
        "StackTraceDecoderOptions.PackageMetadataStore is set; use DecodeAsync (or clear the store) so package metadata can be resolved asynchronously.";

    /// <summary>Parses a full "at …" frame, optionally with "in file:line N".</summary>
    private static readonly Regex FrameRegex = new(@"^\s*at\s+(.+?)\s*(?:in\s+(.+):line\s+(\d+))?\s*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Splits a stripped method path into ns / type / method segments.</summary>
    private static readonly Regex MethodNameRegex = new(@"^(?<ns>.+?)\.(?<type>[^.]+)\.(?<method>[^.(]+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Detects "   ---> ExceptionType: message" inner-exception separator.</summary>
    private static readonly Regex InnerExceptionSeparator = new(@"^\s*-{3}>\s*.+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Namespace prefixes always treated as BCL / Microsoft platform (not optional NuGet).</summary>
    private static readonly string[] BuiltInSystemPrefixes = ["System.", "Microsoft.", "MS.", "Windows.", "mscorlib.", "Interop.", "Internal."];

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
        if (_options.PackageMetadataStore is not null)
            throw new InvalidOperationException(SyncDecodeBlockedWhenPackageStoreConfigured);

        return DecodeInternal(rawTrace);
    }

    /// <inheritdoc />
    public DecodedStackTrace Decode(Exception exception)
    {
        ArgumentHelpers.ThrowIfNull(exception);
        if (_options.PackageMetadataStore is not null)
            throw new InvalidOperationException(SyncDecodeBlockedWhenPackageStoreConfigured);

        var inners = new List<DecodedStackTrace>();
        var inner = exception.InnerException;
        while (inner is not null) {
            inners.Add(DecodeInternal($"{inner.GetType().FullName}: {inner.Message}\n{inner.StackTrace ?? string.Empty}", []));
            inner = inner.InnerException;
        }

        return DecodeInternal($"{exception.GetType().FullName}: {exception.Message}\n{exception.StackTrace ?? string.Empty}", inners);
    }

    /// <inheritdoc />
    public Task<DecodedStackTrace> DecodeAsync(string rawTrace, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(rawTrace);
        if (_options.PackageMetadataStore is null)
            return Task.FromResult(DecodeInternal(rawTrace));

        return DecodeAsyncWithStore(rawTrace, ct);
    }

    /// <inheritdoc />
    public async Task<DecodedStackTrace> DecodeAsync(Exception exception, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(exception);
        if (_options.PackageMetadataStore is null)
            return Decode(exception);

        var store = _options.PackageMetadataStore;
        var keys = CollectStrippedMethodKeysFromException(exception);
        var map = keys.Count == 0 ? new Dictionary<string, PackageMetadataModel?>() : await store.TryGetManyForStrippedMethodPrefixesAsync(keys.ToList(), ct).ConfigureAwait(false);
        var inners = new List<DecodedStackTrace>();
        var inner = exception.InnerException;
        while (inner is not null) {
            var rawInner = $"{inner.GetType().FullName}: {inner.Message}\n{inner.StackTrace ?? string.Empty}";
            inners.Add(await DecodeInternalAsync(rawInner, [], ct, map).ConfigureAwait(false));
            inner = inner.InnerException;
        }

        var rawOuter = $"{exception.GetType().FullName}: {exception.Message}\n{exception.StackTrace ?? string.Empty}";
        return await DecodeInternalAsync(rawOuter, inners, ct, map).ConfigureAwait(false);
    }

    private async Task<DecodedStackTrace> DecodeAsyncWithStore(string rawTrace, CancellationToken ct)
    {
        var keys = CollectStrippedMethodKeys(rawTrace);
        var map = keys.Count == 0
            ? new Dictionary<string, PackageMetadataModel?>()
            : await _options.PackageMetadataStore!.TryGetManyForStrippedMethodPrefixesAsync(keys.ToList(), ct).ConfigureAwait(false);

        return await DecodeInternalAsync(rawTrace, null, ct, map).ConfigureAwait(false);
    }

    private async Task<DecodedStackTrace> DecodeInternalAsync(
        string rawTrace,
        IReadOnlyList<DecodedStackTrace>? prebuiltInners,
        CancellationToken ct,
        IReadOnlyDictionary<string, PackageMetadataModel?>? enrichmentMap = null)
    {
        SplitTrace(rawTrace, out var messageLines, out var frameLines, out var innerBlocks);
        var allFrames = frameLines.Select(TryParseFrame).OfType<StackFrame>().ToList();
        await EnrichPackageMetadataAsync(allFrames, ct, enrichmentMap).ConfigureAwait(false);
        var analysisFrames = _options.StripAsyncNoise ? allFrames.Where(f => !f.IsCompilerGenerated).ToList() : allFrames;
        var userFrames = analysisFrames.Where(f => f.Category == FrameCategory.UserCode).ToList();
        var systemFrames = analysisFrames.Where(f => f.Category == FrameCategory.SystemOrThirdParty).ToList();
        var testFrames = analysisFrames.Where(f => f.Category == FrameCategory.TestFramework).ToList();
        var asyncFrames = allFrames.Where(f => f.IsAsync).ToList();
        var lambdaFrames = allFrames.Where(f => f.IsLambda).ToList();
        List<DecodedStackTrace> inners;
        if (prebuiltInners is not null)
            inners = prebuiltInners.ToList();
        else {
            inners = new(innerBlocks.Count);
            foreach (var b in innerBlocks)
                inners.Add(await DecodeInternalAsync(b, [], ct, enrichmentMap).ConfigureAwait(false));
        }

        var (crashSite, crashConf) = ResolveLikelyCrashSite(userFrames, inners);
        var deepest = ResolveDeepestUserFrame(userFrames, inners);
        var lastSystem = systemFrames.Count > 0 ? systemFrames[0] : null;
        return new(
            string.Join("\n", messageLines).Trim(), allFrames, userFrames, systemFrames, testFrames, asyncFrames, lambdaFrames, crashSite, crashConf, deepest, lastSystem,
            BuildGroups(analysisFrames), BuildNamespaces(userFrames), DetectRecursion(analysisFrames), BuildFingerprint(userFrames), inners);
    }

    private async Task EnrichPackageMetadataAsync(List<StackFrame> allFrames, CancellationToken ct, IReadOnlyDictionary<string, PackageMetadataModel?>? enrichmentMap)
    {
        if (allFrames.Count == 0)
            return;

        if (enrichmentMap is not null) {
            ApplyPackageMetadataMap(allFrames, enrichmentMap);
            return;
        }

        var store = _options.PackageMetadataStore;
        if (store is null)
            return;

        var keys = new string[allFrames.Count];
        for (var i = 0; i < allFrames.Count; i++)
            keys[i] = StripForAnalysis(allFrames[i].FullMethod);

        var map = await store.TryGetManyForStrippedMethodPrefixesAsync(keys, ct).ConfigureAwait(false);
        ApplyPackageMetadataMap(allFrames, map);
    }

    private static void ApplyPackageMetadataMap(List<StackFrame> allFrames, IReadOnlyDictionary<string, PackageMetadataModel?> map)
    {
        for (var i = 0; i < allFrames.Count; i++) {
            var f = allFrames[i];
            var key = StripForAnalysis(f.FullMethod);
            if (map.TryGetValue(key, out var meta) && meta is not null)
                allFrames[i] = f with { PackageMetadata = meta };
        }
    }

    /// <summary>All stripped methods used for package lookups across this textual trace and nested inner blocks (<see cref="SplitTrace" /> semantics).</summary>
    private static HashSet<string> CollectStrippedMethodKeys(string rawTrace)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        CollectStrippedMethodKeysInto(rawTrace, set);
        return set;
    }

    /// <summary>Union of keys across an exception chain (outer plus every <see cref="Exception.InnerException" />).</summary>
    private static HashSet<string> CollectStrippedMethodKeysFromException(Exception exception)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        var ex = exception;
        while (ex is not null) {
            var raw = $"{ex.GetType().FullName}: {ex.Message}\n{ex.StackTrace ?? string.Empty}";
            CollectStrippedMethodKeysInto(raw, set);
            ex = ex.InnerException;
        }

        return set;
    }

    private static void CollectStrippedMethodKeysInto(string rawTrace, HashSet<string> into)
    {
        SplitTrace(rawTrace, out var _, out var frameLines, out var innerBlocks);
        foreach (var line in frameLines) {
            var match = FrameRegex.Match(line);
            if (!match.Success)
                continue;

            into.Add(StripForAnalysis(match.Groups[1].Value.Trim()));
        }

        foreach (var block in innerBlocks)
            CollectStrippedMethodKeysInto(block, into);
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
        var inners = prebuiltInners ?? innerBlocks.Select(b => DecodeInternal(b, [])).ToList();
        var (crashSite, crashConf) = ResolveLikelyCrashSite(userFrames, inners);
        var deepest = ResolveDeepestUserFrame(userFrames, inners);
        var lastSystem = systemFrames.Count > 0 ? systemFrames[0] : null;
        return new(
            string.Join("\n", messageLines).Trim(), allFrames, userFrames, systemFrames, testFrames, asyncFrames, lambdaFrames, crashSite, crashConf, deepest, lastSystem,
            BuildGroups(analysisFrames), BuildNamespaces(userFrames), DetectRecursion(analysisFrames), BuildFingerprint(userFrames), inners);
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

        if (_systemPrefixes.Any(p => stripped.StartsWith(p, StringComparison.Ordinal)))
            return FrameCategory.SystemOrThirdParty;

        return _options.RestrictUserCodeToListedPrefixes ? FrameCategory.SystemOrThirdParty : FrameCategory.UserCode;
    }

    /// <summary>
    /// Innermost user frame across this exception and nested inners (first user frame in the innermost inner that has one, else outer trace’s innermost user frame). Mirrors
    /// <see cref="ResolveLikelyCrashSite" /> when root cause is user code.
    /// </summary>
    private static StackFrame? ResolveDeepestUserFrame(IReadOnlyList<StackFrame> outerUserFrames, IReadOnlyList<DecodedStackTrace> inners)
    {
        for (var i = inners.Count - 1; i >= 0; i--) {
            var innerUser = inners[i].UserFrames;
            if (innerUser.Count > 0)
                return innerUser[0];
        }

        if (outerUserFrames.Count > 0)
            return outerUserFrames[0];

        return null;
    }

    /// <summary>Uses the first user-code frame of the innermost nested exception that has one (root cause). Falls back to the outer trace’s first user frame.</summary>
    private static (StackFrame? site, CrashSiteConfidence confidence) ResolveLikelyCrashSite(IReadOnlyList<StackFrame> outerUserFrames, IReadOnlyList<DecodedStackTrace> inners)
    {
        for (var i = inners.Count - 1; i >= 0; i--) {
            var innerUser = inners[i].UserFrames;
            if (innerUser.Count > 0) {
                var frame = innerUser[0];
                return (frame, ScoreCrashSite(frame));
            }
        }

        if (outerUserFrames.Count > 0) {
            var frame = outerUserFrames[0];
            return (frame, ScoreCrashSite(frame));
        }

        return (null, CrashSiteConfidence.None);
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
        var threshold = _options.RecursionThreshold;
        var maxCycle = Math.Max(1, _options.MaxRecursionCycleLength);
        var i = 0;
        while (i < frames.Count) {
            var bestRun = 0;
            var bestPeriod = 1;
            var limit = Math.Min(maxCycle, frames.Count - i);
            for (var period = 1; period <= limit; period++) {
                var run = CountRepeatingCycleLength(frames, i, period);
                if (run < threshold)
                    continue;

                // One block of length `period` is not repetition; need at least two full cycles (e.g. ABAB, or AAA).
                var fullCycles = run / period;
                if (fullCycles < 2)
                    continue;

                if (run > bestRun || (run == bestRun && period < bestPeriod)) {
                    bestRun = run;
                    bestPeriod = period;
                }
            }

            if (bestRun > 0) {
                if (SegmentContainsRecursionRelevantFrame(frames, i, bestRun))
                    results.Add(new(frames[i], bestRun, i));

                i += bestRun;
            }
            else
                i++;
        }

        return results;
    }

    /// <summary>
    /// Repeating known BCL / third-party-only tails (e.g. AutoMapper <c>Map → Map</c>) are ignored. Under
    /// <see cref="StackTraceDecoderOptions.RestrictUserCodeToListedPrefixes" />, app namespaces are classified as system for user-frame counting but must still count here so real
    /// overflows are detected.
    /// </summary>
    private bool SegmentContainsRecursionRelevantFrame(IReadOnlyList<StackFrame> frames, int start, int length)
    {
        var end = start + length;
        for (var j = start; j < end; j++) {
            if (FrameIsRecursionRelevantAnchor(frames[j]))
                return true;
        }

        return false;
    }

    private bool FrameIsRecursionRelevantAnchor(StackFrame frame)
    {
        if (frame.Category is FrameCategory.UserCode or FrameCategory.TestFramework)
            return true;

        var stripped = StripForAnalysis(frame.FullMethod);
        return !IsKnownThirdPartyOrBclMethodPrefix(stripped);
    }

    private bool IsKnownThirdPartyOrBclMethodPrefix(string stripped)
    {
        if (TestFrameworkPrefixes.Any(p => stripped.StartsWith(p, StringComparison.Ordinal)))
            return true;

        return _systemPrefixes.Any(p => stripped.StartsWith(p, StringComparison.Ordinal));
    }

    /// <summary>Counts frames starting at <paramref name="i" /> that form full repeats of the first <paramref name="period" /> signatures.</summary>
    private static int CountRepeatingCycleLength(IReadOnlyList<StackFrame> frames, int i, int period)
    {
        if (period <= 0 || i + period > frames.Count)
            return 0;

        var run = period;
        while (i + run + period <= frames.Count && BlockMethodsEqual(frames, i, i + run, period))
            run += period;

        return run;
    }

    private static bool BlockMethodsEqual(IReadOnlyList<StackFrame> frames, int a, int b, int len)
    {
        for (var j = 0; j < len; j++) {
            if (frames[a + j].FullMethod != frames[b + j].FullMethod)
                return false;
        }

        return true;
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