using System.Diagnostics;

namespace Lyo.Diagnostic.StackTrace;

/// <summary>A single <c>at …</c> line from a .NET stack trace, fully parsed. All source-location properties are <see langword="null" /> when PDB information is absent.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record StackFrame(
    string RawText,
    string FullMethod,
    string Namespace,
    string TypeName,
    string MethodName,
    string? SourceFile,
    int? SourceLine,
    FrameCategory Category,
    bool IsAsync,
    bool IsLambda)
{
    /// <summary>Short file name without directory path, or <see langword="null" />.</summary>
    public string? ShortFileName => SourceFile is null ? null : Path.GetFileName(SourceFile);

    /// <summary>Human-readable location, e.g. <c>OrderService.cs:87</c>.</summary>
    public string? LocationSummary => ShortFileName is null ? null : $"{ShortFileName}:{SourceLine}";

    /// <summary>Method with parameters collapsed to <c>(…)</c>.</summary>
    public string ShortMethod {
        get {
            var paren = FullMethod.IndexOf('(');
            return paren > 0 ? FullMethod[..paren] + "(…)" : FullMethod;
        }
    }

    /// <summary>True when this frame is compiler-generated and does not represent a line the developer wrote directly (async state machines or lambdas without PDB source info).</summary>
    public bool IsCompilerGenerated => (IsAsync || IsLambda) && SourceFile is null;

    public override string ToString() => LocationSummary is null ? $"{ShortMethod} [{Category}]" : $"{ShortMethod} [{Category}] @ {LocationSummary}";
}