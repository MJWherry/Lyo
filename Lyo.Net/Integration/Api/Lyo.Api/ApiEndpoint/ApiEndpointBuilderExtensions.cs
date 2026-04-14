using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.ApiEndpoint;

public static class ApiEndpointBuilderExtensions
{
    // Convenience methods for ReadOnly endpoints using the new flag system. Default sort order is primary key.
    public static ApiEndpointBuilder<TDbContext, TDbEntity, object, TResponse, TKey> WithReadOnlyEndpoints<TDbContext, TDbEntity, TResponse, TKey>(
        this ApiEndpointBuilder<TDbContext, TDbEntity, object, TResponse, TKey> builder)
        where TDbContext : DbContext where TDbEntity : class
        => builder.WithCrud(ApiFeatureFlag.ReadOnly, new());

    public static ApiEndpointBuilder<TDbContext, TDbEntity, object, TResponse, Guid> WithReadOnlyEndpoints<TDbContext, TDbEntity, TResponse>(
        this ApiEndpointBuilder<TDbContext, TDbEntity, object, TResponse, Guid> builder)
        where TDbContext : DbContext where TDbEntity : class
        => builder.WithCrud(ApiFeatureFlag.ReadOnly, new());

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
        public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, Guid> CreateBuilder<TDbContext, TDbEntity, TRequest, TResponse>(string baseRoute, string groupName)
            where TDbContext : DbContext where TDbEntity : class
            => new(app, baseRoute, groupName);

        public ApiEndpointBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey> CreateBuilder<TDbContext, TDbEntity, TRequest, TResponse, TKey>(
            string baseRoute,
            string groupName)
            where TDbContext : DbContext where TDbEntity : class
            => new(app, baseRoute, groupName);

        public ApiEndpointBuilder<TDbContext, TDbEntity, object, TResponse, Guid> CreateReadOnlyBuilder<TDbContext, TDbEntity, TResponse>(string baseRoute, string groupName)
            where TDbContext : DbContext where TDbEntity : class
            => new(app, baseRoute, groupName);

        public ApiEndpointBuilder<TDbContext, TDbEntity, object, TResponse, TKey> CreateReadOnlyBuilder<TDbContext, TDbEntity, TResponse, TKey>(string baseRoute, string groupName)
            where TDbContext : DbContext where TDbEntity : class
            => new(app, baseRoute, groupName);
    }
}