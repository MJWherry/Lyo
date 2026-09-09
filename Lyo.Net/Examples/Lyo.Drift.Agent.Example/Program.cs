using Lyo.Api.Client;
using Lyo.Drift.Agent;
using Lyo.Drift.Client;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) => {
        services.AddLogging(i => i.ClearProviders()
            .AddSimpleConsole(c => {
                c.SingleLine = true;
                c.UseUtcTimestamp = true;
            }));

        var apiBaseUrl = context.Configuration["DriftAgent:ApiBaseUrl"] ?? "http://localhost:5099";
        services.AddLyoApiClient(optionsOverride: o => o.BaseUrl = apiBaseUrl);
        services.AddDriftClient(sp => sp.GetRequiredService<IApiClient>(), new() { RoutePrefix = apiBaseUrl.TrimEnd('/') });
        services.AddDriftAgentFromConfiguration(context.Configuration);
    })
    .Build();

await host.RunAsync();
