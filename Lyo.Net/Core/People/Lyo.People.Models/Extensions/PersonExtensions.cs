using Lyo.Geolocation.Models.Addresses;
using Lyo.Geolocation.Models.Enums;
using Lyo.People.Models.Contact;
using Lyo.People.Models.Enum;
using Lyo.People.Models.Relationships;

namespace Lyo.People.Models.Extensions;

/// <summary>Extension methods for Person model</summary>
public static class PersonExtensions
{
    /// <summary>Gets the home address for the person</summary>
    public static ContactAddress? GetHomeAddress(this Person person) => person?.GetAddressByType(ContactAddressType.Home);

    /// <summary>Gets the work address for the person</summary>
    public static ContactAddress? GetWorkAddress(this Person person) => person?.GetAddressByType(ContactAddressType.Work);

    /// <summary>Gets the mobile phone number for the person</summary>
    public static ContactPhoneNumber? GetMobilePhone(this Person person) => person?.GetPhoneNumberByType(ContactPhoneType.Mobile);

    /// <summary>Gets the work phone number for the person</summary>
    public static ContactPhoneNumber? GetWorkPhone(this Person person) => person?.GetPhoneNumberByType(ContactPhoneType.Work);

    /// <summary>Gets the work email address for the person</summary>
    public static ContactEmailAddress? GetWorkEmail(this Person person) => person?.GetEmailAddressByType(ContactEmailType.Work);

    /// <summary>Gets the personal email address for the person</summary>
    public static ContactEmailAddress? GetPersonalEmail(this Person person) => person?.GetEmailAddressByType(ContactEmailType.Personal);

    /// <summary>Checks if the person has a verified email address</summary>
    public static bool HasValidEmail(this Person person) => person?.EmailAddresses?.Any(e => e.EmailAddress?.IsVerified == true && e.IsCurrent) ?? false;

    /// <summary>Checks if the person has a verified phone number</summary>
    public static bool HasValidPhone(this Person person) => person?.PhoneNumbers?.Any(p => p.PhoneNumber?.IsVerified == true && p.IsCurrent) ?? false;

    /// <summary>Gets the current employment record</summary>
    public static Employment? GetCurrentEmployment(this Person person) => person?.Employments?.FirstOrDefault(e => e.IsCurrent);

    /// <summary>Gets all active relationships</summary>
    public static IEnumerable<PersonRelationship> GetActiveRelationships(this Person person)
        => person?.Relationships?.Where(r => r.IsActive && r.IsCurrent) ?? Enumerable.Empty<PersonRelationship>();
}