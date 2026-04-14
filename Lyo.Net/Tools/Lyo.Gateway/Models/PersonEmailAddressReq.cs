namespace Lyo.Gateway.Models;

public sealed class PersonEmailAddressReq
{
    public string Address { get; set; } = string.Empty;

    public DateOnly CreatedDate { get; set; }

    public DateOnly UpdatedDate { get; set; }
}