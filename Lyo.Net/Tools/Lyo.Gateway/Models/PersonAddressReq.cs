using System.Diagnostics;

namespace Lyo.Gateway.Models;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class PersonAddressReq
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

    public override string ToString() => $"PersonAddressReq: city={City}, state={State}, zip={Zipcode}";
}