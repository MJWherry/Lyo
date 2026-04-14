namespace Lyo.Gateway.Models;

public sealed record PersonAddressRes(
    Guid Id,
    Guid PersonId,
    string? HouseNumber,
    string? StreetPreDirection,
    string? StreetName,
    string? StreetPostDirection,
    string? StreetType,
    string? Unit,
    string? UnitType,
    string? City,
    string? State,
    string? County,
    string? Zipcode,
    string? Zipcode4,
    DateOnly CreatedDate,
    DateOnly UpdatedDate)
{
    public override string ToString() => $"{HouseNumber}";
}