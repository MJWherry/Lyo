using System.Text;
using Lyo.Exceptions;
using Lyo.Hashing;

namespace Lyo.Cli.Services;

/// <summary>Parses CLI key material from <c>--key</c> / <c>--key-file</c> (hex, base64, or UTF-8 passphrase → SHA-256).</summary>
internal static class KeyMaterial
{
    /// <summary>Resolves raw key bytes. Prefer hex (even length) or base64; otherwise UTF-8 passphrase hashed with SHA-256.</summary>
    public static async Task<byte[]> ResolveAsync(string? key, string? keyFile, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIf(string.IsNullOrWhiteSpace(key) && string.IsNullOrWhiteSpace(keyFile), "Provide --key or --key-file.");
        ArgumentHelpers.ThrowIf(!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(keyFile), "Provide only one of --key or --key-file.");
        var text = !string.IsNullOrWhiteSpace(keyFile) ? (await File.ReadAllTextAsync(keyFile, ct).ConfigureAwait(false)).Trim() : key!.Trim();
        ArgumentHelpers.ThrowIf(string.IsNullOrWhiteSpace(text), "Key material is empty.");
        if (TryParseHex(text, out var hex))
            return hex!;

        if (TryParseBase64(text, out var b64))
            return b64!;

        return Hasher.ComputeSha256(Encoding.UTF8.GetBytes(text));
    }

    /// <summary>Same as <see cref="ResolveAsync" /> but truncates/extends via SHA-2 to <paramref name="byteLength" /> (Gateway-style secret derivation).</summary>
    public static async Task<byte[]> ResolveForLengthAsync(string? key, string? keyFile, int byteLength, CancellationToken ct = default)
    {
        var raw = await ResolveAsync(key, keyFile, ct).ConfigureAwait(false);
        if (raw.Length == byteLength)
            return raw;

        var hash = byteLength <= 32 ? Hasher.ComputeSha256(raw) : Hasher.ComputeSha2(512, raw);
        ArgumentHelpers.ThrowIf(byteLength > hash.Length, $"Requested key length {byteLength} exceeds hash output ({hash.Length}).");
        if (byteLength == hash.Length)
            return hash;

        var truncated = new byte[byteLength];
        hash.AsSpan(0, byteLength).CopyTo(truncated);
        return truncated;
    }

    private static bool TryParseHex(string text, out byte[]? bytes)
    {
        bytes = null;
        if (text.Length == 0 || text.Length % 2 != 0)
            return false;

        foreach (var c in text) {
            if (!Uri.IsHexDigit(c))
                return false;
        }

        try {
            bytes = Convert.FromHexString(text);
            return true;
        }
        catch {
            return false;
        }
    }

    private static bool TryParseBase64(string text, out byte[]? bytes)
    {
        bytes = null;
        try {
            bytes = Convert.FromBase64String(text);
            return bytes.Length > 0;
        }
        catch {
            return false;
        }
    }
}