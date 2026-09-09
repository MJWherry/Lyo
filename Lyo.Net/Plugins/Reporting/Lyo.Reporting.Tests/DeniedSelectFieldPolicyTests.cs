using Lyo.Api.ApiEndpoint;
using Lyo.Api.Models.Common.Request;

namespace Lyo.Reporting.Tests;

public sealed class DeniedSelectFieldPolicyTests
{
    [Theory]
    [InlineData("EncryptedValue", true)]
    [InlineData("encryptedvalue", true)]
    [InlineData("Parameters.EncryptedValue", true)]
    [InlineData("Generations.Parameters.EncryptedValue", true)]
    [InlineData("EncryptedValue.Length", true)]
    [InlineData("Name", false)]
    [InlineData("Parameters.Key", false)]
    public void IsDeniedField_NestedPaths_ReturnsExpected(string field, bool denied)
        => Assert.Equal(denied, DeniedSelectFieldPolicy.IsDeniedField(field, ["EncryptedValue"]));

    [Fact]
    public void ValidateProjection_SelectAndComputedTemplates_Rejects()
    {
        var errors = DeniedSelectFieldPolicy.ValidateProjection(
            ["Key", "Parameters.EncryptedValue"], [new() { Name = "Sneaky", Template = "{EncryptedValue}" }], ["EncryptedValue"]);

        Assert.Equal(2, errors.Count);
    }

    [Fact]
    public void ValidateExport_ColumnsAndQuerySelects_Rejects()
    {
        var request = new ExportRequest {
            Query = new() { Select = ["Parameters.EncryptedValue"] },
            Columns = new() { ["Secret"] = "EncryptedValue" },
            ColumnList = [new() { Header = "Sneaky", Value = "{EncryptedValue}" }]
        };

        var errors = DeniedSelectFieldPolicy.ValidateExport(request, ["EncryptedValue"]);
        Assert.Equal(3, errors.Count);
    }

    [Fact]
    public void ValidateExport_CleanRequest_Allows()
    {
        var request = new ExportRequest { Query = new() { Select = ["Key", "Value"] }, Columns = new() { ["Key"] = "Key" } };
        Assert.Empty(DeniedSelectFieldPolicy.ValidateExport(request, ["EncryptedValue"]));
    }
}
