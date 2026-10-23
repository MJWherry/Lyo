using System.Text.Json;
using Lyo.Web.Components.Catalog;

namespace Lyo.Web.Components.Tests;

public sealed class CatalogSectionTests
{
    [Fact]
    public void TableJson_ReadsHeadersRowsLeadAndTrail()
    {
        const string json = """
                            {
                              "type": "table",
                              "title": "Contracts",
                              "headers": ["Interface", "Methods"],
                              "rows": [["ITtsService", "SynthesizeAsync"]],
                              "lead": "before",
                              "trail": "after"
                            }
                            """;

        var section = JsonSerializer.Deserialize<CatalogSection>(json, CatalogJson.Options);
        Assert.NotNull(section);
        Assert.Equal("table", section.Type);
        Assert.Equal("Contracts", section.Title);
        Assert.Equal(["Interface", "Methods"], section.Headers);
        Assert.NotNull(section.Rows);
        Assert.Equal(["ITtsService", "SynthesizeAsync"], section.Rows[0]);
        Assert.Equal("before", section.Lead);
        Assert.Equal("after", section.Trail);
    }
}