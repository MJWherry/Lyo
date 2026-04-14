namespace Lyo.TestApi.Person.Response;

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
    public string GetFormattedStreet()
        => $"{(string.IsNullOrEmpty(HouseNumber) ? "" : $"{HouseNumber} ")}" + $"{(string.IsNullOrEmpty(StreetPreDirection) ? "" : $"{StreetPreDirection} ")}" +
            $"{(string.IsNullOrEmpty(StreetName) ? "" : $"{StreetName} ")}" + $"{(string.IsNullOrEmpty(StreetType) ? "" : $"{StreetType}")}" + $"{(string.IsNullOrEmpty(Unit) ? ""
                : string.IsNullOrEmpty(UnitType) ? $" Apt {Unit}" : $" {UnitType} {Unit}")}";
}