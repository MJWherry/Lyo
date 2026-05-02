using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Lyo.Exceptions;
using Lyo.Privacy.Abstractions;
using Lyo.Privacy.Rules;

namespace Lyo.Privacy.Policy;

/// <summary>
/// Stable fingerprints of a <see cref="RedactionPolicy" /> for audit (SHA-256 prefix, lowercase hex). Does not include secret literals from rules; built-ins append coarse
/// option data only.
/// </summary>
public static class RedactionPolicyFingerprint
{
    /// <param name="hexCharCount">Even number ≤ 64 recommended; default 16.</param>
    public static string ComputeSha256HexPrefix(RedactionPolicy policy, int hexCharCount = 16)
    {
        ArgumentHelpers.ThrowIfNull(policy);
        ArgumentHelpers.ThrowIfNotInRange(hexCharCount, 0, 64);
        using var sha = SHA256.Create();
        using var ms = new MemoryStream();
        WriteUtf8Line(ms, policy.Name ?? "");
        WriteUtf8Line(ms, policy.Placeholder);
        WriteUtf8Line(ms, policy.MergeAdjacentRuns ? "1" : "0");
        foreach (var n in policy.NeverRedactSubstrings.OrderBy(s => s, StringComparer.Ordinal))
            WriteUtf8Line(ms, "never:" + n);

        foreach (var r in policy.Rules) {
            WriteUtf8Line(ms, "rule:" + r.Kind + ":" + (r.GetType().FullName ?? r.GetType().Name));
            AppendRuleDetail(ms, r);
        }

        var hash = sha.ComputeHash(ms.ToArray());
        return ToHexPrefix(hash, hexCharCount);
    }

    private static void AppendRuleDetail(Stream ms, IRedactionRule r)
    {
        switch (r) {
            case PaymentCardRedactionRule p:
                WriteUtf8Line(ms, "bins:" + FormatBins(p.AllowedBin6) + ":" + FormatBins(p.BlockedBin6));
                break;
            case ApiSecretRedactionRule a:
                WriteUtf8Line(ms, "api:" + a.Patterns + ":" + a.MinEntropyBitsPerChar + ":" + a.MinimumAssignmentValueLength);
                break;
            case NationalIdRedactionRule n:
                WriteUtf8Line(ms, "nid:" + n.Packs);
                break;
            case BankAccountNumberRedactionRule b:
                WriteUtf8Line(ms, "bank:" + b.MinNumericValue);
                break;
            case PhoneRedactionRule ph:
                WriteUtf8Line(ms, "phoneMin:" + ph.MinDigits + ":" + JsonSerializer.Serialize(ph.MaskOptions));
                break;
            case EmailRedactionRule em:
                WriteUtf8Line(ms, "email:" + JsonSerializer.Serialize(em.Options));
                break;
            case RegexRedactionRule rr:
                WriteUtf8Line(ms, "regex:" + StringComparer.Ordinal.GetHashCode(rr.Pattern) + ":" + (int)rr.EffectiveRegexOptions);
                break;
            case IpAddressRedactionRule ip:
                WriteUtf8Line(ms, "ip:" + ip.Mode);
                break;
        }
    }

    private static string FormatBins(HashSet<string>? s) 
        => s is null or { Count: 0 } 
            ? "-" 
            : string.Join(",", s.OrderBy(x => x, StringComparer.Ordinal));

    private static void WriteUtf8Line(Stream s, string line)
    {
        var b = Encoding.UTF8.GetBytes(line + "\n");
        s.Write(b, 0, b.Length);
    }

    private static string ToHexPrefix(byte[] hash, int hexCharCount)
    {
        var n = Math.Min(hexCharCount / 2, hash.Length);
        var chars = new char[n * 2];
        for (var i = 0; i < n; i++) {
            var b = hash[i];
            chars[2 * i] = NibbleHex(b >> 4);
            chars[2 * i + 1] = NibbleHex(b & 0xF);
        }

        return new(chars);

        static char NibbleHex(int v)
        {
            v &= 0xF;
            return (char)(v < 10 ? '0' + v : 'a' + (v - 10));
        }
    }
}