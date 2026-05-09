using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.QRCode.Payloads;

/// <summary>Arbitrary text or preformatted payload (no transformation).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class PlainTextQrPayload(string text) : IQrPayload
{
    /// <summary>Raw text to encode.</summary>
    public string Text { get; } = ArgumentHelpers.ThrowIfNullReturn(text);

    /// <inheritdoc />
    public override string ToString()
    {
        var t = Text.Length <= 40 ? Text : Text[..40] + "…";
        return $"PlainText ({Text.Length} chars): \"{t}\"";
    }

    /// <inheritdoc />
    public string ToQrString()
    {
        ArgumentHelpers.ThrowIf(string.IsNullOrWhiteSpace(Text), "Plain text payload cannot be empty.", nameof(Text));
        return Text;
    }
}
