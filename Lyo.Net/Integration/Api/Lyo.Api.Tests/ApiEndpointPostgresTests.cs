using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Api.Models.Enums;
using Lyo.Api.Tests.Fixtures;
using Lyo.Common.Enums;
using Lyo.Common.Records;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;

namespace Lyo.Api.Tests;

[Collection(ApiPostgresCollection.Name)]
public class ApiEndpointPostgresTests : IDisposable
{
    private readonly HttpClient _client;
    private readonly ApiPostgresFixture _fixture;

    public ApiEndpointPostgresTests(ApiPostgresFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task Metadata_Endpoint_ReturnsEntityRequestAndResponseSchemas()
    {
        var response = await _client.GetAsync("/api/Job/Definition/Metadata", TestContext.Current.CancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<EndpointMetadataResponse>(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.Equal("Id", result.KeyPropertyName);
        Assert.Equal("Guid", result.KeyType);
        Assert.NotNull(result.Entity);
        Assert.Equal("JobDefinition", result.Entity.TypeName);
        Assert.Equal("JobDefinitionReq", result.Request?.TypeName);
        Assert.Equal("JobDefinitionRes", result.Response?.TypeName);
        Assert.Contains(result.Entity.Properties, p => p.Name == "CreatedTimestamp" && p.Type == "DateTime");
        Assert.Contains(result.Request!.Properties, p => p.Name == "CreateSchedules" && p.Type.StartsWith("List<", StringComparison.Ordinal));
        Assert.Contains(result.Response!.Properties, p => p.Name == "JobSchedules" && p.Type.StartsWith("IReadOnlyList<", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Query_Endpoint_ReturnsJobDefinitions()
    {
        await _fixture.SeedJobDefinitionAsync("EndpointQueryTest").ConfigureAwait(false);
        var request = new QueryReq { Start = 0, Amount = 10 };
        var response = await _client.PostAsJsonAsync("/api/Job/Definition/Query", request, TestContext.Current.CancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<QueryRes<JobDefinitionRes>>(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Items);
        Assert.True(result.Items!.Count >= 1);
        Assert.True(result.QueryScore > 0);
    }

    [Fact]
    public async Task Query_Endpoint_WithFilters_ReturnsMatchingItems()
    {
        await _fixture.SeedJobDefinitionAsync("EndpointQueryFilterA").ConfigureAwait(false);
        await _fixture.SeedJobDefinitionAsync("EndpointQueryFilterB").ConfigureAwait(false);
        var request = new QueryReq { Start = 0, Amount = 10, WhereClause = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.Equals, "EndpointQueryFilterA") };
        var response = await _client.PostAsJsonAsync("/api/Job/Definition/Query", request, TestContext.Current.CancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<QueryRes<JobDefinitionRes>>(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Single(result.Items!);
        Assert.Equal("EndpointQueryFilterA", result.Items![0].Name);
        Assert.True(result.QueryScore > 0);
    }

    [Fact]
    public async Task Query_Endpoint_WithInclude_LoadsNavigationProperties()
    {
        await _fixture.SeedJobDefinitionAsync("EndpointQueryInclude").ConfigureAwait(false);
        var request = new QueryReq {
            Start = 0,
            Amount = 10,
            Include = ["JobParameters", "JobSchedules"],
            WhereClause = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.Equals, "EndpointQueryInclude")
        };

        var response = await _client.PostAsJsonAsync("/api/Job/Definition/Query", request, TestContext.Current.CancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<QueryRes<JobDefinitionRes>>(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Single(result.Items!);
        Assert.NotNull(result.Items![0].JobParameters);
        Assert.NotNull(result.Items![0].JobSchedules);
    }

    [Fact]
    public async Task Query_Endpoint_WithTotalCountModeHasMore_ReturnsHasMoreFlag()
    {
        var prefix = $"EndpointHasMore_{Guid.NewGuid():N}";
        await _fixture.SeedJobDefinitionAsync($"{prefix}_A").ConfigureAwait(false);
        await _fixture.SeedJobDefinitionAsync($"{prefix}_B").ConfigureAwait(false);
        var request = new QueryReq {
            Start = 0,
            Amount = 1,
            Options = new() { TotalCountMode = QueryTotalCountMode.HasMore },
            WhereClause = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.StartsWith, prefix),
            SortBy = [new("Name", SortDirection.Asc)]
        };

        var response = await _client.PostAsJsonAsync("/api/Job/Definition/Query", request, TestContext.Current.CancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<QueryRes<JobDefinitionRes>>(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Single(result.Items!);
        Assert.Null(result.Total);
        Assert.True(result.HasMore);
    }

    [Fact]
    public async Task QueryProject_Endpoint_WithSelect_ReturnsProjectedRows()
    {
        var name = $"EndpointProjected_{Guid.NewGuid():N}";
        var defId = await _fixture.SeedJobDefinitionAsync(name, "Endpoint projected description").ConfigureAwait(false);
        var request = new ProjectionQueryReq {
            Start = 0,
            Amount = 10,
            Select = ["Id", "Name"],
            WhereClause = WhereClauseBuilder.Condition("Id", ComparisonOperatorEnum.Equals, defId)
        };

        var response = await _client.PostAsJsonAsync("/api/Job/Definition/QueryProject", request, TestContext.Current.CancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ProjectedQueryRes<JsonElement>>(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Single(result.Items!);
        var row = result.Items![0];
        Assert.Equal(JsonValueKind.Object, row.ValueKind);
        Assert.True(row.TryGetProperty("Id", out var _));
        Assert.True(row.TryGetProperty("Name", out var nameProp));
        Assert.False(row.TryGetProperty("Description", out var _));
        Assert.Equal(name, nameProp.GetString());
    }

    [Fact]
    public async Task QueryProject_Endpoint_WithoutSelect_ReturnsBadRequest()
    {
        var request = new ProjectionQueryReq { Start = 0, Amount = 10 };
        var response = await _client.PostAsJsonAsync("/api/Job/Definition/QueryProject", request, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task QueryProject_Endpoint_WithWildcardAndNavigationPath_ReturnsProjectedNestedPayload()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var defId = await _fixture.SeedJobDefinitionAsync($"EndpointWildcard_{suffix}").ConfigureAwait(false);
        await _fixture.SeedJobRunAsync(defId, $"endpoint-user-{suffix}").ConfigureAwait(false);
        var request = new ProjectionQueryReq {
            Start = 0,
            Amount = 10,
            Select = ["jobruns.*", "jobruns.createdby"],
            WhereClause = WhereClauseBuilder.Condition("Id", ComparisonOperatorEnum.Equals, defId)
        };

        var response = await _client.PostAsJsonAsync("/api/Job/Definition/QueryProject", request, TestContext.Current.CancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ProjectedQueryRes<JsonElement>>(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Single(result.Items!);
        var row = result.Items[0];
        Assert.Equal(JsonValueKind.Object, row.ValueKind);
        // Default ZipSiblingCollectionSelections merges jobruns.* + jobruns.createdby into one array under the collection merge key.
        // JSON uses camelCase for dictionary keys (e.g. jobRuns), not necessarily the select path casing.
        Assert.True(
            row.TryGetProperty("jobruns", out var jobRuns)
            || row.TryGetProperty("jobRuns", out jobRuns)
            || row.TryGetProperty("JobRuns", out jobRuns));
        Assert.Equal(JsonValueKind.Array, jobRuns.ValueKind);
        Assert.True(jobRuns.GetArrayLength() >= 1);
        var first = jobRuns[0];
        Assert.Equal(JsonValueKind.Object, first.ValueKind);
        Assert.True(
            first.TryGetProperty("CreatedBy", out _)
            || first.TryGetProperty("createdBy", out _)
            || first.TryGetProperty("createdby", out _),
            "merged row should include CreatedBy from jobruns.createdby");
    }

    [Fact]
    public async Task QueryProject_Endpoint_WithSingleCollectionWildcard_ReturnsArrayItem()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var defId = await _fixture.SeedJobDefinitionAsync($"EndpointSingleWildcard_{suffix}").ConfigureAwait(false);
        await _fixture.SeedJobRunAsync(defId, $"endpoint-single-user-{suffix}").ConfigureAwait(false);
        var request = new ProjectionQueryReq {
            Start = 0,
            Amount = 10,
            Select = ["jobruns.*"],
            WhereClause = WhereClauseBuilder.Condition("Id", ComparisonOperatorEnum.Equals, defId)
        };

        var response = await _client.PostAsJsonAsync("/api/Job/Definition/QueryProject", request, TestContext.Current.CancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ProjectedQueryRes<JsonElement>>(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Single(result.Items!);
        Assert.Equal(JsonValueKind.Array, result.Items[0].ValueKind);
    }

    [Fact]
    public async Task QueryProject_Endpoint_WithRootWildcard_DoesNotReturnWildcardWrapperKey()
    {
        var name = $"EndpointRootWildcard_{Guid.NewGuid():N}";
        var defId = await _fixture.SeedJobDefinitionAsync(name).ConfigureAwait(false);
        var request = new ProjectionQueryReq {
            Start = 0,
            Amount = 10,
            Select = ["*"],
            WhereClause = WhereClauseBuilder.Condition("Id", ComparisonOperatorEnum.Equals, defId)
        };

        var response = await _client.PostAsJsonAsync("/api/Job/Definition/QueryProject", request, TestContext.Current.CancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ProjectedQueryRes<JsonElement>>(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Single(result.Items!);
        var row = result.Items[0];
        Assert.Equal(JsonValueKind.Object, row.ValueKind);
        Assert.False(row.TryGetProperty("*", out var _));
        Assert.True(row.TryGetProperty("Id", out var _));
        Assert.True(row.TryGetProperty("Name", out var _));
    }

    [Fact]
    public async Task QueryProject_Endpoint_WithMatchedOnly_FiltersCollectionValues()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var defId = await _fixture.SeedJobDefinitionAsync($"EndpointMatchedOnly_{suffix}").ConfigureAwait(false);
        await _fixture.SeedJobRunAsync(defId, $"keep-{suffix}").ConfigureAwait(false);
        await _fixture.SeedJobRunAsync(defId, $"drop-{suffix}").ConfigureAwait(false);
        var request = new ProjectionQueryReq {
            Start = 0,
            Amount = 10,
            Options = new() { IncludeFilterMode = QueryIncludeFilterMode.MatchedOnly },
            Select = ["jobruns.createdby"],
            WhereClause = WhereClauseBuilder.And(and => and.AddCondition("jobruns.createdby", ComparisonOperatorEnum.Equals, $"keep-{suffix}").AddCondition("Id", ComparisonOperatorEnum.Equals, defId))
        };

        var response = await _client.PostAsJsonAsync("/api/Job/Definition/QueryProject", request, TestContext.Current.CancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ProjectedQueryRes<JsonElement>>(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Single(result.Items!);
        Assert.Equal(JsonValueKind.Array, result.Items[0].ValueKind);
        var values = result.Items[0].EnumerateArray().Select(i => i.GetString()).ToList();
        Assert.NotEmpty(values);
        Assert.All(values, v => Assert.Equal($"keep-{suffix}", v));
    }

    [Fact]
    public async Task Get_Endpoint_ReturnsJobDefinition()
    {
        var defId = await _fixture.SeedJobDefinitionAsync("EndpointGetTest").ConfigureAwait(false);
        var response = await _client.GetAsync($"/api/Job/Definition/{defId}", TestContext.Current.CancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JobDefinitionRes>(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.Equal(defId, result.Id);
        Assert.Equal("EndpointGetTest", result.Name);
        Assert.Contains("[beforeGet]", result.Description ?? "");
    }

    [Fact]
    public async Task Get_Endpoint_WhenNotFound_Returns404()
    {
        var response = await _client.GetAsync($"/api/Job/Definition/{Guid.NewGuid()}", TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_Endpoint_WithInclude_LoadsNavigationProperties()
    {
        var defId = await _fixture.SeedJobDefinitionAsync("EndpointGetInclude").ConfigureAwait(false);
        var response = await _client.GetAsync($"/api/Job/Definition/{defId}?include=JobParameters&include=JobSchedules", TestContext.Current.CancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JobDefinitionRes>(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.NotNull(result.JobParameters);
        Assert.NotNull(result.JobSchedules);
    }

    [Fact]
    public async Task Create_Endpoint_PersistsAndReturns201()
    {
        var req = new JobDefinitionReq("EndpointCreateTest", "Created via endpoint") { Type = "Test", WorkerType = ProgrammingLanguageInfo.CSharp.ShortName };
        var response = await _client.PostAsJsonAsync("/api/Job/Definition", req, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CreateResult<JobDefinitionRes>>(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("EndpointCreateTest", result.Data!.Name);
        Assert.Contains("[afterCreate]", result.Data.Description ?? "");
    }

    [Fact]
    public async Task Create_BeforeCreate_SetsId()
    {
        var req = new JobDefinitionReq("BeforeCreateTest", "Test") { Type = "Test", WorkerType = ProgrammingLanguageInfo.CSharp.ShortName };
        var response = await _client.PostAsJsonAsync("/api/Job/Definition", req, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CreateResult<JobDefinitionRes>>(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.NotEqual(Guid.Empty, result.Data!.Id);
    }

    [Fact]
    public async Task Update_Endpoint_ModifiesEntity()
    {
        var defId = await _fixture.SeedJobDefinitionAsync("EndpointUpdateOriginal").ConfigureAwait(false);
        var req = new JobDefinitionReq("EndpointUpdateModified", "Updated via endpoint", false) { Type = "Test", WorkerType = ProgrammingLanguageInfo.CSharp.ShortName };
        var updateRequest = new UpdateRequest<JobDefinitionReq>(req, defId);
        var response = await _client.PostAsJsonAsync("/api/Job/Definition/Update", updateRequest, TestContext.Current.CancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<UpdateResult<JobDefinitionRes>>(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.True(result.Result is UpdateResultEnum.Updated or UpdateResultEnum.NoChange);
        Assert.NotNull(result.NewData);
        Assert.Equal("EndpointUpdateModified", result.NewData!.Name);
        Assert.Contains("Updated via endpoint", result.NewData.Description ?? "");
        Assert.Contains("[beforeUpdate]", result.NewData.Description ?? "");
        Assert.Contains("[afterUpdate]", result.NewData.Description ?? "");
        Assert.False(result.NewData.Enabled);
    }

    [Fact]
    public async Task Patch_Endpoint_ModifiesProperties()
    {
        var defId = await _fixture.SeedJobDefinitionAsync("EndpointPatchOriginal").ConfigureAwait(false);
        var patchRequest = new PatchRequest { Keys = [[defId]], Properties = new() { ["Name"] = "EndpointPatchModified" } };
        var response = await _client.PatchAsJsonAsync("/api/Job/Definition", patchRequest, TestContext.Current.CancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PatchResult<JobDefinitionRes>>(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.Equal(PatchResultEnum.Updated, result.Result);
        Assert.Equal("EndpointPatchModified", result.NewData!.Name);
        Assert.Contains("[beforePatch]", result.NewData.Description ?? "");
        Assert.Contains("[afterPatch]", result.NewData.Description ?? "");
    }

    [Fact]
    public async Task Delete_Endpoint_RemovesEntity()
    {
        var defId = await _fixture.SeedJobDefinitionAsync("EndpointDeleteTest").ConfigureAwait(false);
        var response = await _client.DeleteAsync($"/api/Job/Definition/{defId}", TestContext.Current.CancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var getResponse = await _client.GetAsync($"/api/Job/Definition/{defId}", TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_Endpoint_WhenNotFound_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/Job/Definition/{Guid.NewGuid()}", TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateBulk_Endpoint_PersistsAll()
    {
        var requests = new List<JobDefinitionReq> {
            new("EndpointBulk1") { Type = "Test", WorkerType = ProgrammingLanguageInfo.CSharp.ShortName },
            new("EndpointBulk2") { Type = "Test", WorkerType = ProgrammingLanguageInfo.CSharp.ShortName }
        };

        var response = await _client.PostAsJsonAsync("/api/Job/Definition/Bulk", requests, TestContext.Current.CancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreateBulkResult<JobDefinitionRes>>(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.Equal(2, result.CreatedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal(2, result.Results.Count);
        Assert.All(result.Results, r => Assert.Contains("[afterCreateBulk]", r.Data?.Description ?? ""));
    }

    [Fact]
    public async Task UpdateBulk_Endpoint_ModifiesEntities()
    {
        var id1 = await _fixture.SeedJobDefinitionAsync("EndpointUpdateBulk1").ConfigureAwait(false);
        var id2 = await _fixture.SeedJobDefinitionAsync("EndpointUpdateBulk2").ConfigureAwait(false);
        var requests = new List<UpdateRequest<JobDefinitionReq>> {
            new(new("EndpointUpdateBulk1Mod", "Mod1", false) { Type = "Test", WorkerType = ProgrammingLanguageInfo.CSharp.ShortName }, id1),
            new(new("EndpointUpdateBulk2Mod", "Mod2", false) { Type = "Test", WorkerType = ProgrammingLanguageInfo.CSharp.ShortName }, id2)
        };

        var response = await _client.PostAsJsonAsync("/api/Job/Definition/Bulk/Update", requests, TestContext.Current.CancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<UpdateBulkResult<JobDefinitionRes>>(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.Equal(2, result.UpdatedCount + result.NoChangeCount);
        Assert.Equal(0, result.FailedCount);
        var get1 = await _client.GetAsync($"/api/Job/Definition/{id1}", TestContext.Current.CancellationToken).ConfigureAwait(false);
        var get2 = await _client.GetAsync($"/api/Job/Definition/{id2}", TestContext.Current.CancellationToken).ConfigureAwait(false);
        get1.EnsureSuccessStatusCode();
        get2.EnsureSuccessStatusCode();
        var fetched1 = await get1.Content.ReadFromJsonAsync<JobDefinitionRes>(TestContext.Current.CancellationToken).ConfigureAwait(false);
        var fetched2 = await get2.Content.ReadFromJsonAsync<JobDefinitionRes>(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal("EndpointUpdateBulk1Mod", fetched1!.Name);
        Assert.Equal("EndpointUpdateBulk2Mod", fetched2!.Name);
        Assert.Contains("[beforeUpdateBulk]", fetched1.Description ?? "");
        // afterUpdateBulk runs after SaveChanges so it doesn't persist; beforeUpdateBulk does
    }

    [Fact]
    public async Task PatchBulk_Endpoint_ModifiesProperties()
    {
        var id1 = await _fixture.SeedJobDefinitionAsync("EndpointPatchBulk1").ConfigureAwait(false);
        var id2 = await _fixture.SeedJobDefinitionAsync("EndpointPatchBulk2").ConfigureAwait(false);
        var requests = new List<PatchRequest> {
            new() { Keys = [[id1]], Properties = new() { ["Name"] = "EndpointPatchBulk1Mod" } }, new() { Keys = [[id2]], Properties = new() { ["Name"] = "EndpointPatchBulk2Mod" } }
        };

        var response = await _client.PatchAsJsonAsync("/api/Job/Definition/Bulk", requests, TestContext.Current.CancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PatchBulkResult<JobDefinitionRes>>(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.Equal(2, result.UpdatedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Contains("[beforePatchBulk]", result.Results![0].NewData?.Description ?? "");
        Assert.Contains("[afterPatchBulk]", result.Results[0].NewData?.Description ?? "");
    }

    [Fact]
    public async Task Upsert_Endpoint_WhenNotExists_Creates()
    {
        var req = new JobDefinitionReq("EndpointUpsertCreate", "Upsert created") { Type = "Test", WorkerType = ProgrammingLanguageInfo.CSharp.ShortName };
        var upsertRequest = new UpsertRequest<JobDefinitionReq>(req, "Name", "EndpointUpsertCreate");
        var response = await _client.PostAsJsonAsync("/api/Job/Definition/Upsert", upsertRequest, TestContext.Current.CancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<UpsertResult<JobDefinitionRes>>(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.Equal(UpsertResultEnum.Created, result.Result);
        Assert.NotNull(result.NewData);
        Assert.Equal("EndpointUpsertCreate", result.NewData!.Name);
    }

    [Fact]
    public async Task Upsert_Endpoint_WhenExists_Updates()
    {
        var defId = await _fixture.SeedJobDefinitionAsync("EndpointUpsertOriginal").ConfigureAwait(false);
        var req = new JobDefinitionReq("EndpointUpsertUpdated", "Upsert updated", false) { Type = "Test", WorkerType = ProgrammingLanguageInfo.CSharp.ShortName };
        var upsertRequest = new UpsertRequest<JobDefinitionReq>(req, "Id", defId);
        var response = await _client.PostAsJsonAsync("/api/Job/Definition/Upsert", upsertRequest, TestContext.Current.CancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<UpsertResult<JobDefinitionRes>>(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.Equal(UpsertResultEnum.Updated, result.Result);
        Assert.NotNull(result.NewData);
        Assert.Equal("EndpointUpsertUpdated", result.NewData!.Name);
    }

    [Fact]
    public async Task UpsertBulk_Endpoint_CreatesAndUpdates()
    {
        var existingId = await _fixture.SeedJobDefinitionAsync("EndpointUpsertBulkExisting").ConfigureAwait(false);
        var requests = new List<UpsertRequest<JobDefinitionReq>> {
            new(new("EndpointUpsertBulkNew", "New") { Type = "Test", WorkerType = ProgrammingLanguageInfo.CSharp.ShortName }, "Name", "EndpointUpsertBulkNew"),
            new(new("EndpointUpsertBulkUpdated", "Updated", false) { Type = "Test", WorkerType = ProgrammingLanguageInfo.CSharp.ShortName }, "Id", existingId)
        };

        var response = await _client.PostAsJsonAsync("/api/Job/Definition/Bulk/Upsert", requests, TestContext.Current.CancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<UpsertBulkResult<JobDefinitionRes>>(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.Equal(1, result.CreatedCount);
        Assert.Equal(1, result.UpdatedCount);
    }

    [Fact]
    public async Task Delete_Endpoint_ByBody_RemovesEntity()
    {
        var defId = await _fixture.SeedJobDefinitionAsync("EndpointDeleteByBody").ConfigureAwait(false);
        var deleteRequest = new DeleteRequest { Keys = [[defId]] };
        using var msg = new HttpRequestMessage(HttpMethod.Delete, "/api/Job/Definition");
        msg.Content = JsonContent.Create(deleteRequest);
        var response = await _client.SendAsync(msg, TestContext.Current.CancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var getResponse = await _client.GetAsync($"/api/Job/Definition/{defId}", TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_Endpoint_ByIdentifiers_RemovesMatchingEntity()
    {
        var defId = await _fixture.SeedJobDefinitionAsync("EndpointDeleteByIdentifier").ConfigureAwait(false);
        var deleteRequest = new DeleteRequest("Name", "EndpointDeleteByIdentifier");
        using var msg = new HttpRequestMessage(HttpMethod.Delete, "/api/Job/Definition");
        msg.Content = JsonContent.Create(deleteRequest);
        var response = await _client.SendAsync(msg, TestContext.Current.CancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var getResponse = await _client.GetAsync($"/api/Job/Definition/{defId}", TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Update_Endpoint_WhenNotFound_Returns404()
    {
        var req = new JobDefinitionReq("NonExistent", "Test", false) { Type = "Test", WorkerType = ProgrammingLanguageInfo.CSharp.ShortName };
        var updateRequest = new UpdateRequest<JobDefinitionReq>(req, Guid.NewGuid());
        var response = await _client.PostAsJsonAsync("/api/Job/Definition/Update", updateRequest, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Patch_Endpoint_WhenNoChange_ReturnsOk()
    {
        var defId = await _fixture.SeedJobDefinitionAsync("EndpointPatchNoChange").ConfigureAwait(false);
        var patchRequest = new PatchRequest { Keys = [[defId]], Properties = new() { ["Name"] = "EndpointPatchNoChange" } };
        var response = await _client.PatchAsJsonAsync("/api/Job/Definition", patchRequest, TestContext.Current.CancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PatchResult<JobDefinitionRes>>(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.True(result.Result is PatchResultEnum.Updated or PatchResultEnum.NoChange);
    }

    [Fact]
    public async Task DeleteBulk_Endpoint_RemovesEntities()
    {
        var id1 = await _fixture.SeedJobDefinitionAsync("EndpointDeleteBulk1").ConfigureAwait(false);
        var id2 = await _fixture.SeedJobDefinitionAsync("EndpointDeleteBulk2").ConfigureAwait(false);
        var requests = new List<DeleteRequest> { new() { Keys = [[id1]] }, new() { Keys = [[id2]] } };
        using var msg = new HttpRequestMessage(HttpMethod.Delete, "/api/Job/Definition/Bulk");
        msg.Content = JsonContent.Create(requests);
        var response = await _client.SendAsync(msg, TestContext.Current.CancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<DeleteBulkResult<JobDefinitionRes>>(TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.NotNull(result);
        Assert.Equal(2, result.DeletedCount);
        Assert.Equal(0, result.FailedCount);
    }
}