namespace Lyo.TestApi.Person.Response;

public sealed record PersonEmailAddressRes(Guid Id, Guid PersonId, string Address);