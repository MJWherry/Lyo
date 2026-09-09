using System.Diagnostics;
using System.Text.Json.Serialization;
using Lyo.Common.Metadata.JsonConverters;
using Lyo.Common.Metadata.Records;

namespace Lyo.People.Models.Contact;

/// <summary>A person's profile on one social platform.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class SocialMediaProfile
{
    /// <summary>Primary key for this profile row.</summary>
    public Guid Id { get; set; }

    /// <summary>Platform (LinkedIn, X, and so on).</summary>
    [JsonConverter(typeof(SocialPlatformInfoJsonConverter))]
    public SocialPlatformInfo Platform { get; set; } = SocialPlatformInfo.Unknown;

    /// <summary>Handle on that platform.</summary>
    public string Username { get; set; } = null!;

    /// <summary>Explicit URL of the profile page.</summary>
    public string? ProfileUrl { get; set; }

    /// <summary>Stored profile URL, or one built from <see cref="Platform" /> plus <see cref="Username" />.</summary>
    public string? ResolvedProfileUrl => ProfileUrl ?? Platform.TryBuildProfileUri(Username);

    /// <summary>True when <see cref="VerifiedAt" /> is set.</summary>
    public bool IsVerified => VerifiedAt.HasValue;

    /// <summary>When the platform verified the profile.</summary>
    public DateTime? VerifiedAt { get; set; }

    /// <summary>Display name on the platform, when known.</summary>
    public string? DisplayName { get; set; }

    /// <summary>When the person attached this profile.</summary>
    public DateTime? AddedAt { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"SocialMediaProfile: id={Id}, platform={Platform}, username={Username}";
}
