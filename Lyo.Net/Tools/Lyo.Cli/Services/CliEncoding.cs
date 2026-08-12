using Lyo.Common.Enums;
using Lyo.Exceptions;
using Lyo.TextEncoding;

namespace Lyo.Cli.Services;

/// <summary>Binary and charset encoding helpers over <see cref="BinaryEncoding" /> / <see cref="CharsetEncoding" />.</summary>
internal static class CliEncoding
{
    public static BinaryEncodingKind ParseKind(string name)
        => name.Trim().ToLowerInvariant() switch {
            "base64" => BinaryEncodingKind.Base64,
            "base64url" or "base64-url" or "url" => BinaryEncodingKind.Base64Url,
            "hex" or "hexadecimal" => BinaryEncodingKind.Hex,
            var _ => throw new ArgumentException($"Unknown encoding kind '{name}'. Use base64, base64url, or hex.")
        };

    public static async Task EncodeAsync(BinaryEncodingKind kind, Stream input, TextWriter output, TextLetterCase hexCase, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(input);
        ArgumentHelpers.ThrowIfNull(output);
        // BinaryEncoding.EncodeAsync may not take hex case on stream path — buffer then encode for hex case control.
        await using var ms = new MemoryStream();
        await input.CopyToAsync(ms, ct).ConfigureAwait(false);
        var encoded = BinaryEncoding.Encode(kind, ms.TryGetBuffer(out var seg) ? seg.AsSpan() : ms.ToArray().AsSpan(), hexCase);
        await output.WriteAsync(encoded.AsMemory(), ct).ConfigureAwait(false);
        await output.FlushAsync(ct).ConfigureAwait(false);
    }

    public static async Task DecodeAsync(BinaryEncodingKind kind, TextReader input, Stream output, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(input);
        ArgumentHelpers.ThrowIfNull(output);
        var text = await input.ReadToEndAsync(ct).ConfigureAwait(false);
        var bytes = BinaryEncoding.Decode(kind, text.AsSpan().Trim());
        await output.WriteAsync(bytes.AsMemory(), ct).ConfigureAwait(false);
        await output.FlushAsync(ct).ConfigureAwait(false);
    }

    public static async Task ConvertCharsetAsync(Stream input, Stream output, string from, string to, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(input);
        ArgumentHelpers.ThrowIfNull(output);
        var fromEnc = CharsetEncoding.GetEncoding(from);
        var toEnc = CharsetEncoding.GetEncoding(to);
        await CharsetEncoding.ConvertAsync(input, output, fromEnc, toEnc, ct: ct).ConfigureAwait(false);
    }

    public static async Task<string> DetectCharsetAsync(Stream input, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(input);
        CharsetDetectionResult result;
        if (input.CanSeek) {
            result = CharsetEncoding.DetectEncoding(input);
            input.Position = 0;
        }
        else {
            await using var ms = new MemoryStream();
            await input.CopyToAsync(ms, ct).ConfigureAwait(false);
            ms.Position = 0;
            result = CharsetEncoding.DetectEncoding(ms);
        }

        var label = result.Encoding.WebName;
        return $"{label} ({result.Kind})";
    }
}