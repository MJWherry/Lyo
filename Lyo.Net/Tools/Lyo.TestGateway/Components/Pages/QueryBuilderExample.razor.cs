using Microsoft.AspNetCore.Components;

namespace Lyo.TestGateway.Components.Pages;

public partial class QueryBuilderExample
{
    protected override string PageName { get; set; } = "Query Builder Example";

    // Entity routes for /QueryConcrete|/QueryProject; "" seeds the root /Query (dynamic base) for PeopleDbContext.
    public readonly Dictionary<string, List<string>> Routes = new() { ["http://localhost:5251"] = ["Person", ""], ["http://localhost:5074"] = ["Docket", "Docket/Charge"] };
}
