using NpgsqlTypes;

namespace Lyo.TestApi.Person.Response;

public sealed record PersonAddressRes
{
    public Guid Id { get; init; }

    public Guid PersonId { get; init; }

    public string? HouseNumber { get; init; }

    public string? StreetPreDirection { get; init; }

    public string? StreetName { get; init; }

    public string? StreetPostDirection { get; init; }

    public string? StreetType { get; init; }

    public string? Unit { get; init; }

    public string? UnitType { get; init; }

    public string? StreetAddress { get; init; }

    public string? StreetAddressLine2 { get; init; }

    public string? City { get; init; }

    public string? State { get; init; }

    public string? County { get; init; }

    public string? Zipcode { get; init; }

    public string? Zipcode4 { get; init; }

    public string? PostalCode { get; init; }

    public string CountryCode { get; init; } = "US";

    public string? FullAddress { get; init; }

    public NpgsqlPoint? Coordinates { get; init; }

    public string? SourceEntityType { get; init; }

    public string? SourceEntityId { get; init; }

    public DateTime? ImportedAt { get; init; }

    public DateTime CreatedTimestamp { get; init; }

    public DateTime? UpdatedTimestamp { get; init; }

    public string GetFormattedStreet()
        => $"{(string.IsNullOrEmpty(HouseNumber) ? "" : $"{HouseNumber} ")}" + $"{(string.IsNullOrEmpty(StreetPreDirection) ? "" : $"{StreetPreDirection} ")}" +
            $"{(string.IsNullOrEmpty(StreetName) ? "" : $"{StreetName} ")}" + $"{(string.IsNullOrEmpty(StreetType) ? "" : $"{StreetType}")}" + $"{(string.IsNullOrEmpty(Unit) ? ""
                : string.IsNullOrEmpty(UnitType) ? $" Apt {Unit}" : $" {UnitType} {Unit}")}";
}