namespace Lyo.Portfolio.Api.Person.Request;

public class PersonPhoneNumberReq
{
    public string Number { get; set; } = null!;

    public string Type { get; set; } = null!;

    public DateOnly CreatedDate { get; set; }

    public DateOnly UpdatedDate { get; set; }
}