namespace Lyo.TestApi.Person.Response;

/// <summary>Minimal phone number info for display when referenced by contact_phone_number_id (Lyo people schema).</summary>
public sealed record ContactPhoneNumberRes(Guid Id, string Number, string? Type);