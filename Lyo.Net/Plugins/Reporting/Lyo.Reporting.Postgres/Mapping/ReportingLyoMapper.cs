using Lyo.Api.Mapping;
using Lyo.Common.Core.Conversion;
using Lyo.Parameters;
using Lyo.Common.Metadata.Records;
using Lyo.Reporting.Models.Enums;
using Lyo.Reporting.Models.Request;
using Lyo.Reporting.Models.Response;
using Lyo.Reporting.Postgres.Database;

namespace Lyo.Reporting.Postgres.Mapping;

/// <summary>Hand-rolled <see cref="ILyoMapper" /> for reporting Req, entity, and Res types.</summary>
public sealed class ReportingLyoMapper : ILyoMapper
{
    public TResult Map<TResult>(object source)
        => source switch {
            ReportDefinitionReq req when typeof(TResult) == typeof(ReportDefinition) => (TResult)(object)ReqToNew(req),
            ReportDefinitionParameterReq req when typeof(TResult) == typeof(ReportDefinitionParameter) => (TResult)(object)ReqToNew(req),
            ReportGenerationReq req when typeof(TResult) == typeof(ReportGeneration) => (TResult)(object)ReqToNew(req),
            ReportGenerationParameterReq req when typeof(TResult) == typeof(ReportGenerationParameter) => (TResult)(object)ReqToNew(req),
            ReportDefinition e when typeof(TResult) == typeof(ReportDefinitionRes) => (TResult)(object)ToRes(e),
            ReportDefinitionParameter e when typeof(TResult) == typeof(ReportDefinitionParameterRes) => (TResult)(object)ToRes(e),
            ReportGeneration e when typeof(TResult) == typeof(ReportGenerationRes) => (TResult)(object)ToRes(e),
            ReportGenerationParameter e when typeof(TResult) == typeof(ReportGenerationParameterRes) => (TResult)(object)ToRes(e),
            var _ => throw Unmapped(source.GetType(), typeof(TResult))
        };

    public void Map<TSource, TDest>(TSource source, TDest destination)
    {
        switch (source, destination) {
            case (ReportDefinitionReq req, ReportDefinition e):
                Apply(req, e);
                break;
            case (ReportDefinitionParameterReq req, ReportDefinitionParameter e):
                Apply(req, e);
                break;
            case (ReportGenerationReq req, ReportGeneration e):
                Apply(req, e);
                break;
            case (ReportGenerationParameterReq req, ReportGenerationParameter e):
                Apply(req, e);
                break;
            default:
                throw Unmapped(typeof(TSource), typeof(TDest));
        }
    }

    internal static ReportDefinition ReqToNew(ReportDefinitionReq req)
    {
        var entity = new ReportDefinition();
        Apply(req, entity);
        entity.Parameters = req.CreateParameters.Select(ReqToNew).ToList();
        return entity;
    }

    internal static void Apply(ReportDefinitionReq req, ReportDefinition entity)
    {
        entity.Name = req.Name;
        entity.Description = req.Description;
        entity.ReportDataJson = req.ReportDataJson;
        entity.Tags = req.Tags;
        entity.IsActive = req.IsActive;
        entity.DefaultFormat = req.DefaultFormat?.ToString();
        entity.DefaultFileName = req.DefaultFileName;
        entity.DefaultPathPrefix = req.DefaultPathPrefix;
        entity.GenerationProfileKey = req.GenerationProfileKey;
    }

    internal static ReportDefinitionParameter ReqToNew(ReportDefinitionParameterReq req)
    {
        var entity = new ReportDefinitionParameter();
        Apply(req, entity);
        return entity;
    }

    internal static void Apply(ReportDefinitionParameterReq req, ReportDefinitionParameter entity)
    {
        entity.ReportDefinitionId = req.ReportDefinitionId;
        entity.Key = req.Key;
        entity.Description = req.Description;
        entity.Type = LyoTypeInfo.NormalizeFullName(req.Type);
        entity.Value = req.Value;
        entity.EncryptedValue = req.EncryptedValue;
        entity.Required = req.Required;
        entity.ValidationRegex = req.ValidationRegex;
        entity.MinLength = req.MinLength;
        entity.MaxLength = req.MaxLength;
        entity.AllowedValues = req.AllowedValues;
        entity.Options = req.Options;
        entity.DefaultKind = req.DefaultKind.ToString();
        entity.DefaultTemplate = req.DefaultTemplate;
    }

    internal static ReportDefinitionRes ToRes(ReportDefinition e)
        => new(
            e.Id, e.Name, e.Description, e.ReportDataJson, e.Tags, e.IsActive, ParseFormatNullable(e.DefaultFormat), e.DefaultFileName, e.DefaultPathPrefix, e.GenerationProfileKey,
            e.CreatedBy, e.CreatedTimestamp, e.UpdatedTimestamp, e.Parameters.Select(ToRes).ToList());

    internal static ReportDefinitionParameterRes ToRes(ReportDefinitionParameter e)
        => new(
            e.Id, e.ReportDefinitionId, e.Key, e.Description, LyoTypeInfo.NormalizeFullName(e.Type), LyoParameterMapper.MaskValue(e.Value, e.EncryptedValue),
            LyoParameterMapper.MaskEncryptedValue(e.EncryptedValue), e.Required, e.ValidationRegex, e.MinLength, e.MaxLength, e.AllowedValues, e.Options, e.CreatedTimestamp,
            e.UpdatedTimestamp, TypeConversion.EnumOrDefault<LyoParameterDefaultKind>(e.DefaultKind), e.DefaultTemplate);

    internal static ReportGeneration ReqToNew(ReportGenerationReq req)
    {
        var entity = new ReportGeneration();
        Apply(req, entity);
        entity.Parameters = req.Parameters.Select(ReqToNew).ToList();
        return entity;
    }

    internal static void Apply(ReportGenerationReq req, ReportGeneration entity)
    {
        entity.ReportDefinitionId = req.ReportDefinitionId;
        entity.ReportDataJson = req.ReportDataJson;
        entity.Format = req.Format.ToString();
        entity.Status = req.Status.ToString();
        entity.OutputFileId = req.OutputFileId;
        entity.OriginalFileName = req.OriginalFileName;
        entity.ContentType = req.ContentType;
        entity.ErrorMessage = req.ErrorMessage;
        entity.PathPrefix = req.PathPrefix;
        if (!string.IsNullOrWhiteSpace(req.CreatedBy))
            entity.CreatedBy = TruncateCreatedBy(req.CreatedBy!);
    }

    internal static ReportGenerationParameter ReqToNew(ReportGenerationParameterReq req)
    {
        var entity = new ReportGenerationParameter();
        Apply(req, entity);
        return entity;
    }

    internal static void Apply(ReportGenerationParameterReq req, ReportGenerationParameter entity)
    {
        entity.Key = req.Key;
        entity.Description = req.Description;
        entity.Type = LyoTypeInfo.NormalizeFullName(req.Type);
        entity.Value = req.Value;
        entity.EncryptedValue = req.EncryptedValue;
    }

    /// <param name="e">Generation entity to map.</param>
    /// <param name="includeReportData">
    /// When false, <see cref="ReportGenerationRes.ReportDataJson" /> is omitted. Generate and rerun default to false so a multi-megabyte composition is not echoed back on every
    /// call. The query/get projection keeps it.
    /// </param>
    internal static ReportGenerationRes ToRes(ReportGeneration e, bool includeReportData = true)
        => new(
            e.Id, e.ReportDefinitionId, includeReportData ? e.ReportDataJson : null, ParseFormat(e.Format), ParseStatus(e.Status), e.OutputFileId, e.OriginalFileName,
            e.ContentType, e.ErrorMessage, e.PathPrefix, e.CreatedBy, e.CreatedTimestamp, e.StartedTimestamp, e.FinishedTimestamp, e.Parameters.Select(ToRes).ToList());

    internal static ReportGenerationParameterRes ToRes(ReportGenerationParameter e)
        => new(
            e.Id, e.ReportGenerationId, e.Key, LyoTypeInfo.NormalizeFullName(e.Type), LyoParameterMapper.MaskValue(e.Value, e.EncryptedValue), e.Description,
            LyoParameterMapper.MaskEncryptedValue(e.EncryptedValue));

    private static ReportFormat ParseFormat(string value) => TypeConversion.EnumOrDefault(value, ReportFormat.Html);

    private static ReportFormat? ParseFormatNullable(string? value) => string.IsNullOrWhiteSpace(value) ? null : TypeConversion.EnumOrNull<ReportFormat>(value);

    private static ReportGenerationStatus ParseStatus(string value) => TypeConversion.EnumOrDefault(value, ReportGenerationStatus.Pending);

    internal static string TruncateCreatedBy(string value) => value.Length > 50 ? value[..50] : value;

    private static InvalidOperationException Unmapped(Type source, Type dest) => new($"No mapping registered from {source.Name} to {dest.Name}.");
}