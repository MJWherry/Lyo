using Lyo.Endato.Client.Models.Enrichment.Request;

namespace Lyo.Endato.Client.Tests;

public class EnrichmentQueryBuilderTests
{
    [Fact]
    public void Build_RequiresAtLeastTwoIdentifiers()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            EnrichmentQueryBuilder.New()
                .WithName("John", "Smith")
                .Build());

        Assert.Contains("at least two", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_AllowsNameAndPhone()
    {
        var query = EnrichmentQueryBuilder.Create("John", "Smith")
            .WithPhone("5125550100")
            .Build();

        Assert.Equal("John", query.FirstName);
        Assert.Equal("Smith", query.LastName);
        Assert.Equal("5125550100", query.Phone);
    }

    [Fact]
    public void Build_AllowsPhoneAndEmail()
    {
        var query = EnrichmentQueryBuilder.New()
            .WithPhone("5125550100")
            .WithEmail("john@example.com")
            .Build();

        Assert.Equal("5125550100", query.Phone);
        Assert.Equal("john@example.com", query.Email);
    }

    [Fact]
    public void Build_AllowsNameAndAddress()
    {
        var query = EnrichmentQueryBuilder.Create("John", "Smith")
            .WithAddress("123 Main St", "Austin, TX 78701")
            .Build();

        Assert.NotNull(query.Address);
        Assert.Equal("123 Main St", query.Address!.AddressLine1);
    }
}
