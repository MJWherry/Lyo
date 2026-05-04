using System.Diagnostics;
using Lyo.PackageMetadata;

namespace Lyo.Diagnostic.StackTrace;

/// <summary>Controls how <see cref="StackTraceDecoder" /> classifies and processes frames. Pass into the constructor; safe to share across threads once built.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record StackTraceDecoderOptions
{
    /// <summary>
    /// Additional namespace prefixes to treat as user / application code. Use this to mark internal shared libraries that you own. Example:
    /// <c>["MyCompany.Core.", "MyCompany.Shared."]</c>
    /// </summary>
    public IReadOnlyList<string> UserCodePrefixes { get; init; } = [];

    /// <summary>
    /// Additional namespace prefixes to treat as system / third-party frames. Use for NuGet and vendor SDKs you do not own
    /// (or load from application config / database). Apart from the decoder’s small fixed BCL / Microsoft list, nothing else is
    /// hard-coded.
    /// </summary>
    public IReadOnlyList<string> ExtraSystemPrefixes { get; init; } = [];

    /// <summary>
    /// Optional store that resolves NuGet-style package metadata for frames. When set, use <see cref="IStackTraceDecoder.DecodeAsync" /> so lookups can run asynchronously
    /// (sync <see cref="IStackTraceDecoder.Decode" /> throws).
    /// </summary>
    public IPackageMetadataStore? PackageMetadataStore { get; init; }

    /// <summary>
    /// When <see langword="true" />, only frames whose method signature starts with a <see cref="UserCodePrefixes" /> entry are
    /// classified as <see cref="FrameCategory.UserCode" />. Everything else that is not test or known system code is treated as
    /// <see cref="FrameCategory.SystemOrThirdParty" />. When <see langword="false" /> (default), unknown namespaces are still
    /// treated as user code so decode works out-of-the-box without configuration.
    /// </summary>
    public bool RestrictUserCodeToListedPrefixes { get; init; } = false;

    /// <summary>
    /// When true, compiler-generated async state-machine frames (<c>MoveNext</c>) are stripped before analysis (raw frames are still preserved on the model). Defaults to
    /// <see langword="false" />.
    /// </summary>
    public bool StripAsyncNoise { get; init; } = false;

    /// <summary>
    /// Minimum total frames in a repeating stack segment before it is flagged as recursive. The repeat may be a single method
    /// (direct recursion) or a cycle of frames (mutual recursion). Defaults to 3.
    /// </summary>
    public int RecursionThreshold { get; init; } = 3;

    /// <summary>
    /// Largest cycle length (in frames) to try when detecting mutual recursion. Keeps decoding cheap on long stacks. Defaults
    /// to 12.
    /// </summary>
    public int MaxRecursionCycleLength { get; init; } = 12;

    /// <summary>Default singleton with no customisation.</summary>
    public static StackTraceDecoderOptions Default { get; } = new();

    public override string ToString()
        => $"UserPrefixes={UserCodePrefixes.Count} SystemPrefixes={ExtraSystemPrefixes.Count} PackageMetadataStore={(PackageMetadataStore is null ? "none" : "set")} RestrictUser={RestrictUserCodeToListedPrefixes} RecursionThreshold={RecursionThreshold} MaxRecursionCycle={MaxRecursionCycleLength} StripAsync={StripAsyncNoise}";
}
