using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Api.Models.Enums;
using Lyo.Api.Tests.Fixtures;
using Lyo.Json;
using Lyo.Common.Metadata.Records;
using Lyo.Job.Models.Response;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using Lyo.Common.Core.Identifiers;

namespace Lyo.Api.Tests;

/// <summary>Checks for dynamic CRUD endpoints (MapDynamicCrudEndpoints). Uses entity-as-request-response at /api/Job/JobDefinition/*.</summary>
[Collection(ApiPostgresCollection.Name)]
public class DynamicCrudApiPostgresTests : IDisposable
{
    private const string BaseRoute = "/api/Job/JobDefinition";

    /// <summary>Match <see cref="Microsoft.AspNetCore.Http.Json.JsonOptions" /> on the test host (<see cref="LyoJsonSerializerOptions" />).</summary>
    private static readonly JsonSerializerOptions JsonOptions = LyoJsonSerializerOptions.Create();

    private readonly HttpClient _client;

    private readonly ApiPostgresFixture _fixture;

    public DynamicCrudApiPostgresTests(ApiPostgresFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task Metadata_ReturnsEntityTypes()
    {
        var response = await _client.GetAsync($"{BaseRoute}/Metadata", TestCt);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<EntityTypeMetadata>(JsonOptions, TestCt);
        Assert.NotNull(result);
        Assert.Equal("JobDefinition", result.EntityType);
    }

    [Fact]
    public async Task Query_Endpoint_ReturnsJobDefinitions()
    {
        await _fixture.SeedJobDefinitionAsync("DynamicQueryTest");
        var request = new QueryConcreteReq { Start = 0, Amount = 10 };
        var response = await _client.PostAsJsonAsync($"{BaseRoute}/QueryConcrete", request, JsonOptions, TestCt);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<QueryRes<JobDefinitionRes>>(JsonOptions, TestCt);
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.True(result.Items!.Count >= 1);
    }

    [Fact]
    public async Task Query_Endpoint_WithFilters_ReturnsMatchingItems()
    {
        await _fixture.SeedJobDefinitionAsync("DynamicQueryFilterA");
        await _fixture.SeedJobDefinitionAsync("DynamicQueryFilterB");
        var request = new QueryConcreteReq { Start = 0, Amount = 10, WhereClause = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.Equals, "DynamicQueryFilterA") };
        var response = await _client.PostAsJsonAsync($"{BaseRoute}/QueryConcrete", request, JsonOptions, TestCt);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<QueryRes<JobDefinitionRes>>(JsonOptions, TestCt);
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Single(result.Items!);
        Assert.Equal("DynamicQueryFilterA", result.Items![0].Name);
    }

    [Fact]
    public async Task QueryProject_Endpoint_WithSelect_ReturnsProjectedRows()
    {
        var name = $"DynamicProjected_{LyoGuid.CreateV7():N}";
        var defId = await _fixture.SeedJobDefinitionAsync(name, "Dynamic projected description");
        var request = new ProjectionQueryReq {
            Start = 0,
            Amount = 10,
            Select = ["Id", "Name"],
            WhereClause = WhereClauseBuilder.Condition("Id", ComparisonOperatorEnum.Equals, defId)
        };

        var response = await _client.PostAsJsonAsync($"{BaseRoute}/QueryProject", request, JsonOptions, TestCt);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ProjectedQueryRes<JsonElement>>(JsonOptions, TestCt);
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Single(result.Items!);
        var row = result.Items![0];
        Assert.Equal(JsonValueKind.Object, row.ValueKind);
        Assert.True(row.TryGetProperty("Id", out var _));
        Assert.True(row.TryGetProperty("Name", out var nameProp));
        Assert.Equal(name, nameProp.GetString());
    }

    [Fact]
    public async Task Get_Endpoint_ReturnsJobDefinition()
    {
        var defId = await _fixture.SeedJobDefinitionAsync("DynamicGetTest");
        var response = await _client.GetAsync($"{BaseRoute}/{defId}", TestCt);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JobDefinitionRes>(JsonOptions, TestCt);
        Assert.NotNull(result);
        Assert.Equal(defId, result.Id);
        Assert.Equal("DynamicGetTest", result.Name);
    }

    [Fact]
    public async Task Get_Endpoint_WhenNotFound_Returns404()
    {
        var response = await _client.GetAsync($"{BaseRoute}/{LyoGuid.CreateV7()}", TestCt);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_Endpoint_PersistsAndReturns201()
    {
        var id = LyoGuid.CreateV7();
        var entity = new {
            Id = id,
            Name = "DynamicCreateTest",
            Description = "Created via dynamic endpoint",
            Type = "Test",
            WorkerType = ProgrammingLanguageInfo.CSharp.ShortName,
            Enabled = true
        };

        var response = await _client.PostAsJsonAsync(BaseRoute, entity, JsonOptions, TestCt);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CreateResult<JobDefinitionRes>>(JsonOptions, TestCt);
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(id, result.Data!.Id);
        Assert.Equal("DynamicCreateTest", result.Data!.Name);
    }

    [Fact]
    public async Task Create_WithExplicitId_RespectsId()
    {
        var id = LyoGuid.CreateV7();
        var entity = new {
            Id = id,
            Name = "DynamicExplicitIdTest",
            Description = "Test",
            Type = "Test",
            WorkerType = ProgrammingLanguageInfo.CSharp.ShortName,
            Enabled = true
        };

        var response = await _client.PostAsJsonAsync(BaseRoute, entity, JsonOptions, TestCt);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CreateResult<JobDefinitionRes>>(JsonOptions, TestCt);
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(id, result.Data!.Id);
    }

    [Fact]
    public async Task Update_Endpoint_ModifiesEntity()
    {
        var defId = await _fixture.SeedJobDefinitionAsync("DynamicUpdateOriginal");
        var entity = new {
            Name = "DynamicUpdateModified",
            Description = "Updated via dynamic endpoint",
            Type = "Test",
            WorkerType = ProgrammingLanguageInfo.CSharp.ShortName,
            Enabled = false
        };

        var updateRequest = new UpdateRequest<object>(entity, defId);
        var response = await _client.PostAsJsonAsync($"{BaseRoute}/Update", updateRequest, JsonOptions, TestCt);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<UpdateResult<JobDefinitionRes>>(JsonOptions, TestCt);
        Assert.NotNull(result);
        Assert.Equal(UpdateResultEnum.Updated, result.Result);
        Assert.NotNull(result.NewData);
        Assert.Equal("DynamicUpdateModified", result.NewData!.Name);
    }

    [Fact]
    public async Task Patch_Endpoint_ModifiesProperties()
    {
        var defId = await _fixture.SeedJobDefinitionAsync("DynamicPatchOriginal");
        var patchRequest = new PatchRequest { Keys = [[defId]], Properties = new() { ["Name"] = "DynamicPatchModified" } };
        var response = await _client.PatchAsJsonAsync(BaseRoute, patchRequest, JsonOptions, TestCt);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PatchResult<JobDefinitionRes>>(JsonOptions, TestCt);
        Assert.NotNull(result);
        Assert.Equal(PatchResultEnum.Updated, result.Result);
        Assert.Equal("DynamicPatchModified", result.NewData!.Name);
    }

    [Fact]
    public async Task Delete_Endpoint_RemovesEntity()
    {
        var defId = await _fixture.SeedJobDefinitionAsync("DynamicDeleteTest");
        var response = await _client.DeleteAsync($"{BaseRoute}/{defId}", TestCt);
        response.EnsureSuccessStatusCode();
        var getResponse = await _client.GetAsync($"{BaseRoute}/{defId}", TestCt);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_Endpoint_WhenNotFound_Returns404()
    {
        var response = await _client.DeleteAsync($"{BaseRoute}/{LyoGuid.CreateV7()}", TestCt);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateBulk_Endpoint_PersistsAll()
    {
        var entities = new[] {
            new {
                Id = LyoGuid.CreateV7(),
                Name = "DynamicBulk1",
                Description = "Bulk 1",
                Type = "Test",
                WorkerType = ProgrammingLanguageInfo.CSharp.ShortName,
                Enabled = true
            },
            new {
                Id = LyoGuid.CreateV7(),
                Name = "DynamicBulk2",
                Description = "Bulk 2",
                Type = "Test",
                WorkerType = ProgrammingLanguageInfo.CSharp.ShortName,
                Enabled = true
            }
        };

        var response = await _client.PostAsJsonAsync($"{BaseRoute}/Bulk", entities, JsonOptions, TestCt);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreateBulkResult<JobDefinitionRes>>(JsonOptions, TestCt);
        Assert.NotNull(result);
        Assert.Equal(2, result.CreatedCount);
        Assert.Equal(0, result.FailedCount);
    }

    [Fact]
    public async Task UpdateBulk_Endpoint_ModifiesEntities()
    {
        var id1 = await _fixture.SeedJobDefinitionAsync("DynamicUpdateBulk1");
        var id2 = await _fixture.SeedJobDefinitionAsync("DynamicUpdateBulk2");
        var requests = new[] {
            new UpdateRequest<object>(
                new {
                    Name = "DynamicUpdateBulk1Mod",
                    Description = "Mod1",
                    Type = "Test",
                    WorkerType = ProgrammingLanguageInfo.CSharp.ShortName,
                    Enabled = false
                }, id1),
            new UpdateRequest<object>(
                new {
                    Name = "DynamicUpdateBulk2Mod",
                    Description = "Mod2",
                    Type = "Test",
                    WorkerType = ProgrammingLanguageInfo.CSharp.ShortName,
                    Enabled = false
                }, id2)
        };

        var response = await _client.PostAsJsonAsync($"{BaseRoute}/Bulk/Update", requests, JsonOptions, TestCt);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<UpdateBulkResult<JobDefinitionRes>>(JsonOptions, TestCt);
        Assert.NotNull(result);
        Assert.Equal(2, result.UpdatedCount);
        Assert.Equal(0, result.NoChangeCount);
        Assert.Equal(0, result.FailedCount);
    }

    [Fact]
    public async Task PatchBulk_Endpoint_ModifiesProperties()
    {
        var id1 = await _fixture.SeedJobDefinitionAsync("DynamicPatchBulk1");
        var id2 = await _fixture.SeedJobDefinitionAsync("DynamicPatchBulk2");
        var requests = new[] {
            new PatchRequest { Keys = [[id1]], Properties = new() { ["Name"] = "DynamicPatchBulk1Mod" } },
            new PatchRequest { Keys = [[id2]], Properties = new() { ["Name"] = "DynamicPatchBulk2Mod" } }
        };

        var response = await _client.PatchAsJsonAsync($"{BaseRoute}/Bulk", requests, JsonOptions, TestCt);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PatchBulkResult<JobDefinitionRes>>(JsonOptions, TestCt);
        Assert.NotNull(result);
        Assert.Equal(2, result.UpdatedCount);
        Assert.Equal(0, result.FailedCount);
    }

    [Fact]
    public async Task Upsert_Endpoint_WhenNotExists_Creates()
    {
        var entity = new {
            Id = LyoGuid.CreateV7(),
            Name = "DynamicUpsertCreate",
            Description = "Upsert created",
            Type = "Test",
            WorkerType = ProgrammingLanguageInfo.CSharp.ShortName,
            Enabled = true
        };

        var upsertRequest = new UpsertRequest<object>(entity, "Name", "DynamicUpsertCreate");
        var response = await _client.PostAsJsonAsync($"{BaseRoute}/Upsert", upsertRequest, JsonOptions, TestCt);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<UpsertResult<JobDefinitionRes>>(JsonOptions, TestCt);
        Assert.NotNull(result);
        Assert.Equal(UpsertResultEnum.Created, result.Result);
        Assert.NotNull(result.NewData);
        Assert.Equal("DynamicUpsertCreate", result.NewData!.Name);
    }

    [Fact]
    public async Task Upsert_Endpoint_WhenExists_Updates()
    {
        var defId = await _fixture.SeedJobDefinitionAsync("DynamicUpsertOriginal");
        var entity = new {
            Name = "DynamicUpsertUpdated",
            Description = "Upsert updated",
            Type = "Test",
            WorkerType = ProgrammingLanguageInfo.CSharp.ShortName,
            Enabled = false
        };

        var upsertRequest = new UpsertRequest<object>(entity, "Id", defId);
        var response = await _client.PostAsJsonAsync($"{BaseRoute}/Upsert", upsertRequest, JsonOptions, TestCt);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<UpsertResult<JobDefinitionRes>>(JsonOptions, TestCt);
        Assert.NotNull(result);
        Assert.Equal(UpsertResultEnum.Updated, result.Result);
        Assert.NotNull(result.NewData);
        Assert.Equal("DynamicUpsertUpdated", result.NewData!.Name);
    }

    [Fact]
    public async Task UpsertBulk_Endpoint_CreatesAndUpdates()
    {
        var existingId = await _fixture.SeedJobDefinitionAsync("DynamicUpsertBulkExisting");
        var requests = new[] {
            new UpsertRequest<object>(
                new {
                    Id = LyoGuid.CreateV7(),
                    Name = "DynamicUpsertBulkNew",
                    Description = "New",
                    Type = "Test",
                    WorkerType = ProgrammingLanguageInfo.CSharp.ShortName,
                    Enabled = true
                }, "Name", "DynamicUpsertBulkNew"),
            new UpsertRequest<object>(
                new {
                    Name = "DynamicUpsertBulkUpdated",
                    Description = "Updated",
                    Type = "Test",
                    WorkerType = ProgrammingLanguageInfo.CSharp.ShortName,
                    Enabled = false
                }, "Id", existingId)
        };

        var response = await _client.PostAsJsonAsync($"{BaseRoute}/Bulk/Upsert", requests, JsonOptions, TestCt);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<UpsertBulkResult<JobDefinitionRes>>(JsonOptions, TestCt);
        Assert.NotNull(result);
        Assert.Equal(1, result.CreatedCount);
        Assert.Equal(1, result.UpdatedCount);
    }

    [Fact]
    public async Task Delete_Endpoint_ByBody_RemovesEntity()
    {
        var defId = await _fixture.SeedJobDefinitionAsync("DynamicDeleteByBody");
        var deleteRequest = new DeleteRequest { Keys = [[defId]] };
        using var msg = new HttpRequestMessage(HttpMethod.Delete, BaseRoute);
        msg.Content = JsonContent.Create(deleteRequest);
        var response = await _client.SendAsync(msg, TestCt);
        response.EnsureSuccessStatusCode();
        var getResponse = await _client.GetAsync($"{BaseRoute}/{defId}", TestCt);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_Endpoint_ByIdentifiers_RemovesMatchingEntity()
    {
        var defId = await _fixture.SeedJobDefinitionAsync("DynamicDeleteByIdentifier");
        var deleteRequest = new DeleteRequest("Name", "DynamicDeleteByIdentifier");
        using var msg = new HttpRequestMessage(HttpMethod.Delete, BaseRoute);
        msg.Content = JsonContent.Create(deleteRequest);
        var response = await _client.SendAsync(msg, TestCt);
        response.EnsureSuccessStatusCode();
        var getResponse = await _client.GetAsync($"{BaseRoute}/{defId}", TestCt);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteBulk_Endpoint_RemovesEntities()
    {
        var id1 = await _fixture.SeedJobDefinitionAsync("DynamicDeleteBulk1");
        var id2 = await _fixture.SeedJobDefinitionAsync("DynamicDeleteBulk2");
        var requests = new[] { new DeleteRequest { Keys = [[id1]] }, new DeleteRequest { Keys = [[id2]] } };
        using var msg = new HttpRequestMessage(HttpMethod.Delete, $"{BaseRoute}/Bulk");
        msg.Content = JsonContent.Create(requests);
        var response = await _client.SendAsync(msg, TestCt);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<DeleteBulkResult<JobDefinitionRes>>(JsonOptions, TestCt);
        Assert.NotNull(result);
        Assert.Equal(2, result.DeletedCount);
        Assert.Equal(0, result.FailedCount);
    }

    [Fact]
    public async Task Export_Endpoint_ReturnsCsv()
    {
        var name = $"DynamicExportTest_{LyoGuid.CreateV7():N}";
        await _fixture.SeedJobDefinitionAsync(name);
        var request = new ExportRequest {
            Query = new() { Start = 0, Amount = 10, WhereClause = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.Equals, name) }, Format = ExportFormat.Csv
        };

        var response = await _client.PostAsJsonAsync($"{BaseRoute}/Export", request, JsonOptions, TestCt);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync(TestCt);
        Assert.NotEmpty(content);
        Assert.Contains(name, content);
    }

    [Fact]
    public async Task Export_Endpoint_ReturnsJson()
    {
        var name = $"DynamicExportJsonTest_{LyoGuid.CreateV7():N}";
        await _fixture.SeedJobDefinitionAsync(name);
        var request = new ExportRequest {
            Query = new() { Start = 0, Amount = 10, WhereClause = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.Equals, name) }, Format = ExportFormat.Json
        };

        var response = await _client.PostAsJsonAsync($"{BaseRoute}/Export", request, JsonOptions, TestCt);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync(TestCt);
        Assert.NotEmpty(content);
        Assert.Contains(name, content);
    }

    [Fact]
    public async Task UnknownEntityType_Returns404()
    {
        var request = new QueryConcreteReq { Start = 0, Amount = 10 };
        var response = await _client.PostAsJsonAsync("/api/Job/UnknownEntity/QueryConcrete", request, JsonOptions, TestCt);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}