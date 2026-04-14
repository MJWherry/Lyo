using Lyo.Api.Mapping;
using Lyo.Api.Models;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Error;
using Lyo.Metrics;
using Lyo.Metrics.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Api.Services.Crud;

public abstract class BaseService<TContext>(IDbContextFactory<TContext> contextFactory, ILyoMapper mapper, ILogger? logger, IMetrics? metrics = null)
    where TContext : DbContext
{
    protected readonly IDbContextFactory<TContext> ContextFactory = contextFactory;
    protected readonly ILogger Logger = logger ?? NullLogger.Instance;
    protected readonly ILyoMapper Mapper = mapper;
    protected readonly IMetrics Metrics = metrics ?? NullMetrics.Instance;

    protected IDisposable? BeginActionScope(string action, Type? requestType, Type databaseType, Type resultType)
        => requestType is null
            ? Logger.BeginScope("{ApiAction} {DatabaseContextName} {DatabaseType} {ResponseType}", action, typeof(TContext).Name, databaseType.FullName, resultType.FullName)
            : Logger.BeginScope(
                "{ApiAction} {DatabaseContextName} {RequestType} {DatabaseType} {ResponseType}", action, typeof(TContext).Name, requestType.FullName, databaseType.FullName,
                resultType.FullName);

    protected LyoProblemDetails LogAndReturnApiError(Exception ex, string message, string code = Constants.ApiErrorCodes.Unknown, LogLevel level = LogLevel.Warning)
    {
        Logger.Log(level, ex, message);
        return LyoProblemDetailsBuilder.FromException(ex, code).Build();
    }

    protected LyoProblemDetails LogAndReturnApiError(string message, string code = Constants.ApiErrorCodes.Unknown, LogLevel level = LogLevel.Warning)
    {
        Logger.Log(level, message);
        return LyoProblemDetailsBuilder.CreateWithActivity().WithErrorCode(code).WithMessage(message).Build();
    }

    protected static LyoProblemDetails CreateNotFoundError<TDbModel>(IEnumerable<object>? identifiers = null)
        => LyoProblemDetailsBuilder.CreateWithActivity()
            .WithErrorCode(Constants.ApiErrorCodes.NotFound)
            .WithMessage(
                $"Entity not found.{Environment.NewLine}DatabaseType={typeof(TDbModel).FullName}{Environment.NewLine}Identifiers={string.Join(',', identifiers ?? [])}")
            .Build();

    protected MetricsTimer StartCrudTimer(string operation, Type databaseType, bool isBulk = false)
        => Metrics.StartTimer("api.crud.duration", CrudTags(operation, databaseType, isBulk));

    protected void RecordCrudRequest(string operation, Type databaseType, bool isBulk = false)
        => Metrics.IncrementCounter("api.crud.requests", 1, CrudTags(operation, databaseType, isBulk));

    protected void RecordCrudSuccess(string operation, Type databaseType, bool isBulk = false)
        => Metrics.IncrementCounter("api.crud.success", 1, CrudTags(operation, databaseType, isBulk));

    protected void RecordCrudFailure(string operation, Type databaseType, bool isBulk = false)
        => Metrics.IncrementCounter("api.crud.failure", 1, CrudTags(operation, databaseType, isBulk));

    protected void RecordCrudCancelled(string operation, Type databaseType, bool isBulk = false)
        => Metrics.IncrementCounter("api.crud.cancelled", 1, CrudTags(operation, databaseType, isBulk));

    protected void RecordCrudResultCount(string operation, Type databaseType, int count, bool isBulk = false)
        => Metrics.RecordGauge("api.crud.result_count", count, CrudTags(operation, databaseType, isBulk));

    private IEnumerable<(string, string)> CrudTags(string operation, Type databaseType, bool isBulk)
        => [("operation", operation), ("context", typeof(TContext).Name), ("database_type", databaseType.Name), ("is_bulk", isBulk ? "true" : "false")];

    /// <summary>Maps source to TResult, or casts when TSource and TResult are the same type (skips mapping).</summary>
    protected static TResult MapOrCast<TSource, TResult>(ILyoMapper mapper, TSource source)
    {
        if (typeof(TSource) == typeof(TResult))
            return (TResult)(object)source!;

        return mapper.Map<TResult>(source);
    }
}