using Lyo.Api;
using Lyo.Api.ApiEndpoint;
using Lyo.Exceptions;
using Lyo.Postgres;
using Lyo.Web.Automation.Core;
using Lyo.Web.Automation.Plan;
using Lyo.Web.Automation.Postgres.Database;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Web.Automation.Postgres;

/// <summary>Registers the automation plan database and its query endpoint.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds the plan database from <paramref name="configure" /> and replaces <see cref="IAutomationPlanStore" />.</summary>
        public IServiceCollection AddAutomationPlansPostgres(Action<PostgresAutomationPlanOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresAutomationPlanOptions();
            configure(options);
            return services.AddAutomationPlansPostgres(options);
        }

        /// <summary>Adds the plan database from configuration. A missing section keeps the declared defaults.</summary>
        public IServiceCollection AddAutomationPlansPostgresFromConfiguration(IConfiguration configuration, string sectionName = PostgresAutomationPlanOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configuration);
            var options = new PostgresAutomationPlanOptions();
            configuration.GetSection(sectionName).Bind(options);
            return services.AddAutomationPlansPostgres(options);
        }

        /// <summary>Adds the plan database and replaces <see cref="IAutomationPlanStore" />.</summary>
        public IServiceCollection AddAutomationPlansPostgres(PostgresAutomationPlanOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresDbContextFactory<AutomationPlanDbContext, PostgresAutomationPlanOptions>(options);
            services.AddPostgresMigrations<AutomationPlanDbContext, PostgresAutomationPlanOptions>();
            services.AddLyoCrudServices<AutomationPlanDbContext>();
            services.Replace(ServiceDescriptor.Singleton<IAutomationPlanStore, PostgresAutomationPlanStore>());
            return services;
        }
    }

    /// <summary>Maps query and get for stored plans. Call after <see cref="Extensions.AddAutomationPlansPostgres(PostgresAutomationPlanOptions)" />.</summary>
    public static WebApplication BuildAutomationPlanRecords(this WebApplication app)
    {
        ArgumentHelpers.ThrowIfNull(app);
        app.CreateBuilder<AutomationPlanDbContext, AutomationPlanEntity, AutomationPlanEntity, AutomationPlanEntity, Guid>(Constants.Rest.PlanRecords, "Automation")
            .AllowAnonymous()
            .WithQuery()
            .WithGet()
            .Build();

        return app;
    }
}