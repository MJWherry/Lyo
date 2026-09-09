using Lyo.Geolocation.Models.Addresses;
using Lyo.Geolocation.Models.Enums;
using Lyo.People.Models.Contact;
using Lyo.People.Models.Enum;
using Lyo.People.Models.Relationships;

namespace Lyo.People.Models.Extensions;

/// <summary>Convenience lookups on <see cref="Person" />.</summary>
public static class PersonExtensions
{
    extension(Person? person)
    {
        /// <summary>Home address, when present.</summary>
        public ContactAddress? GetHomeAddress() => person?.GetAddressByType(ContactAddressType.Home);

        /// <summary>Work address, when present.</summary>
        public ContactAddress? GetWorkAddress() => person?.GetAddressByType(ContactAddressType.Work);

        /// <summary>Mobile phone, when present.</summary>
        public ContactPhoneNumber? GetMobilePhone() => person?.GetPhoneNumberByType(ContactPhoneType.Mobile);

        /// <summary>Work phone, when present.</summary>
        public ContactPhoneNumber? GetWorkPhone() => person?.GetPhoneNumberByType(ContactPhoneType.Work);

        /// <summary>Work email, when present.</summary>
        public ContactEmailAddress? GetWorkEmail() => person?.GetEmailAddressByType(ContactEmailType.Work);

        /// <summary>Personal email, when present.</summary>
        public ContactEmailAddress? GetPersonalEmail() => person?.GetEmailAddressByType(ContactEmailType.Personal);

        /// <summary>True when a current, verified email exists.</summary>
        public bool HasValidEmail() => person?.EmailAddresses.Any(e => e.EmailAddress?.IsVerified == true && e.IsCurrent) ?? false;

        /// <summary>True when a current, verified phone exists.</summary>
        public bool HasValidPhone() => person?.PhoneNumbers.Any(p => p.PhoneNumber?.IsVerified == true && p.IsCurrent) ?? false;

        /// <summary>Current employment row, when present.</summary>
        public Employment? GetCurrentEmployment() => person?.Employments.FirstOrDefault(e => e.IsCurrent);

        /// <summary>Relationships that are both active and current.</summary>
        public IEnumerable<PersonRelationship> GetActiveRelationships() => person?.Relationships.Where(r => r.IsActive && r.IsCurrent) ?? [];
    }
}
