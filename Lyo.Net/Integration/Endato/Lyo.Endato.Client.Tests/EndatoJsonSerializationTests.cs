using System.Text.Json;
using Lyo.Endato.Client;
using Lyo.Endato.Client.Models.Enrichment.Request;
using Lyo.Endato.Client.Models.Person.Request;

namespace Lyo.Endato.Client.Tests;

public class EndatoJsonSerializationTests
{
    private static readonly JsonSerializerOptions Options = EndatoJsonSerializerOptions.Create();

    [Fact]
    public void PersonQuery_SerializesDobWireName()
    {
        var query = PersonQueryBuilder.Create("John", "Smith")
            .WithDateOfBirth("01/15/1980")
            .WithResultsPerPage(10)
            .Build();

        var json = JsonSerializer.Serialize(query, Options);

        Assert.Contains("\"dob\":\"01/15/1980\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"firstName\":\"John\"", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnrichmentQuery_SerializesAddressLinesInCamelCase()
    {
        var query = EnrichmentQueryBuilder.Create("John", "Smith")
            .WithPhone("5125550100")
            .WithDateOfBirth("01/15/1980")
            .WithAddress("123 Main St", "Austin, TX")
            .Build();

        var json = JsonSerializer.Serialize(query, Options);

        Assert.Contains("\"addressLine1\":\"123 Main St\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"addressLine2\":\"Austin, TX\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"dob\":\"01/15/1980\"", json, StringComparison.OrdinalIgnoreCase);
    }
}
