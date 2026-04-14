using Lyo.Api.Services.Crud;
using Lyo.Api.Services.Crud.Create;
using Lyo.Api.Services.Crud.Delete;
using Lyo.Api.Services.Crud.Read;
using Lyo.Api.Services.Crud.Read.Project;
using Lyo.Api.Services.Crud.Read.Query;
using Lyo.Api.Services.Crud.Update;
using Lyo.Api.Services.Export;
using Lyo.Api.Services.TypeConversion;
using Lyo.Diff;
using Lyo.Query;
using Lyo.Query.Services.ValueConversion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Api;

/// <summary>
/// Registers Lyo.Api query services including TypeConversionService, EntityLoaderService, and Lyo.Query services. Requires CacheService and CacheOptions to be registered
/// (e.g. via AddFusionCache or AddLocalCache).
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers ITypeConversionService, IEntityLoaderService, IValueConversionService (via TypeConversionService), IPropertyComparisonService, and IWhereClauseService. Use this
        /// when hosting Lyo.Api with EF Core.
        /// </summary>
        public IServiceCollection AddLyoQueryServices()
        {
            services.TryAddSingleton(_ => new QueryOptions());
            services.AddSingleton<ITypeConversionService, TypeConversionService>()
                .AddSingleton<IValueConversionService>(sp => sp.GetRequiredService<ITypeConversionService>())
                .AddSingleton<IEntityLoaderService, EntityLoaderService>()
                .AddSingleton<IProjectionService, ProjectionService>()
                .AddSingleton<IQueryPathExecutor, QueryPathExecutor>()
                .AddSingleton<IQueryPagingHelper, QueryPagingHelper>()
                .AddLyoQueryServices(false);

            return services;
        }

        /// <summary>
        /// Registers CRUD services for a DbContext: IQueryService, ICreateService, IPatchService, IDeleteService, IUpdateService, IUpsertService, IQueryHistoryService. Export is
        /// opt-in via WithExportService. Requires: AddLyoQueryServices, AddFusionCache or AddLocalCache, ILyoMapper, IDbContextFactory&lt;TContext&gt;.
        /// </summary>
        public IServiceCollection AddLyoCrudServices<TContext>()
            where TContext : DbContext
        {
            services.TryAddSingleton(_ => new BulkOperationOptions());
            services.TryAddSingleton(_ => new QueryOptions());
            services.AddScoped<IQueryService<TContext>, QueryService<TContext>>();
            services.AddScoped<ICreateService<TContext>, CreateService<TContext>>();
            services.AddScoped<IPatchService<TContext>, PatchService<TContext>>();
            services.AddScoped<IDeleteService<TContext>, DeleteService<TContext>>();
            services.AddScoped<IUpdateService<TContext>, UpdateService<TContext>>();
            services.AddScoped<IUpsertService<TContext>, UpsertService<TContext>>();
            services.AddScoped<IQueryHistoryService<TContext>, QueryHistoryService<TContext>>();
            services.AddScoped<ILyoRepository<TContext>, LyoRepository<TContext>>();
            return services;
        }

        /// <summary>Registers export service for a DbContext. Call this when using endpoint builders with WithExport.</summary>
        public IServiceCollection WithExportService<TContext>()
            where TContext : DbContext
        {
            services.TryAddScoped<IExportService<TContext>, ExportService<TContext>>();
            return services;
        }

        /// <summary>Registers <see cref="PostgresSprocService{TContext}" /> for PostgreSQL set-returning functions (SELECT * FROM schema.func(…)).</summary>
        public IServiceCollection AddPostgresSprocService<TContext>()
            where TContext : DbContext
        {
            services.AddScoped<ISprocService<TContext>, PostgresSprocService<TContext>>();
            return services;
        }

        /// <summary>Registers Lyo.Diff: text diff, object-graph diff, and <see cref="IDiffService" />.</summary>
        public IServiceCollection AddLyoDiffServices()
        {
            services.AddLyoDiff();
            return services;
        }
    }
}