# Lyo.People.Models

People-domain records: `Person`, contact, employment, identification, and relationships.

**Archetype A (Lyo domain).** Vendor ingest (e.g. [`Lyo.Endato.Client`](../../../Integration/Endato/Lyo.Endato.Client/README.md)) is Archetype C. Map into `people.*` in the host. See [package layout](../../../docs/package-layout.md).

## Examples

### How to use it

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

Demographics, contact info, addresses, employment, relationships, and preferences live on `Person`. Addresses reuse
`Lyo.Geolocation.Models`. Internal rows sit in `people.*` with parallel `{entity}_source` link tables ([`EntitySourceRecord`](../../EntityReference/Lyo.EntityReference.Models/EntitySourceRecord.cs) / [`PeopleSourceTypes`](PeopleSourceTypes.cs)): `source_entity_*` plus `imported_at` (owner via parent FK). Aggregates implement `IEntitySourceDerived` (`Sources`, optional `LocallyModifiedAt`). Enriched locations may also be stored in [`geolocation.address`](../Geolocation/Lyo.Geolocation.Postgres/README.md). Cross-store links use `EntityRef` on source rows (e.g. `GeolocationAddress`), not cross-schema FKs.

## Core types

- **Person.** Contact info, demographics, addresses, and metadata.
- **PersonName.** Preferred name, prefix, suffix, and formatting options.
- **Employment.** Title, company, dates, and compensation.
- **Identification.** Driver's license, passport, SSN, and similar documents.

## Contact types

- **PhoneNumber.** Base phone number (E.164 format).
- **EmailAddress.** Base email address.
- **ContactPhoneNumber.** Person-phone link with type (home, mobile, work).
- **ContactEmailAddress.** Person-email link with type (work, personal).
- **SocialMediaProfile.** Profiles on social platforms (Twitter, LinkedIn, and similar).
- **CommunicationPreferences.** Channel preferences (SMS, email, marketing opt-in).

## Preference types

- **PersonPreferences.** Timezone, contact method, language.
- **PrivacyPreferences.** Directory visibility and data sharing.

## Relationship types

- **PersonRelationship.** Links between people (parent, spouse, employer, and similar).

## Enums

- **ContactEmailType.** Work, Personal, Other.
- **ContactPhoneType.** Home, Mobile, Work, Fax, Other.
- **RelationshipType.** Parent, Spouse, Child, Employer, and similar.
- **IdentificationType.** DriversLicense, Passport, SSN, and similar.
- **EmploymentType.** PartTime, FullTime, Contract, Freelance, and similar.
- **NameFormat.** Formal, Full, Display, Initials, and similar.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.DateAndTime` (direct, lyo)
- `Lyo.EntityReference.Models` (direct, lyo)
- `Lyo.Geolocation.Models` (direct, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)