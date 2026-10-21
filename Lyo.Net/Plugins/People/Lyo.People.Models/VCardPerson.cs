using Lyo.Common.Core.Enums;
using Lyo.Exceptions;
using Lyo.Geolocation.Models.Addresses;

namespace Lyo.People.Models;

/// <summary>Maps a vCard onto <see cref="Person" /> contact collections. The first telephone, email, and address is primary when the card does not mark one.</summary>
public static class VCardPerson
{
    /// <summary>Builds a person from the card. Street, locality, region, postal code, and a known country code are copied. Other ADR components are dropped.</summary>
    public static Person ToPerson(VCardCard card)
    {
        ArgumentHelpers.ThrowIfNull(card);
        var person = new Person {
            Name = new() { FirstName = card.GivenName ?? "", LastName = card.FamilyName ?? "" }, CurrentCompany = card.Organization, CurrentJobTitle = card.Title
        };

        for (var i = 0; i < card.Telephones.Count; i++) {
            person.PhoneNumbers.Add(new() { IsPrimary = i == 0, PhoneNumber = new() { Number = card.Telephones[i] } });
        }

        for (var i = 0; i < card.Emails.Count; i++) {
            person.EmailAddresses.Add(new() { IsPrimary = i == 0, EmailAddress = new() { Email = card.Emails[i] } });
        }

        for (var i = 0; i < card.Addresses.Count; i++) {
            person.Addresses.Add(new() { IsPrimary = i == 0, Address = ToAddress(card.Addresses[i]) });
        }

        return person;
    }

    /// <summary>Writes the person's current phones, emails, and addresses, including ones that are not primary.</summary>
    public static VCardCard ToCard(Person person)
    {
        ArgumentHelpers.ThrowIfNull(person);
        var card = new VCardCard {
            FormattedName = person.Name?.DisplayName,
            GivenName = person.Name?.FirstName,
            FamilyName = person.Name?.LastName,
            Organization = person.CurrentCompany,
            Title = person.CurrentJobTitle
        };

        foreach (var phone in person.PhoneNumbers) {
            if (phone.IsCurrent && phone.PhoneNumber?.Number is { Length: > 0 } number)
                card.Telephones.Add(number);
        }

        foreach (var email in person.EmailAddresses) {
            if (email.IsCurrent && email.EmailAddress?.Email is { Length: > 0 } address)
                card.Emails.Add(address);
        }

        foreach (var link in person.Addresses) {
            if (link.IsCurrent && link.Address is not null)
                card.Addresses.Add(FromAddress(link.Address));
        }

        return card;
    }

    private static Address ToAddress(VCardAddress source)
    {
        var address = new Address {
            StreetAddress = source.Street,
            City = source.Locality,
            State = source.Region,
            PostalCode = source.PostalCode
        };

        if (source.Country is not null && System.Enum.TryParse<CountryCode>(source.Country, true, out var country))
            address.CountryCode = country;

        return address;
    }

    private static VCardAddress FromAddress(Address source)
        => new() {
            Street = source.StreetAddress,
            Locality = source.City,
            Region = source.State,
            PostalCode = source.PostalCode,
            Country = source.CountryCode == CountryCode.UU ? null : source.CountryCode.ToString()
        };
}