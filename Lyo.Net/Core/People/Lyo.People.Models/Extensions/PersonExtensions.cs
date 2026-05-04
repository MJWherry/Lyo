using Lyo.Geolocation.Models.Addresses;
using Lyo.Geolocation.Models.Enums;
using Lyo.People.Models.Contact;
using Lyo.People.Models.Enum;
using Lyo.People.Models.Relationships;

namespace Lyo.People.Models.Extensions;

/// <summary>Extension methods for Person model</summary>
public static class PersonExtensions
{
    extension(Person? person)
    {
        /// <summary>Gets the home address for the person</summary>
        public ContactAddress? GetHomeAddress() => person?.GetAddressByType(ContactAddressType.Home);

        /// <summary>Gets the work address for the person</summary>
        public ContactAddress? GetWorkAddress() => person?.GetAddressByType(ContactAddressType.Work);

        /// <summary>Gets the mobile phone number for the person</summary>
        public ContactPhoneNumber? GetMobilePhone() => person?.GetPhoneNumberByType(ContactPhoneType.Mobile);

        /// <summary>Gets the work phone number for the person</summary>
        public ContactPhoneNumber? GetWorkPhone() => person?.GetPhoneNumberByType(ContactPhoneType.Work);

        /// <summary>Gets the work email address for the person</summary>
        public ContactEmailAddress? GetWorkEmail() => person?.GetEmailAddressByType(ContactEmailType.Work);

        /// <summary>Gets the personal email address for the person</summary>
        public ContactEmailAddress? GetPersonalEmail() => person?.GetEmailAddressByType(ContactEmailType.Personal);

        /// <summary>Checks if the person has a verified email address</summary>
        public bool HasValidEmail() => person?.EmailAddresses.Any(e => e.EmailAddress?.IsVerified == true && e.IsCurrent) ?? false;

        /// <summary>Checks if the person has a verified phone number</summary>
        public bool HasValidPhone() => person?.PhoneNumbers.Any(p => p.PhoneNumber?.IsVerified == true && p.IsCurrent) ?? false;

        /// <summary>Gets the current employment record</summary>
        public Employment? GetCurrentEmployment() => person?.Employments.FirstOrDefault(e => e.IsCurrent);

        /// <summary>Gets all active relationships</summary>
        public IEnumerable<PersonRelationship> GetActiveRelationships() => person?.Relationships.Where(r => r.IsActive && r.IsCurrent) ?? [];
    }
}