using System.Diagnostics;

namespace Lyo.Gateway.Models;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record PersonRes(
    Guid Id,
    Guid? EndatoPersonId,
    string? Prefix,
    string? FirstName,
    string? MiddleName,
    string? LastName,
    string? Suffix,
    string Source,
    IReadOnlyList<PersonAddressRes>? Addresses,
    IReadOnlyList<PersonEmailAddressRes>? EmailAddresses,
    IReadOnlyList<PersonPhoneNumberRes>? PhoneNumbers)
{
    public PersonAddressRes? MostRecentAddress => Addresses?.OrderByDescending(a => a.UpdatedDate).FirstOrDefault();

    public string FullName => $"{FirstName}{(string.IsNullOrEmpty(MiddleName) ? " " : $" {MiddleName} ")}{LastName}";

    public override string ToString()
        => $"PersonRes: id={Id}, name={FullName}, source={Source}, addresses={Addresses?.Count ?? 0}";
}
