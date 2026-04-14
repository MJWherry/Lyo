# Lyo.People.Models

People and person-related models for the Lyo library suite.

## Overview

This package provides domain models for representing people, their contact information, relationships, employment history, and preferences. It integrates with
`Lyo.Geolocation.Models` for address support.

## Models

### Core

- **Person** — Main person entity with demographics, contact info, addresses, and metadata
- **PersonName** — Structured name with prefix, suffix, preferred name, and formatting options
- **Employment** — Employment history with company, title, dates, and compensation
- **Identification** — ID documents (passport, driver's license, SSN, etc.)

### Contact

- **PhoneNumber** — Base phone number (E.164 format)
- **EmailAddress** — Base email address
- **ContactPhoneNumber** — Person–phone association with type (mobile, home, work)
- **ContactEmailAddress** — Person–email association with type (personal, work)
- **SocialMediaProfile** — Social platform profiles (LinkedIn, Twitter, etc.)
- **CommunicationPreferences** — Channel preferences (email, SMS, marketing opt-in)

### Preferences

- **PersonPreferences** — Contact method, timezone, language
- **PrivacyPreferences** — Data sharing and directory visibility

### Relationships

- **PersonRelationship** — Links between people (spouse, parent, employer, etc.)

## Enums

- `ContactEmailType` — Personal, Work, Other
- `ContactPhoneType` — Mobile, Home, Work, Fax, Other
- `RelationshipType` — Spouse, Parent, Child, Employer, etc.
- `IdentificationType` — Passport, DriversLicense, SSN, etc.
- `EmploymentType` — FullTime, PartTime, Contract, Freelance, etc.
- `NameFormat` — Full, Formal, Display, Initials, etc.

## Usage

```csharp
using Lyo.People.Models;
using Lyo.People.Models.Contact;
using Lyo.People.Models.Enum;
using Lyo.People.Models.Extensions;
using Lyo.People.Models.Models;

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

// Use extension methods
var homeAddress = person.GetHomeAddress();
var mobilePhone = person.GetMobilePhone();
var hasValidEmail = person.HasValidEmail();
```

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.People.Models.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

*None declared in this project file.*

### Project references

- `Lyo.Common`
- `Lyo.DateAndTime`
- `Lyo.Geolocation.Models`

## Public API (generated)

Top-level `public` types in `*.cs` (*20*). Nested types and file-scoped namespaces may omit some entries.

- `CommunicationPreferences`
- `ContactEmailAddress`
- `ContactEmailType`
- `ContactPhoneNumber`
- `ContactPhoneType`
- `EmailAddress`
- `Employment`
- `EmploymentType`
- `Identification`
- `IdentificationType`
- `NameFormat`
- `Person`
- `PersonExtensions`
- `PersonName`
- `PersonPreferences`
- `PersonRelationship`
- `PhoneNumber`
- `PrivacyPreferences`
- `RelationshipType`
- `SocialMediaProfile`

<!-- LYO_README_SYNC:END -->

