using Lyo.Common.Metadata.Records;
using Lyo.Parameters;
using Lyo.Reporting.Models.Enums;
using Lyo.Reporting.Models.Request;
using Lyo.Reporting.Models.Response;
using Lyo.Reporting.Postgres;
using Lyo.Reporting.Postgres.Database;
using Lyo.Reporting.Postgres.Mapping;

namespace Lyo.Reporting.Tests;

public sealed class ReportingLyoMapperTests
{
    private readonly ReportingLyoMapper _mapper = new();

    [Fact]
    public void Map_DefinitionReqWithCreateParameters_MapsEntity()
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
                new() {
                    Key = "ClientId",
                    Type = LyoTypeInfo.Guid.FullName,
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
        var res = _mapper.Map<ReportDefinitionRes>(entity);
        Assert.Equal(entity.Id, res.Id);
        Assert.NotNull(res.Parameters);
        Assert.Single(res.Parameters!);
        Assert.Equal("ClientId", res.Parameters![0].Key);
    }

    [Fact]
    public void Map_GenerationWithParameters_MapsEntity()
    {
        var req = new ReportGenerationReq {
            ReportDataJson = "{\"Title\":\"t\"}",
            Format = ReportFormat.Csv,
            Status = ReportGenerationStatus.Succeeded,
            CreatedBy = "worker",
            Parameters = [new("ClientId", LyoTypeInfo.Guid, LyoTypeInfo.Guid.ToJson(Guid.NewGuid()))]
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

        var res = _mapper.Map<ReportGenerationRes>(entity);
        Assert.Equal(ReportFormat.Csv, res.Format);
        Assert.NotNull(res.Parameters);
        Assert.Single(res.Parameters!);
    }

    [Fact]
    public void MergeParameters_DefaultsAndOverrides_Applies()
    {
        var defParams = new List<ReportDefinitionParameter> {
            new() {
                Key = "A",
                Type = LyoTypeInfo.String.FullName,
                Value = "default-a",
                Required = true
            },
            new() {
                Key = "B",
                Type = LyoTypeInfo.Int.FullName,
                Value = "1",
                Required = false
            }
        };

        var merged = ReportService.MergeParameters(defParams, [new("A", LyoTypeInfo.String, LyoTypeInfo.String.ToJson("override-a"))]);
        Assert.Equal(2, merged.Count);
        Assert.Equal(LyoTypeInfo.String.ToJson("override-a"), merged.First(p => p.Key == "A").Value);
        Assert.Equal("1", merged.First(p => p.Key == "B").Value);
    }

    [Fact]
    public void MergeParameters_ExpressionDefault_ResolvesToDeclaredType()
    {
        var merged = ReportService.MergeParameters([YesterdayParameter()], [], _ => "2026-08-25");
        var asOfDate = Assert.Single(merged);

        // The generation snapshot keeps Type = DateTime, so the merged value has to be the rendered date instead of the template.
        Assert.Equal(LyoTypeInfo.DateTime.FullName, asOfDate.Type);
        Assert.Equal("\"2026-08-25\"", asOfDate.Value);
    }

    [Fact]
    public void MergeParameters_SuppliedValue_PrefersOverExpressionDefault()
    {
        var merged = ReportService.MergeParameters(
            [YesterdayParameter()], [new("AsOfDate", LyoTypeInfo.DateTime, LyoTypeInfo.DateTime.ToJson(new DateTime(2020, 1, 1)))], _ => "2026-08-25");

        Assert.Equal(LyoTypeInfo.DateTime.ToJson(new DateTime(2020, 1, 1)), Assert.Single(merged).Value);
    }

    [Fact]
    public void MergeParameters_ExpressionDefaultWithoutResolver_Reports()
    {
        var errors = new List<string>();
        Assert.Empty(ReportService.MergeParameters([YesterdayParameter()], [], errors: errors));
        Assert.Contains(errors, e => e.Contains("no template resolver is registered", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MergeParameters_ExpressionDefaultWrongType_Reports()
    {
        var errors = new List<string>();
        Assert.Empty(ReportService.MergeParameters([YesterdayParameter()], [], _ => "not a date", errors));
        Assert.Contains(errors, e => e.Contains("not a valid System.DateTime", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Map_DefinitionParameter_RoundTripsDefaultChannel()
    {
        var req = new ReportDefinitionParameterReq {
            Key = "AsOfDate",
            Type = LyoTypeInfo.DateTime.FullName,
            DefaultKind = LyoParameterDefaultKind.Expression,
            DefaultTemplate = "{DateTime.UtcNow.AddDays(-1):yyyy-MM-dd}"
        };

        var entity = _mapper.Map<ReportDefinitionParameter>(req);
        Assert.Equal(nameof(LyoParameterDefaultKind.Expression), entity.DefaultKind);
        Assert.Equal(req.DefaultTemplate, entity.DefaultTemplate);

        var res = _mapper.Map<ReportDefinitionParameterRes>(entity);
        Assert.Equal(LyoParameterDefaultKind.Expression, res.DefaultKind);
        Assert.Equal(req.DefaultTemplate, res.DefaultTemplate);
    }

    [Fact]
    public void Map_UnreadableDefaultColumn_DefaultsToLiteral()
    {
        // Rows written before the column existed, or by a client sending garbage, cannot be read as expression-authored.
        var res = _mapper.Map<ReportDefinitionParameterRes>(
            new ReportDefinitionParameter {
                Key = "Code",
                Type = LyoTypeInfo.String.FullName,
                DefaultKind = "nonsense"
            });

        Assert.Equal(LyoParameterDefaultKind.Literal, res.DefaultKind);
    }

    private static ReportDefinitionParameter YesterdayParameter()
        => new() {
            Key = "AsOfDate",
            Type = LyoTypeInfo.DateTime.FullName,
            DefaultKind = nameof(LyoParameterDefaultKind.Expression),
            DefaultTemplate = "{DateTime.UtcNow.AddDays(-1):yyyy-MM-dd}"
        };
}