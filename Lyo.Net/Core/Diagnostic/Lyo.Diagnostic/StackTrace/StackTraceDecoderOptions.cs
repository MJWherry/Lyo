using System.Diagnostics;

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

    /// <summary>Additional namespace prefixes to treat as system / third-party frames, supplementing the built-in list.</summary>
    public IReadOnlyList<string> ExtraSystemPrefixes { get; init; } = [];

    /// <summary>
    /// When true, compiler-generated async state-machine frames (<c>MoveNext</c>) are stripped before analysis (raw frames are still preserved on the model). Defaults to
    /// <see langword="false" />.
    /// </summary>
    public bool StripAsyncNoise { get; init; } = false;

    /// <summary>Minimum number of identical consecutive frames before a run is flagged as recursive. Defaults to 3.</summary>
    public int RecursionThreshold { get; init; } = 3;

    /// <summary>Default singleton with no customisation.</summary>
    public static StackTraceDecoderOptions Default { get; } = new();

    public override string ToString()
        => $"UserPrefixes={UserCodePrefixes.Count} SystemPrefixes={ExtraSystemPrefixes.Count} RecursionThreshold={RecursionThreshold} StripAsync={StripAsyncNoise}";
}