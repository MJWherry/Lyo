using Lyo.Api.Models;
using Lyo.Common;
using Lyo.Query.Models.Common;
using Lyo.Query.Services.WhereClause;
using Lyo.Validation;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.Services.Crud.Validation;

/// <summary>Single <see cref="Lyo.Validation" /> entry point for include, sort, and where-clause paths on a projected entity query.</summary>
public static class ProjectedQueryModelValidator
{
    public static Result<ProjectedQueryValidatorInput<TContext, TDbModel>> Validate<TContext, TDbModel>(
        ProjectedQueryValidatorInput<TContext, TDbModel> input)
        where TContext : DbContext
        where TDbModel : class
        => Holder<TContext, TDbModel>.Instance.Validate(input);

    private static class Holder<TContext, TDbModel>
        where TContext : DbContext
        where TDbModel : class
    {
        internal static readonly IValidator<ProjectedQueryValidatorInput<TContext, TDbModel>> Instance =
            ValidatorBuilder<ProjectedQueryValidatorInput<TContext, TDbModel>>.Create()
                .Custom(Collect)
                .Build();

        private static IReadOnlyList<Error> Collect(ProjectedQueryValidatorInput<TContext, TDbModel> input)
        {
            var errors = new List<Error>();
            var pathCache = input.PathCache;
            if (input.Include.Count > 0) {
                if (input.Db is null)
                    errors.Add(new Error("Database context is required to validate include paths.", Constants.ApiErrorCodes.InvalidRequest));
                else {
                    foreach (var include in input.Include.Where(i => !string.IsNullOrWhiteSpace(i))) {
                        if (!pathCache.TryValidateInclude<TContext, TDbModel>(input.Loader, input.Db, include, out var includeMsg))
                            errors.Add(new Error(includeMsg!, Constants.ApiErrorCodes.InvalidInclude));
                    }
                }
            }

            errors.AddRange(CollectSortByFieldErrors<TDbModel>(pathCache, input.Filter, input.SortBy));
            errors.AddRange(CollectWhereFieldErrors<TDbModel>(pathCache, input.Filter, input.Where));
            return errors;
        }
    }

    private static List<Error> CollectSortByFieldErrors<TDbModel>(QueryPathValidationCache pathCache, IWhereClauseService filter, IReadOnlyList<SortBy> sortBy)
        where TDbModel : class
    {
        var errors = new List<Error>();
        foreach (var s in sortBy) {
            var name = s.PropertyName;
            if (string.IsNullOrWhiteSpace(name)) {
                errors.Add(new Error(
                    $"Sort entry is missing a property name for type '{typeof(TDbModel).Name}'.",
                    Constants.ApiErrorCodes.InvalidSortByField));
                continue;
            }

            if (!pathCache.TryValidateFilterPropertyPath<TDbModel>(filter, name, out var message))
                errors.Add(new Error(FormatSortMessage<TDbModel>(name, message), Constants.ApiErrorCodes.InvalidSortByField));
        }

        return errors;
    }

    private static string FormatSortMessage<TDbModel>(string propertyName, string? resolverMessage)
        where TDbModel : class
    {
        var detail = string.IsNullOrWhiteSpace(resolverMessage) ? string.Empty : $" {resolverMessage}";
        return $"Sort field {ValidationFieldFormatter.Quote(propertyName)} is not valid for type '{typeof(TDbModel).Name}'.{detail}";
    }

    private static List<Error> CollectWhereFieldErrors<TDbModel>(QueryPathValidationCache pathCache, IWhereClauseService filter, WhereClause? root)
        where TDbModel : class
    {
        var errors = new List<Error>();
        VisitWhere(root);
        return errors;

        void VisitWhere(WhereClause? node)
        {
            if (node is null)
                return;

            switch (node) {
                case ConditionClause c:
                    AddFieldErrorIfInvalid(c.Field);
                    VisitWhere(c.SubClause);
                    break;
                case GroupClause g:
                    foreach (var child in g.Children)
                        VisitWhere(child);

                    VisitWhere(g.SubClause);
                    break;
                default:
                    VisitWhere(node.SubClause);
                    break;
            }
        }

        void AddFieldErrorIfInvalid(string field)
        {
            if (string.IsNullOrWhiteSpace(field)) {
                errors.Add(new Error(
                    $"Where clause condition is missing a field name for type '{typeof(TDbModel).Name}'.",
                    Constants.ApiErrorCodes.InvalidWhereField));
                return;
            }

            if (!pathCache.TryValidateFilterPropertyPath<TDbModel>(filter, field, out var message))
                errors.Add(new Error(FormatWhereMessage<TDbModel>(field, message), Constants.ApiErrorCodes.InvalidWhereField));
        }
    }

    private static string FormatWhereMessage<TDbModel>(string field, string? resolverMessage)
        where TDbModel : class
    {
        var detail = string.IsNullOrWhiteSpace(resolverMessage) ? string.Empty : $" {resolverMessage}";
        return $"Where field {ValidationFieldFormatter.Quote(field)} is not valid for type '{typeof(TDbModel).Name}'.{detail}";
    }
}

/// <summary>Input to <see cref="ProjectedQueryModelValidator.Validate{TContext,TDbModel}" />. <see cref="Db" /> is required when <see cref="Include" /> is non-empty (QueryProject passes includes derived from projection, not raw client includes).</summary>
public sealed class ProjectedQueryValidatorInput<TContext, TDbModel>
    where TContext : DbContext
    where TDbModel : class
{
    public required TContext? Db { get; init; }

    public required IEntityLoaderService Loader { get; init; }

    public required IWhereClauseService Filter { get; init; }

    /// <summary>Per-request path validation cache (filter paths, select normalization, includes).</summary>
    public required QueryPathValidationCache PathCache { get; init; }

    public required IReadOnlyList<string> Include { get; init; }

    public required IReadOnlyList<SortBy> SortBy { get; init; }

    public WhereClause? Where { get; init; }
}
