namespace Lyo.TestApi.Person.Response;

public sealed record PersonRes(
    Guid Id,
    Guid? EndatoPersonId,
    string? Prefix,
    string FirstName,
    string? MiddleName,
    string LastName,
    string? Suffix,
    string Source,
    IReadOnlyList<PersonAddressRes>? Addresses,
    IReadOnlyList<PersonEmailAddressRes>? EmailAddresses,
    IReadOnlyList<PersonPhoneNumberRes>? PhoneNumbers)
{
    public PersonAddressRes? MostRecentAddress => Addresses?.OrderByDescending(a => a.UpdatedDate).FirstOrDefault();

    public string FullName => $"{FirstName}{(string.IsNullOrEmpty(MiddleName) ? " " : $" {MiddleName} ")}{LastName}";
}