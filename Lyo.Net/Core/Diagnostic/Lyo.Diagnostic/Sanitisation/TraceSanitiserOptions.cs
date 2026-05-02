using System.Diagnostics;

namespace Lyo.Diagnostic.Sanitisation;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record TraceSanitiserOptions
{
    /// <summary>When true, full source file paths are replaced with just the filename. e.g. "C:\Users\dev\src\MyApp\Services\OrderService.cs" → "OrderService.cs" Default: true.</summary>
    public bool StripFilePaths { get; init; } = true;

    /// <summary>When true, machine-specific path prefixes (drive letters, /home/user, etc.) are removed even if <see cref="StripFilePaths" /> is false. Default: true.</summary>
    public bool StripMachinePaths { get; init; } = true;

    /// <summary>
    /// When true, system and third-party frames are removed from the sanitised output. Useful for minimal API error responses where internal library frames add noise. Default:
    /// false.
    /// </summary>
    public bool RemoveSystemFrames { get; init; }

    /// <summary>When true, compiler-generated frames (async state machines, lambdas without source info) are stripped from the sanitised output. Default: true.</summary>
    public bool RemoveCompilerGeneratedFrames { get; init; } = true;

    /// <summary>
    /// Additional string patterns to redact from frame text and file paths. Useful for stripping internal server names, usernames, or secrets that may appear in paths.
    /// Replacements use <see cref="RedactionPlaceholder" />.
    /// </summary>
    public IReadOnlyList<string> AdditionalRedactionPatterns { get; init; } = [];

    /// <summary>Placeholder text used where content has been redacted. Default: "[redacted]".</summary>
    public string RedactionPlaceholder { get; init; } = "[redacted]";

    /// <summary>When true, the exception message is included in sanitised output. Set false in production if messages may contain PII (e.g. SQL with data). Default: true.</summary>
    public bool IncludeExceptionMessage { get; init; } = true;

    public static TraceSanitiserOptions Default { get; } = new();

    public override string ToString()
        => $"StripPaths={StripFilePaths} RemoveSystem={RemoveSystemFrames} RemoveGenerated={RemoveCompilerGeneratedFrames} IncludeMsg={IncludeExceptionMessage}";

    /// <summary>Aggressive preset: strips paths, system frames, and compiler noise. Suitable for public-facing API error responses.</summary>
    public static TraceSanitiserOptions PublicApiSafe { get; } = new() {
        StripFilePaths = true,
        StripMachinePaths = true,
        RemoveSystemFrames = true,
        RemoveCompilerGeneratedFrames = true,
        IncludeExceptionMessage = false
    };
}