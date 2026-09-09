using System.Diagnostics;
using System.Text.Json.Serialization;
using Lyo.Common.Metadata.JsonConverters;
using Lyo.Common.Metadata.Records;
using Lyo.Tts.Models;

namespace Lyo.Tts.AwsPolly;

/// <summary>Settings for the AWS Polly TTS provider.</summary>
/// <remarks>
/// <para>Not thread-safe. Configure during startup and leave the instance alone after it is registered.</para>
/// <para>Inherited members from <see cref="TtsServiceOptions" /> remain available.</para>
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class AwsPollyOptions : TtsServiceOptions
{
    /// <summary>Default config section used for AwsPollyOptions.</summary>
    public const string SectionName = "AwsPollyOptions";

    /// <summary>AWS access key ID (needed unless IAM roles supply credentials).</summary>
    /// <remarks>Treat as secret; do not commit it. Prefer IAM roles when possible.</remarks>
    public string? AccessKeyId { get; set; }

    /// <summary>AWS secret access key (needed unless IAM roles supply credentials).</summary>
    /// <remarks>Treat as secret; do not commit it. Prefer IAM roles when possible.</remarks>
    public string? SecretAccessKey { get; set; }

    /// <summary>AWS region, such as "us-east-1" or "eu-west-1".</summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>Optional AWS service URL for local tests or a custom endpoint.</summary>
    public string? ServiceUrl { get; set; }

    /// <summary>Default voice as <see cref="AwsPollyVoiceId" />, parsed from <see cref="TtsServiceOptions.DefaultVoiceId" /> or <see cref="AwsPollyVoiceId.Amy" /> when unset.</summary>
    public AwsPollyVoiceId DefaultVoiceIdEnum => Enum.TryParse<AwsPollyVoiceId>(DefaultVoiceId, out var id) ? id : AwsPollyVoiceId.Amy;

    /// <summary>Default language (for example "en-US").</summary>
    [JsonConverter(typeof(NullableLanguageCodeInfoJsonConverter))]
    public LanguageCodeInfo? DefaultLanguageCode { get; set; }

    /// <summary>Privacy-safe string form of the options (credentials omitted).</summary>
    /// <returns>A string that includes the Region.</returns>
    public override string ToString()
        => $"Region={Region} DefaultVoiceId={DefaultVoiceIdEnum} DefaultLanguageCode={DefaultLanguageCode?.Bcp47 ?? DefaultLanguageCode?.Iso6391 ?? DefaultLanguageCode?.Iso6393 ?? DefaultLanguageCode?.Name}";
}