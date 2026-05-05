using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.ApiEndpoint;

/// <summary>Entry points for typed minimal-API CRUD: creates <see cref="ApiEndpointBuilder{TDbContext,TDbEntity,TRequest,TResponse,TKey}" /> instances from <see cref="WebApplication" />.</summary>
public static class ApiEndpointBuilderExtensions
{
    /// <summary>Registers Query, QueryProject, and Get only (<see cref="ApiFeatureFlag.ReadOnly" />). Default sort uses the entity primary key.</summary>
    public static ApiEndpointBuilder<TDbContext, TDbEntity, object, TResponse, TKey> WithReadOnlyEndpoints<TDbContext, TDbEntity, TResponse, TKey>(
        this ApiEndpointBuilder<TDbContext, TDbEntity, object, TResponse, TKey> builder)
        where TDbContext : DbContext where TDbEntity : class
        => builder.WithCrud(ApiFeatureFlag.ReadOnly, new());

    /// <summary>Same as <see cref="WithReadOnlyEndpoints{TDbContext,TDbEntity,TResponse,TKey}" /> with <c>TKey = Guid</c>.</summary>
    public static ApiEndpointBuilder<TDbContext, TDbEntity, object, TResponse, Guid> WithReadOnlyEndpoints<TDbContext, TDbEntity, TResponse>(
        this ApiEndpointBuilder<TDbContext, TDbEntity, object, TResponse, Guid> builder)
        where TDbContext : DbContext where TDbEntity : class
        => builder.WithCrud(ApiFeatureFlag.ReadOnly, new());

    /// <summary>Route template suffix for GET/DELETE by primary key (e.g. <c>/{id:guid}</c> for <see cref="Guid" />).</summary>
    public static string GetDefaultEndpoint<TKey>()
        => typeof(TKey) switch {
            { } t when t == typeof(Guid) => "/{id:guid}",
            { } t when t == typeof(int) => "/{id:int}",
            { } t when t == typeof(long) => "/{id:long}",
            { } t when t == typeof(string) => "/{id}",
            var _ => "/{id}"
        };

    extension(WebApplication app)
    {
        /// <summary>Starts a typed CRUD endpoint group with <c>TKey = Guid</c> (same route shape as the overload that takes an explicit key type parameter).</summary>
        /// <param name="baseRoute">URL prefix without trailing slash (e.g. <c>api/items</c>).</param>
        /// <param name="groupName">OpenAPI / endpoint group display name.</param>
        public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, Guid> CreateBuilder<TDbContext, TDbEntity, TRequest, TResponse>(string baseRoute, string groupName)
            where TDbContext : DbContext where TDbEntity : class
            => new(app, baseRoute, groupName);

        /// <summary>Starts a typed CRUD endpoint group for <typeparamref name="TDbEntity" /> with request/response DTOs and primary-key type <typeparamref name="TKey" />.</summary>
        /// <param name="baseRoute">URL prefix without trailing slash.</param>
        /// <param name="groupName">OpenAPI / endpoint group display name.</param>
        public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> CreateBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey>(
            string baseRoute,
            string groupName)
            where TDbContext : DbContext where TDbEntity : class
            => new(app, baseRoute, groupName);

        /// <summary>Starts a read-only typed builder (<c>TRequest = object</c>, <c>TKey = Guid</c>). Call <see cref="WithReadOnlyEndpoints{TDbContext,TDbEntity,TResponse}" /> or equivalent.</summary>
        public ApiEndpointBuilder<TDbContext, TDbEntity, object, TResponse, Guid> CreateReadOnlyBuilder<TDbContext, TDbEntity, TResponse>(string baseRoute, string groupName)
            where TDbContext : DbContext where TDbEntity : class
            => new(app, baseRoute, groupName);

        /// <summary>Starts a read-only typed builder with explicit <typeparamref name="TKey" /> for GET/DELETE routes.</summary>
        public ApiEndpointBuilder<TDbContext, TDbEntity, object, TResponse, TKey> CreateReadOnlyBuilder<TDbContext, TDbEntity, TResponse, TKey>(string baseRoute, string groupName)
            where TDbContext : DbContext where TDbEntity : class
            => new(app, baseRoute, groupName);
    }
}