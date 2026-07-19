using System.Diagnostics;

namespace Lyo.Gateway.Models;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record PersonPhoneNumberRes
{
    public Guid Id { get; init; }

    public Guid PersonId { get; init; }

    public string Number { get; init; } = string.Empty;

    public string? CountryCode { get; init; }

    public string? CountryCodeString { get; init; }

    public string? TechnologyType { get; init; }

    public DateTime? VerifiedAt { get; init; }

    public string? Label { get; init; }

    public string? Type { get; init; }

    public string? SourceEntityType { get; init; }

    public string? SourceEntityId { get; init; }

    public DateTime? ImportedAt { get; init; }

    public DateTime CreatedTimestamp { get; init; }

    public DateTime? UpdatedTimestamp { get; init; }

    public override string ToString() => $"PersonPhoneNumberRes: {Number}, type={Type}, person={PersonId}";
}
