using System.Text;
namespace Lyo.TextEncoding;

/// <summary>Result of charset detection. <see cref="Encoding" /> is never null.</summary>
public sealed class CharsetDetectionResult
{
    /// <summary>Resolved BCL encoding.</summary>
    public required System.Text.Encoding Encoding { get; init; }

    /// <summary>Detection strategy used.</summary>
    public required CharsetDetectionKind Kind { get; init; }

    /// <summary>Well-known or custom catalog entry when mappable.</summary>
    public CharsetInfo? Charset { get; init; }

    /// <summary>Declared label when <see cref="Kind" /> is <see cref="CharsetDetectionKind.TextDeclaration" />.</summary>
    public string? DeclaredName { get; init; }

    /// <summary>
    /// Bytes consumed from a non-seekable stream during detection (empty when the stream was seekable and rewound).
    /// Callers must process these bytes before the remainder of the stream — use <see cref="CharsetEncoding.CreateReplayStream" />.
    /// </summary>
    public byte[] ConsumedPrefix { get; init; } = [];
}
