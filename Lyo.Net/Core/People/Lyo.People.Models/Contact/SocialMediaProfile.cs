using System.Diagnostics;
using System.Text.Json.Serialization;
using Lyo.Common.JsonConverters;
using Lyo.Common.Records;

namespace Lyo.People.Models.Contact;

/// <summary>Represents a person's social media profile on a platform</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class SocialMediaProfile
{
    /// <summary>Unique identifier for the profile</summary>
    public Guid Id { get; set; }

    /// <summary>Social media platform (LinkedIn, X, etc.)</summary>
    [JsonConverter(typeof(SocialPlatformInfoJsonConverter))]
    public SocialPlatformInfo Platform { get; set; } = SocialPlatformInfo.Unknown;

    /// <summary>Username or handle on the platform</summary>
    public string Username { get; set; } = null!;

    /// <summary>Full URL to the profile page</summary>
    public string? ProfileUrl { get; set; }

    /// <summary>Explicit profile URL, or one derived from <see cref="Platform" /> + <see cref="Username" />.</summary>
    public string? ResolvedProfileUrl => ProfileUrl ?? Platform.TryBuildProfileUri(Username);

    /// <summary>Whether the profile has been verified by the platform</summary>
    public bool IsVerified => VerifiedAt.HasValue;

    /// <summary>Date and time when the profile was verified</summary>
    public DateTime? VerifiedAt { get; set; }

    /// <summary>Optional display name on the platform</summary>
    public string? DisplayName { get; set; }

    /// <summary>When the person added this profile</summary>
    public DateTime? AddedAt { get; set; }

    /// <inheritdoc />
    public override string ToString()
        => $"SocialMediaProfile: id={Id}, platform={Platform}, username={Username}";
}
