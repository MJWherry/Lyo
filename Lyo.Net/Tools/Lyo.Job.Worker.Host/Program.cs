using Lyo.Api.Client;
using Lyo.Job.Client;
using Lyo.Job.Worker;
using Lyo.Job.Worker.Host;
using Lyo.MessageQueue.RabbitMq;
using Lyo.Metrics;
using Constants = Lyo.Job.Worker.Host.Constants;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) => {
        services.AddLogging(i => i.ClearProviders()
            .AddSimpleConsole(c => {
                c.SingleLine = true;
                c.UseUtcTimestamp = true;
            }));

        services.AddLyoMetrics();
        services.SetupRabbitMqServiceFromConfiguration(context.Configuration, []);
        var jobApiBaseUrl = context.Configuration["JobWorker:ApiBaseUrl"] ?? Constants.DefaultApiBaseUrl;
        var workerType = context.Configuration["JobWorker:WorkerType"] ?? Constants.ExampleWorkerType;
        services.AddLyoApiClient();
        services.AddJobClient(sp => sp.GetRequiredService<IApiClient>(), new() { RoutePrefix = jobApiBaseUrl.TrimEnd('/') });
        services.AddMqJobEventPublisherFromConfiguration(context.Configuration);
        services.AddJobWorker<ExampleJobWorker>(workerType, jobApiBaseUrl);
    })
    .Build();

await host.RunAsync();
