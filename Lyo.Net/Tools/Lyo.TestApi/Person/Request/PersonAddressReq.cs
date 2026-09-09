namespace Lyo.TestApi.Person.Request;

public class PersonAddressReq
{
    public string? HouseNumber { get; set; }

    public string? StreetPreDirection { get; set; }

    public string? StreetName { get; set; }

    public string? StreetPostDirection { get; set; }

    public string? StreetType { get; set; }

    public string? Unit { get; set; }

    public string? UnitType { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? County { get; set; }

    public string? Zipcode { get; set; }

    public string? Zipcode4 { get; set; }

    public DateOnly CreatedDate { get; set; }

    public DateOnly UpdatedDate { get; set; }
}