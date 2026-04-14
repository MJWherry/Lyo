using System.Diagnostics;
using System.Text.Json.Serialization;
using Lyo.Common.JsonConverters;
using Lyo.Common.Records;
using Lyo.Tts.Models;

namespace Lyo.Tts.AwsPolly;

/// <summary>Configuration options for AWS Polly TTS service.</summary>
/// <remarks>
/// <para>This class is not thread-safe. Options should be configured during application startup and not modified after service registration.</para>
/// <para>All properties from the base <see cref="TtsServiceOptions" /> class are also available.</para>
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class AwsPollyOptions : TtsServiceOptions
{
    /// <summary>The default configuration section name for AwsPollyOptions.</summary>
    public const string SectionName = "AwsPollyOptions";

    /// <summary>Gets or sets the AWS access key ID (required if not using IAM roles).</summary>
    /// <remarks>Keep this secure and never commit it to source control. Consider using IAM roles instead.</remarks>
    public string? AccessKeyId { get; set; }

    /// <summary>Gets or sets the AWS secret access key (required if not using IAM roles).</summary>
    /// <remarks>Keep this secure and never commit it to source control. Consider using IAM roles instead.</remarks>
    public string? SecretAccessKey { get; set; }

    /// <summary>Gets or sets the AWS region (e.g., "us-east-1", "eu-west-1").</summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>Gets or sets the AWS service URL (optional, for local testing or custom endpoints).</summary>
    public string? ServiceUrl { get; set; }

    /// <summary>Gets or sets the default AWS Polly voice ID as an enum.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AwsPollyVoiceId DefaultVoiceIdEnum => Enum.TryParse<AwsPollyVoiceId>(DefaultVoiceId, out var id) ? id : AwsPollyVoiceId.Amy;

    /// <summary>Gets or sets the default language code (e.g., "en-US").</summary>
    [JsonConverter(typeof(NullableLanguageCodeInfoJsonConverter))]
    public LanguageCodeInfo? DefaultLanguageCode { get; set; }

    /// <summary>Returns a string representation of the options (privacy-safe, does not include credentials).</summary>
    /// <returns>A string containing the Region.</returns>
    public override string ToString()
        => $"Region={Region} DefaultVoiceId={DefaultVoiceIdEnum} DefaultLanguageCode={DefaultLanguageCode?.Bcp47 ?? DefaultLanguageCode?.Iso6391 ?? DefaultLanguageCode?.Iso6393 ?? DefaultLanguageCode?.Name}";
}