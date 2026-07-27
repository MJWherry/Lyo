using Lyo.Reporting.Models.Enums;
using Lyo.Reporting.Models.Request;
using Lyo.Reporting.Postgres;
using Lyo.Reporting.Postgres.Database;
using Lyo.Reporting.Postgres.Mapping;

namespace Lyo.Reporting.Tests;

public sealed class ReportingLyoMapperTests
{
    private readonly ReportingLyoMapper _mapper = new();

    [Fact]
    public void Maps_definition_req_with_create_parameters()
    {
        var req = new ReportDefinitionReq {
            Name = "N",
            Description = "D",
            ReportDataJson = "{}",
            Tags = "a,b",
            IsActive = true,
            DefaultFormat = ReportFormat.Csv,
            GenerationProfileKey = "profile-a",
            CreateParameters = [
                new ReportDefinitionParameterReq {
                    Key = "ClientId",
                    Type = ReportParameterType.Guid,
                    Required = true,
                    Value = null
                }
            ]
        };

        var entity = _mapper.Map<ReportDefinition>(req);
        entity.Id = Guid.NewGuid();
        entity.CreatedTimestamp = DateTime.UtcNow;
        entity.UpdatedTimestamp = entity.CreatedTimestamp;
        entity.CreatedBy = "tester";
        foreach (var p in entity.Parameters) {
            p.Id = Guid.NewGuid();
            p.ReportDefinitionId = entity.Id;
            p.CreatedTimestamp = entity.CreatedTimestamp;
        }

        Assert.Equal("N", entity.Name);
        Assert.Single(entity.Parameters);
        Assert.Equal("ClientId", entity.Parameters[0].Key);

        var res = _mapper.Map<Lyo.Reporting.Models.Response.ReportDefinitionRes>(entity);
        Assert.Equal(entity.Id, res.Id);
        Assert.NotNull(res.Parameters);
        Assert.Single(res.Parameters!);
        Assert.Equal("ClientId", res.Parameters![0].Key);
    }

    [Fact]
    public void Maps_generation_with_parameters()
    {
        var req = new ReportGenerationReq {
            ReportDataJson = "{\"Title\":\"t\"}",
            Format = ReportFormat.Csv,
            Status = ReportGenerationStatus.Succeeded,
            CreatedBy = "worker",
            Parameters = [
                new ReportGenerationParameterReq("ClientId", ReportParameterType.Guid, Guid.NewGuid().ToString())
            ]
        };

        var entity = _mapper.Map<ReportGeneration>(req);
        Assert.Equal(nameof(ReportFormat.Csv), entity.Format);
        Assert.Single(entity.Parameters);

        entity.Id = Guid.NewGuid();
        entity.CreatedTimestamp = DateTime.UtcNow;
        foreach (var p in entity.Parameters) {
            p.Id = Guid.NewGuid();
            p.ReportGenerationId = entity.Id;
        }

        var res = _mapper.Map<Lyo.Reporting.Models.Response.ReportGenerationRes>(entity);
        Assert.Equal(ReportFormat.Csv, res.Format);
        Assert.NotNull(res.Parameters);
        Assert.Single(res.Parameters!);
    }

    [Fact]
    public void MergeParameters_applies_defaults_and_overrides()
    {
        var defParams = new List<ReportDefinitionParameter> {
            new() {
                Key = "A",
                Type = nameof(ReportParameterType.String),
                Value = "default-a",
                Required = true
            },
            new() {
                Key = "B",
                Type = nameof(ReportParameterType.Int),
                Value = "1",
                Required = false
            }
        };

        var merged = ReportService.MergeParameters(
            defParams,
            [new ReportGenerationParameterReq("A", ReportParameterType.String, "override-a")]);

        Assert.Equal(2, merged.Count);
        Assert.Equal("override-a", merged.First(p => p.Key == "A").Value);
        Assert.Equal("1", merged.First(p => p.Key == "B").Value);
    }
}
