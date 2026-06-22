using Lyo.Endato.Client.Models.Enrichment.Request;
using Lyo.Endato.Client.Models.Person.Request;

namespace Lyo.Endato.Client.Tests;

public class PersonQueryBuilderTests
{
    [Fact]
    public void Build_IncludesFluentFields()
    {
        var query = PersonQueryBuilder.Create("Jane", "Doe", age: 42)
            .WithMiddleName("Q")
            .WithPhone("5125550100")
            .WithEmail("jane@example.com")
            .WithPage(2)
            .WithResultsPerPage(25)
            .AddAddress("123 Main St", "Austin, TX", "Travis")
            .AddAka("Janet", "Doe")
            .WithTahoeIds("abc-123")
            .WithIncludes("Addresses", "PhoneNumbers")
            .Build();

        Assert.Equal("Jane", query.FirstName);
        Assert.Equal("Q", query.MiddleName);
        Assert.Equal("Doe", query.LastName);
        Assert.Equal(42, query.Age);
        Assert.Equal("5125550100", query.Phone);
        Assert.Equal("jane@example.com", query.Email);
        Assert.Equal(2, query.Page);
        Assert.Equal(25, query.ResultsPerPage);
        Assert.Single(query.Addresses!);
        Assert.Equal("123 Main St", query.Addresses![0].AddressLine1);
        Assert.Single(query.Akas!);
        Assert.Equal("Janet", query.Akas![0].FirstName);
        Assert.Single(query.TahoeIds!);
        Assert.Equal(2, query.Includes!.Count);
    }
}
