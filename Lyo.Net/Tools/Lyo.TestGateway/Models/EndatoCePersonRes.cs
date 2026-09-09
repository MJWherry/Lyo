using System.Diagnostics;

namespace Lyo.TestGateway.Models;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record EndatoCePersonRes
{
    public Guid Id { get; init; }

    public string FirstName { get; init; } = string.Empty;

    public string? MiddleName { get; init; }

    public string LastName { get; init; } = string.Empty;

    public DateOnly? DateOfBirth { get; init; }

    public IReadOnlyList<EndatoCeAddressRes> Addresses { get; init; } = [];

    public IReadOnlyList<EndatoCePhoneNumberRes> PhoneNumbers { get; init; } = [];

    public IReadOnlyList<EndatoCeEmailAddressRes> EmailAddresses { get; init; } = [];

    public string FullName => string.Join(" ", new[] { FirstName, MiddleName, LastName }.Where(static s => !string.IsNullOrWhiteSpace(s)));

    public override string ToString() => $"EndatoCePersonRes: id={Id}, name={FullName}";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record EndatoCeAddressRes
{
    public Guid Id { get; init; }

    public Guid EndatoCePersonId { get; init; }

    public string Street { get; init; } = string.Empty;

    public string? Unit { get; init; }

    public string? City { get; init; }

    public string? State { get; init; }

    public string? Zipcode { get; init; }

    public DateOnly FirstReportedDate { get; init; }

    public DateOnly LastReportedDate { get; init; }

    public override string ToString() => $"EndatoCeAddressRes: {Street}, {City}";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record EndatoCePhoneNumberRes
{
    public Guid Id { get; init; }

    public Guid EndatoCePersonId { get; init; }

    public string Number { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public bool IsConnected { get; init; }

    public DateOnly FirstReportedDate { get; init; }

    public DateOnly LastReportedDate { get; init; }

    public override string ToString() => $"EndatoCePhoneNumberRes: {Number}";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record EndatoCeEmailAddressRes
{
    public Guid Id { get; init; }

    public Guid EndatoCePersonId { get; init; }

    public string Email { get; init; } = string.Empty;

    public bool IsValidated { get; init; }

    public bool IsBusiness { get; init; }

    public override string ToString() => $"EndatoCeEmailAddressRes: {Email}";
}
