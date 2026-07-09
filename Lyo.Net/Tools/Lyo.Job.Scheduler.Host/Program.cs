using Lyo.Api.Client;
using Lyo.Formatter;
using Lyo.Job.Postgres;
using Lyo.Job.Scheduler;
using Lyo.MessageQueue.RabbitMq;
using Lyo.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) => {
        services.AddLogging(i => i.ClearProviders()
            .AddSimpleConsole(c => {
                c.SingleLine = true;
                c.UseUtcTimestamp = true;
            }));

        services.AddLyoMetrics();
        services.AddFormatterService();
        services.SetupRabbitMqServiceFromConfiguration(context.Configuration, []);
        services.AddMqJobEventPublisherFromConfiguration(context.Configuration);

        services.Configure<ApiClientOptions>(context.Configuration.GetSection(ApiClientOptions.SectionName));
        services.AddLyoApiClient();

        services.AddJobScheduler();
        // BindConfiguration cannot convert string → TimeZoneInfo; resolve IANA/Windows ids from config.
        services.PostConfigure<JobSchedulerOptions>(options => {
            var tzId = context.Configuration[$"{JobSchedulerOptions.SectionName}:TimeZone"];
            if (string.IsNullOrWhiteSpace(tzId) || options.TimeZone != null)
                return;
            options.TimeZone = TimeZoneInfo.FindSystemTimeZoneById(tzId);
        });
        // Optional: services.AddJobWorkflowEngine();
    })
    .Build();

await host.RunAsync();
