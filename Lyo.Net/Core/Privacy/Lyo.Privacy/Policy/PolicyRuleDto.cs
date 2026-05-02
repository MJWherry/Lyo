using System.Text.Json.Serialization;
using Lyo.Privacy.Rules;

namespace Lyo.Privacy.Policy;

/// <summary>One rule entry in policy JSON.</summary>
public sealed class PolicyRuleDto
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "";

    [JsonPropertyName("emailMask")]
    public string? EmailMask { get; set; }

    [JsonPropertyName("emailLocalPrefix")]
    public int? EmailLocalPrefix { get; set; }

    [JsonPropertyName("emailOptions")]
    public EmailMaskOptions? EmailOptions { get; set; }

    [JsonPropertyName("phoneMode")]
    public string? PhoneMode { get; set; }

    [JsonPropertyName("phoneDigits")]
    public int? PhoneDigits { get; set; }

    [JsonPropertyName("phoneMinDigits")]
    public int? PhoneMinDigits { get; set; }

    [JsonPropertyName("phoneMask")]
    public PhoneMaskOptions? PhoneMask { get; set; }

    [JsonPropertyName("ipMode")]
    public string? IpMode { get; set; }

    [JsonPropertyName("literal")]
    public string? Literal { get; set; }

    [JsonPropertyName("literalIgnoreCase")]
    public bool? LiteralIgnoreCase { get; set; }

    [JsonPropertyName("regex")]
    public string? Regex { get; set; }

    [JsonPropertyName("regexKind")]
    public string? RegexKind { get; set; }

    [JsonPropertyName("blockedBins")]
    public List<string>? BlockedBins { get; set; }

    [JsonPropertyName("allowedBins")]
    public List<string>? AllowedBins { get; set; }

    [JsonPropertyName("nationalIdPacks")]
    public List<string>? NationalIdPacks { get; set; }

    [JsonPropertyName("apiPatterns")]
    public List<string>? ApiPatterns { get; set; }

    [JsonPropertyName("apiMinEntropy")]
    public double? ApiMinEntropy { get; set; }

    [JsonPropertyName("bankMinNumeric")]
    public ulong? BankMinNumeric { get; set; }
}