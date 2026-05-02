namespace Lyo.Diagnostic.StackTrace;

/// <summary>Decodes raw .NET stack-trace strings into structured <see cref="DecodedStackTrace" /> models.</summary>
public interface IStackTraceDecoder
{
    /// <summary>Decodes a raw stack-trace string.</summary>
    DecodedStackTrace Decode(string rawTrace);

    /// <summary>Convenience overload: decodes directly from a live <see cref="Exception" />, walking the full inner-exception chain automatically.</summary>
    DecodedStackTrace Decode(Exception exception);
}
