using System.Diagnostics;

namespace Lyo.TestGateway.Models;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record EndatoPsPersonRes
{
    public Guid Id { get; init; }

    public Guid QueryId { get; init; }

    public string? Prefix { get; init; }

    public string? FirstName { get; init; }

    public string? MiddleName { get; init; }

    public string? LastName { get; init; }

    public string? Suffix { get; init; }

    public DateOnly? DateOfBirth { get; init; }

    public IReadOnlyList<EndatoPsAddressRes> Addresses { get; init; } = [];

    public IReadOnlyList<EndatoPsEmailAddressRes> EmailAddresses { get; init; } = [];

    public IReadOnlyList<EndatoPsPhoneNumberRes> PhoneNumbers { get; init; } = [];

    public string FullName => string.Join(" ", new[] { Prefix, FirstName, MiddleName, LastName, Suffix }.Where(static s => !string.IsNullOrWhiteSpace(s)));

    public override string ToString() => $"EndatoPsPersonRes: id={Id}, name={FullName}, query={QueryId}";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record EndatoPsAddressRes
{
    public Guid Id { get; init; }

    public Guid EndatoPersonId { get; init; }

    public bool IsDeliverable { get; init; }

    public bool IsMergedAddress { get; init; }

    public bool IsPublic { get; init; }

    public string? AddressHash { get; init; }

    public string? HouseNumber { get; init; }

    public string? StreetPreDirection { get; init; }

    public string? StreetName { get; init; }

    public string? StreetPostDirection { get; init; }

    public string? StreetType { get; init; }

    public string? Unit { get; init; }

    public string? UnitType { get; init; }

    public string? City { get; init; }

    public string? State { get; init; }

    public string? County { get; init; }

    public string? Zipcode { get; init; }

    public string? Zipcode4 { get; init; }

    public string? FullAddress { get; init; }

    public PersonPointRes? Coordinates { get; init; }

    public string[]? PhoneNumbers { get; init; }

    public int OrderNumber { get; init; }

    public DateOnly FirstReportedDate { get; init; }

    public DateOnly LastReportedDate { get; init; }

    public DateOnly PublicFirstSeenDate { get; init; }

    public DateOnly TotalFirstSeenDate { get; init; }

    public override string ToString() => $"EndatoPsAddressRes: {FullAddress ?? $"{City}, {State}"}";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record EndatoPsPhoneNumberRes
{
    public Guid Id { get; init; }

    public Guid EndatoPersonId { get; init; }

    public string Number { get; init; } = string.Empty;

    public string? Company { get; init; }

    public string? Location { get; init; }

    public string? Type { get; init; }

    public bool IsConnected { get; init; }

    public bool IsPublic { get; init; }

    public PersonPointRes? Coordinates { get; init; }

    public int OrderNumber { get; init; }

    public DateOnly FirstReportedDate { get; init; }

    public DateOnly LastReportedDate { get; init; }

    public DateOnly PublicFirstSeenDate { get; init; }

    public DateOnly TotalFirstSeenDate { get; init; }

    public override string ToString() => $"EndatoPsPhoneNumberRes: {Number}";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record EndatoPsEmailAddressRes
{
    public Guid Id { get; init; }

    public Guid EndatoPersonId { get; init; }

    public string Address { get; init; } = string.Empty;

    public int OrderNumber { get; init; }

    public bool IsPremium { get; init; }

    public bool NonBusiness { get; init; }

    public override string ToString() => $"EndatoPsEmailAddressRes: {Address}";
}
