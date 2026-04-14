using Lyo.Api.Services.Crud.Read;
using Lyo.Api.Services.Crud.Read.Project;
using Lyo.Formatter;
using Lyo.Query.Models.Common.Request;

namespace Lyo.Api.Tests.Services.Projection;

public sealed class ComputedFieldTests
{
    private readonly IFormatterService _formatter = new FormatterService();

    private static ProjectedFieldSpec Spec(string path) => new(path, path, path.Split('.'));

    [Fact]
    public void ApplyComputedFields_ConcatenatesMultipleFields()
    {
        var service = new ProjectionService(_formatter);
        var specs = new[] { Spec("FirstName"), Spec("LastName") };
        var items = new List<object?> {
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["FirstName"] = "Alice", ["LastName"] = "Smith" },
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["FirstName"] = "Bob", ["LastName"] = "Jones" }
        };

        var computedFields = new List<ComputedField> { new("FullName", "{FirstName} {LastName}") };
        var result = service.ApplyComputedFields(items, computedFields, specs);
        Assert.Equal(2, result.Count);
        var row0 = Assert.IsType<Dictionary<string, object?>>(result[0]);
        Assert.Equal("Alice Smith", row0["FullName"]);
        var row1 = Assert.IsType<Dictionary<string, object?>>(result[1]);
        Assert.Equal("Bob Jones", row1["FullName"]);
    }

    [Fact]
    public void ApplyComputedFields_SinglePlaceholder_UsesDirectLookup()
    {
        var service = new ProjectionService(_formatter);
        var specs = new[] { Spec("Email") };
        var items = new List<object?> { new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["Email"] = "alice@test.com" } };
        var computedFields = new List<ComputedField> { new("EmailCol", "{Email}") };
        var result = service.ApplyComputedFields(items, computedFields, specs);
        var row = Assert.IsType<Dictionary<string, object?>>(result[0]);
        Assert.Equal("alice@test.com", row["EmailCol"]);
    }

    [Fact]
    public void ApplyComputedFields_WithDateFormat_FormatsCorrectly()
    {
        var service = new ProjectionService(_formatter);
        var specs = new[] { Spec("CreatedAt") };
        var items = new List<object?> { new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["CreatedAt"] = new DateTime(2024, 3, 15) } };
        var computedFields = new List<ComputedField> { new("FormattedDate", "{CreatedAt:yyyy-MM-dd}") };
        var result = service.ApplyComputedFields(items, computedFields, specs);
        var row = Assert.IsType<Dictionary<string, object?>>(result[0]);
        Assert.Equal("2024-03-15", row["FormattedDate"]);
    }

    [Fact]
    public void ApplyComputedFields_PreservesOriginalFields()
    {
        var service = new ProjectionService(_formatter);
        var specs = new[] { Spec("FirstName"), Spec("LastName") };
        var items = new List<object?> { new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["FirstName"] = "Alice", ["LastName"] = "Smith" } };
        var computedFields = new List<ComputedField> { new("FullName", "{FirstName} {LastName}") };
        var result = service.ApplyComputedFields(items, computedFields, specs);
        var row = Assert.IsType<Dictionary<string, object?>>(result[0]);
        Assert.Equal("Alice", row["FirstName"]);
        Assert.Equal("Smith", row["LastName"]);
        Assert.Equal("Alice Smith", row["FullName"]);
    }

    [Fact]
    public void ApplyComputedFields_NullItem_PassedThrough()
    {
        var service = new ProjectionService(_formatter);
        var specs = new[] { Spec("Name") };
        var items = new List<object?> { null };
        var computedFields = new List<ComputedField> { new("Computed", "{Name}") };
        var result = service.ApplyComputedFields(items, computedFields, specs);
        Assert.Single(result);
        Assert.Null(result[0]);
    }

    [Fact]
    public void ApplyComputedFields_EmptyComputedFields_ReturnsUnchanged()
    {
        var service = new ProjectionService(_formatter);
        var specs = new[] { Spec("Name") };
        var items = new List<object?> { new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["Name"] = "Alice" } };
        var result = service.ApplyComputedFields(items, [], specs);
        Assert.Same(items, result);
    }

    [Fact]
    public void ApplyComputedFields_NoFormatter_ReturnsUnchanged()
    {
        var service = new ProjectionService();
        var specs = new[] { Spec("Name") };
        var items = new List<object?> { new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["Name"] = "Alice" } };
        var computedFields = new List<ComputedField> { new("Greeting", "Hello {Name}") };
        var result = service.ApplyComputedFields(items, computedFields, specs);
        Assert.Same(items, result);
    }

    [Fact]
    public void ApplyComputedFields_ScalarItem_PromotedToDictionary()
    {
        var service = new ProjectionService(_formatter);
        var specs = new[] { Spec("Name") };
        var items = new List<object?> { "Alice" };
        var computedFields = new List<ComputedField> { new("Greeting", "Hello {Name}") };
        var result = service.ApplyComputedFields(items, computedFields, specs);
        var row = Assert.IsType<Dictionary<string, object?>>(result[0]);
        Assert.Equal("Alice", row["Name"]);
        Assert.Equal("Hello Alice", row["Greeting"]);
    }

    [Fact]
    public void ApplyComputedFields_MultipleComputedFields()
    {
        var service = new ProjectionService(_formatter);
        var specs = new[] { Spec("First"), Spec("Last") };
        var items = new List<object?> { new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["First"] = "Alice", ["Last"] = "Smith" } };
        var computedFields = new List<ComputedField> { new("FullName", "{First} {Last}"), new("Reversed", "{Last}, {First}") };
        var result = service.ApplyComputedFields(items, computedFields, specs);
        var row = Assert.IsType<Dictionary<string, object?>>(result[0]);
        Assert.Equal("Alice Smith", row["FullName"]);
        Assert.Equal("Smith, Alice", row["Reversed"]);
    }

    [Fact]
    public void ApplyComputedFields_TemplateUsesLowercaseTokens_ResolvesAgainstPascalCaseRowKeys()
    {
        var service = new ProjectionService(_formatter);
        var specs = new[] { Spec("FirstName"), Spec("LastName") };
        var items = new List<object?> { new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["FirstName"] = "Jane", ["LastName"] = "Doe" } };
        var computedFields = new List<ComputedField> { new("Full", "{LastName}, {firstname}") };
        var result = service.ApplyComputedFields(items, computedFields, specs);
        var row = Assert.IsType<Dictionary<string, object?>>(result[0]);
        Assert.Equal("Doe, Jane", row["Full"]);
    }

    [Fact]
    public void ApplyComputedFields_MissingPlaceholder_ReturnsPlaceholder()
    {
        var service = new ProjectionService(_formatter);
        var specs = new[] { Spec("First") };
        var items = new List<object?> { new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["First"] = "Alice" } };
        var computedFields = new List<ComputedField> { new("Missing", "{NonExistent}") };
        var result = service.ApplyComputedFields(items, computedFields, specs);
        var row = Assert.IsType<Dictionary<string, object?>>(result[0]);
        Assert.Equal("{NonExistent}", row["Missing"]);
    }

    // --- GetComputedFieldDependencies ---

    [Fact]
    public void GetComputedFieldDependencies_ExtractsPlaceholderNames()
    {
        var service = new ProjectionService(_formatter);
        var computedFields = new List<ComputedField> { new("FullName", "{FirstName} {LastName}") };
        var deps = service.GetComputedFieldDependencies(computedFields);
        Assert.Equal(2, deps.Count);
        Assert.Contains("FirstName", deps, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("LastName", deps, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetComputedFieldDependencies_MultipleTemplates_ReturnsDistinctUnion()
    {
        var service = new ProjectionService(_formatter);
        var computedFields = new List<ComputedField> { new("Full", "{FirstName} {LastName}"), new("Greeting", "Hello {FirstName}, created {CreatedAt:yyyy-MM-dd}") };
        var deps = service.GetComputedFieldDependencies(computedFields);
        Assert.Equal(3, deps.Count);
        Assert.Contains("FirstName", deps, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("LastName", deps, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("CreatedAt", deps, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetComputedFieldDependencies_NoFormatter_ReturnsEmpty()
    {
        var service = new ProjectionService();
        var computedFields = new List<ComputedField> { new("FullName", "{FirstName} {LastName}") };
        var deps = service.GetComputedFieldDependencies(computedFields);
        Assert.Empty(deps);
    }

    [Fact]
    public void GetComputedFieldDependencies_EmptyList_ReturnsEmpty()
    {
        var service = new ProjectionService(_formatter);
        var deps = service.GetComputedFieldDependencies([]);
        Assert.Empty(deps);
    }

    [Fact]
    public void GetComputedFieldDependencies_BlankTemplate_Skipped()
    {
        var service = new ProjectionService(_formatter);
        var computedFields = new List<ComputedField> { new("A", ""), new("B", "  "), new("C", "{Name}") };
        var deps = service.GetComputedFieldDependencies(computedFields);
        Assert.Single(deps);
        Assert.Contains("Name", deps, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnsureSelectIncludesComputedDependencies_AppendsMissingPaths()
    {
        var service = new ProjectionService(_formatter);
        var request = new ProjectionQueryReq {
            Select = ["Id"],
            ComputedFields = [new("Full", "{FirstName} {LastName}")]
        };

        var added = service.EnsureSelectIncludesComputedDependencies(request);

        Assert.Equal(2, added.Count);
        Assert.Contains("FirstName", request.Select, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("LastName", request.Select, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("FirstName", added, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("LastName", added, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnsureSelectIncludesComputedDependencies_DoesNotDuplicateExistingSelect()
    {
        var service = new ProjectionService(_formatter);
        var request = new ProjectionQueryReq {
            Select = ["Id", "FirstName"],
            ComputedFields = [new("Full", "{FirstName} {LastName}")]
        };

        var added = service.EnsureSelectIncludesComputedDependencies(request);

        Assert.Single(added);
        Assert.Contains("LastName", added, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnsureSelectIncludesComputedDependencies_DoesNotAppendPlaceholderPathAlreadyInSelect_EvenWithDifferentCasingOrWhitespace()
    {
        var service = new ProjectionService(_formatter);
        var request = new ProjectionQueryReq {
            Select = ["  docketcharges.number  "],
            ComputedFields = [new("a", "{docketcharges.number} - {docketcharges.description}")]
        };

        var added = service.EnsureSelectIncludesComputedDependencies(request);

        Assert.Single(added);
        Assert.Equal("docketcharges.description", added[0].Trim(), StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnsureSelectIncludesComputedDependencies_AppendsNestedLeaf_WhenOnlyCollectionScopeWildcardPresent()
    {
        var service = new ProjectionService(_formatter);
        var request = new ProjectionQueryReq {
            Select = ["contactaddresses.*"],
            ComputedFields = [new("n", "{contactaddresses.address.streettype}")]
        };

        var added = service.EnsureSelectIncludesComputedDependencies(request);

        Assert.Single(added);
        Assert.Contains("contactaddresses.address.streettype", request.Select, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnsureSelectIncludesComputedDependencies_CollectionWildcardCoversOnlySingleSegmentUnderCollection()
    {
        var service = new ProjectionService(_formatter);
        var request = new ProjectionQueryReq {
            Select = ["contactaddresses.*"],
            ComputedFields = [new("t", "{contactaddresses.type}")]
        };

        var added = service.EnsureSelectIncludesComputedDependencies(request);

        Assert.Empty(added);
    }

    [Fact]
    public void EnsureSelectIncludesComputedDependencies_AddressBranchWildcardCoversLeafUnderAddress()
    {
        var service = new ProjectionService(_formatter);
        var request = new ProjectionQueryReq {
            Select = ["contactaddresses.address.*"],
            ComputedFields = [new("x", "{contactaddresses.address.streetname}")]
        };

        var added = service.EnsureSelectIncludesComputedDependencies(request);

        Assert.Empty(added);
    }
}