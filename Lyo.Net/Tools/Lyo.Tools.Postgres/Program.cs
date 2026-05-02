using Lyo.Tools.Postgres;
using Lyo.Tools.Postgres.Seeds;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => {
    e.Cancel = true;
    cts.Cancel();
};

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) => {
        services.AddLogging(logging => logging.ClearProviders()
            .AddSimpleConsole(c => {
                c.SingleLine = true;
                c.UseUtcTimestamp = true;
            }));

        var connStrProvider = new ConnectionStringProvider { ConnectionString = ctx.Configuration["ConnectionString"] };
        services.AddSingleton(connStrProvider);
        services.AddScoped<MigrationRunner>();
        services.AddScoped<ComicDbSeeder>();
        services.AddScoped<PeopleDbSeeder>();
    })
    .Build();

await host.StartAsync(cts.Token);
using var scope = host.Services.CreateScope();
await Menu.RunAsync(scope.ServiceProvider, cts.Token);
await host.StopAsync();