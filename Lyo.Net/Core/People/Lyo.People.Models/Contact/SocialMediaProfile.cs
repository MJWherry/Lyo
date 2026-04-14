using Lyo.Common.Enums;

namespace Lyo.People.Models.Contact;

/// <summary>Represents a person's social media profile on a platform</summary>
public class SocialMediaProfile
{
    /// <summary>Unique identifier for the profile</summary>
    public Guid Id { get; set; }

    /// <summary>Social media platform (LinkedIn, Twitter, etc.)</summary>
    public SocialPlatform Platform { get; set; }

    /// <summary>Username or handle on the platform</summary>
    public string Username { get; set; } = null!;

    /// <summary>Full URL to the profile page</summary>
    public string? ProfileUrl { get; set; }

    /// <summary>Whether the profile has been verified by the platform</summary>
    public bool IsVerified => VerifiedAt.HasValue;

    /// <summary>Date and time when the profile was verified</summary>
    public DateTime? VerifiedAt { get; set; }

    /// <summary>Optional display name on the platform</summary>
    public string? DisplayName { get; set; }

    /// <summary>When the person added this profile</summary>
    public DateTime? AddedAt { get; set; }
}