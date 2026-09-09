namespace Lyo.TestApi.Person.Request;

public class PersonEmailAddressReq
{
    public string Address { get; set; } = null!;

    public DateOnly CreatedDate { get; set; }

    public DateOnly UpdatedDate { get; set; }
}