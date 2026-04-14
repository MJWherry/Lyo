namespace Lyo.Gateway.Models;

public sealed class PersonPhoneNumberReq
{
    public string Number { get; set; } = string.Empty;

    public string Type { get; set; } = "Other";

    public DateOnly CreatedDate { get; set; }

    public DateOnly UpdatedDate { get; set; }
}