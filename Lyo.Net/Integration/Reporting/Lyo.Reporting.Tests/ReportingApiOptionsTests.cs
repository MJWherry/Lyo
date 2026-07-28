using Lyo.Api.ApiEndpoint;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Reporting;
using Lyo.Reporting.Postgres;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Reporting.Tests;

public sealed class ReportingApiOptionsTests
{
    [Fact]
    public void Auth_surfaces_default_to_require_authorization()
    {
        var options = new ReportingApiOptions();
        foreach (var auth in new[] { options.DefinitionAuth, options.GenerationAuth, options.GenerateAuth, options.DownloadAuth }) {
            Assert.NotNull(auth);
            Assert.False(auth!.AllowAnonymous);
        }
    }

    [Fact]
    public void Download_factory_defaults_to_null_so_endpoint_is_not_mapped() => Assert.Null(new ReportingApiOptions().DownloadStreamFactory);

    [Fact]
    public void WithAuth_applies_the_same_auth_to_all_surfaces()
    {
        var auth = EndpointAuth.Anonymous();
        Func<ReportDownloadContext, CancellationToken, Task<Stream?>> factory = (_, _) => Task.FromResult<Stream?>(null);
        var options = ReportingApiOptions.WithAuth(auth, factory);
        Assert.Same(auth, options.DefinitionAuth);
        Assert.Same(auth, options.GenerationAuth);
        Assert.Same(auth, options.GenerateAuth);
        Assert.Same(auth, options.DownloadAuth);
        Assert.Same(factory, options.DownloadStreamFactory);
    }

    [Fact]
    public void WithAuth_download_factory_defaults_to_null() => Assert.Null(ReportingApiOptions.WithAuth(EndpointAuth.RequireAuthorization()).DownloadStreamFactory);

    [Fact]
    public void AddReportingApi_registers_management_and_api_services()
    {
        var services = new ServiceCollection();
        services.AddReportingApi(o => o.ConnectionString = "Host=localhost;Database=reporting_test");
        Assert.Contains(services, d => d.ServiceType == typeof(ReportService));
        Assert.Contains(services, d => d.ServiceType == typeof(ReportRetentionService));
        Assert.Contains(services, d => d.ServiceType == typeof(ReportGenerationThrottle));
        Assert.Contains(services, d => d.ServiceType == typeof(IHttpContextAccessor));
    }

    [Fact]
    public void AddReportingApi_validates_options() => Assert.Throws<ArgumentException>(() => new ServiceCollection().AddReportingApi(new PostgresReportingOptions()));

    [Theory]
    [InlineData("EncryptedValue", true)]
    [InlineData("encryptedvalue", true)]
    [InlineData("Parameters.EncryptedValue", true)]
    [InlineData("Generations.Parameters.EncryptedValue", true)]
    [InlineData("EncryptedValue.Length", true)]
    [InlineData("Name", false)]
    [InlineData("Parameters.Key", false)]
    public void Denied_select_fields_block_nested_paths(string field, bool denied) => Assert.Equal(denied, DeniedSelectFieldPolicy.IsDeniedField(field, ["EncryptedValue"]));

    [Fact]
    public void Denied_projection_rejects_select_and_computed_templates()
    {
        var errors = DeniedSelectFieldPolicy.ValidateProjection(
            ["Key", "Parameters.EncryptedValue"], [new() { Name = "Sneaky", Template = "{EncryptedValue}" }], ["EncryptedValue"]);

        Assert.Equal(2, errors.Count);
    }

    [Fact]
    public void Denied_export_rejects_columns_and_query_selects()
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
    public void Denied_export_allows_clean_requests()
    {
        var request = new ExportRequest { Query = new() { Select = ["Key", "Value"] }, Columns = new() { ["Key"] = "Key" } };
        Assert.Empty(DeniedSelectFieldPolicy.ValidateExport(request, ["EncryptedValue"]));
    }
}