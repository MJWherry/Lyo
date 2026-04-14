using System.Linq.Expressions;
using System.Reflection;
using Lyo.Api.ApiEndpoint.Config;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.ApiEndpoint;

/// <summary>Cached reflection for dynamic endpoint handlers. Built once at startup per entity type.</summary>
internal sealed record DynamicMethodCache(
    Delegate? BeforeCreateDelegate,
    MethodInfo Query,
    MethodInfo QueryProjected,
    MethodInfo Get,
    MethodInfo CreateAsync,
    MethodInfo CreateBulkAsync,
    MethodInfo PatchAsync,
    MethodInfo PatchBulkAsync,
    MethodInfo DeleteAsync,
    MethodInfo DeleteByRequestAsync,
    MethodInfo DeleteBulkAsync,
    MethodInfo UpdateAsync,
    MethodInfo UpdateBulkAsync,
    MethodInfo UpsertAsync,
    MethodInfo UpsertBulkAsync,
    PropertyInfo QueryTaskResultProperty,
    PropertyInfo QueryProjectedTaskResultProperty,
    PropertyInfo GetTaskResultProperty,
    PropertyInfo CreateTaskResultProperty,
    PropertyInfo CreateBulkTaskResultProperty,
    PropertyInfo PatchTaskResultProperty,
    PropertyInfo PatchBulkTaskResultProperty,
    PropertyInfo DeleteTaskResultProperty,
    PropertyInfo DeleteBulkTaskResultProperty,
    PropertyInfo UpdateTaskResultProperty,
    PropertyInfo UpdateBulkTaskResultProperty,
    PropertyInfo UpsertTaskResultProperty,
    PropertyInfo UpsertBulkTaskResultProperty,
    PropertyInfo CreateResultIsSuccessProperty,
    PropertyInfo CreateResultDataProperty,
    PropertyInfo CreateResultErrorProperty,
    PropertyInfo CreateResultKeyProperty,
    PropertyInfo PatchResultIsSuccessProperty,
    PropertyInfo PatchResultErrorProperty,
    PropertyInfo UpdateResultResultProperty,
    PropertyInfo UpdateResultErrorProperty);

/// <summary>Metadata for a single entity type used by dynamic CRUD endpoints.</summary>
internal sealed record EntityEndpointMetadata<TContext>(
    Type EntityType,
    Type KeyType,
    string KeyPropertyName,
    LambdaExpression DefaultOrder,
    DynamicMethodCache Cache,
    PatchPropertyAuthorization? PatchPropertyAuthorization,
    Delegate? AdaptedPatchBefore,
    Delegate? AdaptedPatchAfter) where TContext : DbContext;