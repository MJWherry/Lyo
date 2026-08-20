# Lyo.People.Models

`Person`, contact, employment, identification, and relationship records for the people domain.

**Archetype A (Lyo domain).** Vendor ingest (e.g. [`Lyo.Endato.Client`](../../../Integration/Endato/Lyo.Endato.Client/README.md)) is Archetype C. Map into `people.*` in the host. See [package layout](../../../docs/package-layout.md).

## Examples

### Usage

```csharp
using Lyo.People.Models;
using Lyo.People.Models.Contact;
using Lyo.People.Models.Enum;
using Lyo.People.Models.Extensions;

var person = new Person
{
    Id = Guid.NewGuid(),
    Name = new PersonName
    {
        FirstName = "Jane",
        LastName = "Smith",
        PreferredName = "Janey"
    },
    EmailAddresses =
    {
        new ContactEmailAddress
        {
            Type = ContactEmailType.Personal,
            IsPrimary = true,
            EmailAddress = new EmailAddress { Email = "jane@example.com" }
        }
    }
};

// Person instance helpers (primary / by-type / current selectors)
var primaryAddr = person.GetPrimaryAddress();
var workAddr = person.GetAddressByType(ContactAddressType.Work);
var primaryPhone = person.GetPrimaryPhoneNumber();
var mobilePhone = person.GetPhoneNumberByType(ContactPhoneType.Mobile);
var primaryEmail = person.GetPrimaryEmailAddress();

// PersonExtensions (typed convenience wrappers + verification checks)
var homeAddress = person.GetHomeAddress();
var workEmail = person.GetWorkEmail();
var personalMail = person.GetPersonalEmail();
var hasValidMail = person.HasValidEmail(); // true when any current email has VerifiedAt set
var hasValidTel = person.HasValidPhone(); // true when any current phone has VerifiedAt set
var currentJob = person.GetCurrentEmployment();
var activeRels = person.GetActiveRelationships();

// PersonName formatting
var initials = person.Name.GetInitials();
var formatted = person.Name.GetFormattedName(NameFormat.Formal);
var display = person.Name.DisplayName; // PreferredName ?? FullName
```

## Overview

`Person` holds demographics, contact info, addresses, employment, relationships, and preferences. Addresses reuse
`Lyo.Geolocation.Models`. Internal rows live in `people.*` with parallel `{entity}_source` link tables ([`EntitySourceRecord`](../../EntityReference/Lyo.EntityReference.Models/EntitySourceRecord.cs) / [`PeopleSourceTypes`](PeopleSourceTypes.cs)): `source_entity_*` plus `imported_at` (owner via parent FK). Aggregates implement `IEntitySourceDerived` (`Sources`, optional `LocallyModifiedAt`). Enriched locations may also be stored in [`geolocation.address`](../Geolocation/Lyo.Geolocation.Postgres/README.md). Link across stores via `EntityRef` on source rows (e.g. `GeolocationAddress`), not cross-schema FKs.

## Core models

- **Person.** Demographics, contact info, addresses, and metadata.
- **PersonName.** Prefix, suffix, preferred name, and formatting options.
- **Employment.** Company, title, dates, and compensation.
- **Identification.** Passport, driver's license, SSN, and similar documents.

## Contact models

- **PhoneNumber.** Base phone number (E.164 format).
- **EmailAddress.** Base email address.
- **ContactPhoneNumber.** Person-phone association with type (mobile, home, work).
- **ContactEmailAddress.** Person-email association with type (personal, work).
- **SocialMediaProfile.** Social platform profiles (LinkedIn, Twitter, and similar).
- **CommunicationPreferences.** Channel preferences (email, SMS, marketing opt-in).

## Preference models

- **PersonPreferences.** Contact method, timezone, language.
- **PrivacyPreferences.** Data sharing and directory visibility.

## Relationship models

- **PersonRelationship.** Links between people (spouse, parent, employer, and similar).

## Enums

- **ContactEmailType.** Personal, Work, Other.
- **ContactPhoneType.** Mobile, Home, Work, Fax, Other.
- **RelationshipType.** Spouse, Parent, Child, Employer, and similar.
- **IdentificationType.** Passport, DriversLicense, SSN, and similar.
- **EmploymentType.** FullTime, PartTime, Contract, Freelance, and similar.
- **NameFormat.** Full, Formal, Display, Initials, and similar.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` (direct, lyo)
- `Lyo.DateAndTime` (direct, lyo)
- `Lyo.EntityReference.Models` (direct, lyo)
- `Lyo.Geolocation.Models` (direct, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)