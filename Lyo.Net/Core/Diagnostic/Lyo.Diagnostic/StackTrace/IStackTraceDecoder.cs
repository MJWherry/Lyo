namespace Lyo.Diagnostic.StackTrace;

/// <summary>Decodes raw .NET stack-trace strings into structured <see cref="DecodedStackTrace" /> models.</summary>
public interface IStackTraceDecoder
{
    /// <summary>Decodes a raw stack-trace string.</summary>
    /// <exception cref="System.InvalidOperationException">
    /// When <see cref="StackTraceDecoderOptions.PackageMetadataStore" /> is configured; use
    /// <see cref="DecodeAsync(string, CancellationToken)" /> instead.
    /// </exception>
    DecodedStackTrace Decode(string rawTrace);

    /// <summary>Convenience overload: decodes directly from a live <see cref="Exception" />, walking the full inner-exception chain automatically.</summary>
    /// <exception cref="System.InvalidOperationException">
    /// When <see cref="StackTraceDecoderOptions.PackageMetadataStore" /> is configured; use
    /// <see cref="DecodeAsync(Exception, CancellationToken)" /> instead.
    /// </exception>
    DecodedStackTrace Decode(Exception exception);

    /// <summary>Decodes a raw stack-trace string. Required when <see cref="StackTraceDecoderOptions.PackageMetadataStore" /> is set.</summary>
    Task<DecodedStackTrace> DecodeAsync(string rawTrace, CancellationToken ct = default);

    /// <summary>Decodes from a live <see cref="Exception" />, including inner exceptions. Required when <see cref="StackTraceDecoderOptions.PackageMetadataStore" /> is set.</summary>
    Task<DecodedStackTrace> DecodeAsync(Exception exception, CancellationToken ct = default);
}