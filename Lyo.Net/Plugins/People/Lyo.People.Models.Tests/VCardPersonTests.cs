using Lyo.Common.Core.Enums;

namespace Lyo.People.Models.Tests;

public class VCardPersonTests
{
    [Fact]
    public void Map_Adr_FillsExistingAddressFields()
    {
        var card = new VCardCard { GivenName = "Ada", FamilyName = "Lovelace" };
        card.Addresses.Add(
            new() {
                Street = "10 Downing",
                Locality = "London",
                Region = "Greater London",
                PostalCode = "SW1A",
                Country = "US"
            });

        var person = VCardPerson.ToPerson(card);
        var address = person.Addresses.Single().Address!;
        Assert.Equal("10 Downing", address.StreetAddress);
        Assert.Equal("London", address.City);
        Assert.Equal("Greater London", address.State);
        Assert.Equal("SW1A", address.PostalCode);
        Assert.Equal(CountryCode.US, address.CountryCode);
        Assert.True(person.Addresses.Single().IsPrimary);
    }

    [Fact]
    public void Write_IncludesNonPrimaryEmail()
    {
        var person = new Person { Name = new() { FirstName = "Ada", LastName = "Lovelace" } };
        person.EmailAddresses.Add(new() { IsPrimary = true, EmailAddress = new() { Email = "a@x.com" } });
        person.EmailAddresses.Add(new() { IsPrimary = false, EmailAddress = new() { Email = "b@x.com" } });
        var text = new VCardDocument().Write(VCardPerson.ToCard(person));
        Assert.Contains("a@x.com", text);
        Assert.Contains("b@x.com", text);
    }
}