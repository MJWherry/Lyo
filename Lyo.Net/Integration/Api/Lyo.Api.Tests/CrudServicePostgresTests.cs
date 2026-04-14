using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Enums;
using Lyo.Api.Services.Crud.Create;
using Lyo.Api.Services.Crud.Delete;
using Lyo.Api.Services.Crud.Read;
using Lyo.Api.Services.Crud.Read.Query;
using Lyo.Api.Services.Crud.Update;
using Lyo.Api.Tests.Fixtures;
using Lyo.Common.Records;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Job.Postgres.Database;

namespace Lyo.Api.Tests;

[Collection(ApiPostgresCollection.Name)]
public class CrudServicePostgresTests
{
    private readonly ApiPostgresFixture _fixture;

    public CrudServicePostgresTests(ApiPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Create_JobDefinition_PersistsAndReturns()
    {
        using var scope = _fixture.ServiceProvider.CreateScope();
        var createService = scope.ServiceProvider.GetRequiredService<ICreateService<JobContext>>();
        var req = new JobDefinitionReq("CreateTest", "Created by test") { Type = "Test", WorkerType = ProgrammingLanguageInfo.CSharp.ShortName };
        var result = await createService.CreateAsync<JobDefinitionReq, JobDefinition, JobDefinitionRes>(
            req, ctx => {
                ctx.Entity.Id = Guid.NewGuid();
                ctx.Entity.Type = "Test";
                ctx.Entity.WorkerType = ProgrammingLanguageInfo.CSharp.ShortName;
            }, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.NotEqual(Guid.Empty, result.Data!.Id);
        Assert.Equal("CreateTest", result.Data.Name);
    }

    [Fact]
    public async Task CreateBulk_JobDefinitions_PersistsAll()
    {
        using var scope = _fixture.ServiceProvider.CreateScope();
        var createService = scope.ServiceProvider.GetRequiredService<ICreateService<JobContext>>();
        var requests = new List<JobDefinitionReq> {
            new("Bulk1") { Type = "Test", WorkerType = ProgrammingLanguageInfo.CSharp.ShortName },
            new("Bulk2") { Type = "Test", WorkerType = ProgrammingLanguageInfo.CSharp.ShortName }
        };

        var result = await createService.CreateBulkAsync<JobDefinitionReq, JobDefinition, JobDefinitionRes>(
            requests, ctx => {
                ctx.Entity.Id = Guid.NewGuid();
                ctx.Entity.Type = "Test";
                ctx.Entity.WorkerType = ProgrammingLanguageInfo.CSharp.ShortName;
            }, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);

        Assert.Equal(2, result.CreatedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal(2, result.Results.Count);
    }

    [Fact]
    public async Task Update_JobDefinition_ModifiesEntity()
    {
        var defId = await _fixture.SeedJobDefinitionAsync("UpdateOriginal").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var updateService = scope.ServiceProvider.GetRequiredService<IUpdateService<JobContext>>();
        var req = new JobDefinitionReq("UpdateModified", "Updated description", false) { Type = "Test", WorkerType = ProgrammingLanguageInfo.CSharp.ShortName };
        var updateRequest = new UpdateRequest<JobDefinitionReq>(req, defId);
        var result = await updateService.UpdateAsync<JobDefinitionReq, JobDefinition, JobDefinitionRes>(updateRequest, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(result.Result is UpdateResultEnum.Updated or UpdateResultEnum.NoChange);
        Assert.NotNull(result.NewData);
        Assert.Equal("UpdateModified", result.NewData!.Name);
        Assert.Equal("Updated description", result.NewData.Description);
        Assert.False(result.NewData.Enabled);
    }

    [Fact]
    public async Task Patch_JobDefinition_ModifiesSpecifiedProperties()
    {
        var defId = await _fixture.SeedJobDefinitionAsync("PatchOriginal").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var patchService = scope.ServiceProvider.GetRequiredService<IPatchService<JobContext>>();
        var patchRequest = new PatchRequest { Keys = [[defId]], Properties = new() { ["Name"] = "PatchModified", ["Description"] = "Patched" } };
        var result = await patchService.PatchAsync<JobDefinition, JobDefinitionRes>(patchRequest, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(PatchResultEnum.Updated, result.Result);
        Assert.NotNull(result.NewData);
        Assert.Equal("PatchModified", result.NewData!.Name);
        Assert.Equal("Patched", result.NewData.Description);
    }

    [Fact]
    public async Task Delete_ByKey_RemovesEntity()
    {
        var defId = await _fixture.SeedJobDefinitionAsync("DeleteTest").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var deleteService = scope.ServiceProvider.GetRequiredService<IDeleteService<JobContext>>();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<JobContext>>();
        var deleteResult = await deleteService.DeleteAsync<JobDefinition, JobDefinitionRes>([defId], ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(deleteResult.IsSuccess);
        var getResult = await queryService.Get<JobDefinition, JobDefinitionRes>([defId], null, null, null, TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Null(getResult);
    }

    [Fact]
    public async Task Delete_ByIdentifier_RemovesMatchingEntity()
    {
        var defId = await _fixture.SeedJobDefinitionAsync("DeleteByIdentifier").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var deleteService = scope.ServiceProvider.GetRequiredService<IDeleteService<JobContext>>();
        var deleteRequest = new DeleteRequest("Name", "DeleteByIdentifier");
        var deleteResult = await deleteService.DeleteAsync<JobDefinition, JobDefinitionRes>(deleteRequest, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.True(deleteResult.IsSuccess);
    }

    [Fact]
    public async Task Upsert_WhenNotExists_Creates()
    {
        using var scope = _fixture.ServiceProvider.CreateScope();
        var upsertService = scope.ServiceProvider.GetRequiredService<IUpsertService<JobContext>>();
        var req = new JobDefinitionReq("UpsertCreate", "Upsert created") { Type = "Test", WorkerType = ProgrammingLanguageInfo.CSharp.ShortName };
        var upsertRequest = new UpsertRequest<JobDefinitionReq>(req, "Name", "UpsertCreate");
        var result = await upsertService.UpsertAsync<JobDefinitionReq, JobDefinition, JobDefinitionRes>(
            upsertRequest, beforeCreate: ctx => {
                ctx.Entity.Id = Guid.NewGuid();
                ctx.Entity.Type = "Test";
                ctx.Entity.WorkerType = ProgrammingLanguageInfo.CSharp.ShortName;
            }, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);

        Assert.Equal(UpsertResultEnum.Created, result.Result);
        Assert.NotNull(result.NewData);
        Assert.Equal("UpsertCreate", result.NewData!.Name);
    }

    [Fact]
    public async Task Upsert_WhenExists_Updates()
    {
        var defId = await _fixture.SeedJobDefinitionAsync("UpsertOriginal").ConfigureAwait(false);
        using var scope = _fixture.ServiceProvider.CreateScope();
        var upsertService = scope.ServiceProvider.GetRequiredService<IUpsertService<JobContext>>();
        var req = new JobDefinitionReq("UpsertUpdated", "Upsert updated desc", false) { Type = "Test", WorkerType = ProgrammingLanguageInfo.CSharp.ShortName };
        var upsertRequest = new UpsertRequest<JobDefinitionReq>(req, "Id", defId);
        var result = await upsertService.UpsertAsync<JobDefinitionReq, JobDefinition, JobDefinitionRes>(upsertRequest, ct: TestContext.Current.CancellationToken).ConfigureAwait(false);
        Assert.Equal(UpsertResultEnum.Updated, result.Result);
        Assert.NotNull(result.NewData);
        Assert.Equal(defId, result.NewData!.Id);
        Assert.Equal("UpsertUpdated", result.NewData.Name);
    }
}