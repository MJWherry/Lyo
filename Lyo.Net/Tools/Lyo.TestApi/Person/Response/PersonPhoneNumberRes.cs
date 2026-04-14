namespace Lyo.TestApi.Person.Response;

public sealed record PersonPhoneNumberRes(Guid Id, Guid PersonId, string Number, string? Type, DateOnly CreatedDate, DateOnly UpdatedDate);