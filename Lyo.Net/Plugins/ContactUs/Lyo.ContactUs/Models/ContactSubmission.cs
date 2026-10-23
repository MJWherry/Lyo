using System.Diagnostics;

namespace Lyo.ContactUs.Models;

/// <summary>A stored contact form submission.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ContactSubmission
{
    /// <summary>Id, sender, and subject. The message body is omitted.</summary>
    public override string ToString() => $"{Id} {Name} <{Email}> Subject={Subject} MessageLength={Message?.Length ?? 0}";

    /// <summary>Unique identifier for the submission.</summary>
    public Guid Id { get; init; }

    /// <summary>Sender's name.</summary>
    public string Name { get; init; } = null!;

    /// <summary>Sender's email address.</summary>
    public string Email { get; init; } = null!;

    /// <summary>Subject of the message.</summary>
    public string Subject { get; init; } = null!;

    /// <summary>Message body.</summary>
    public string Message { get; init; } = null!;

    /// <summary>Optional phone number.</summary>
    public string? Phone { get; init; }

    /// <summary>Optional company name.</summary>
    public string? Company { get; init; }

    /// <summary>UTC time the submission was stored.</summary>
    public DateTime CreatedTimestamp { get; init; }
}