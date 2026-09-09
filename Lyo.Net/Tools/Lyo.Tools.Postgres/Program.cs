using Lyo.Seed;
using Lyo.Tools.Postgres;
using Lyo.Tools.Postgres.Seeds;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => {
    e.Cancel = true;
    // ReSharper disable once AccessToDisposedClosure
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
        services.AddLyoSeed();
        services.AddSeedContributor<PeopleEfSeedContributor>();
        services.AddSeedContributor<PeopleApiSeedContributor>();
        services.AddSeedContributor<ComicEfSeedContributor>();
    })
    .Build();

await host.StartAsync(cts.Token);
using var scope = host.Services.CreateScope();
if (args is [var cmd, ..] && cmd.Equals("seed", StringComparison.OrdinalIgnoreCase)) {
    var code = await SeedCli.RunAsync(scope.ServiceProvider, args, cts.Token);
    await host.StopAsync();
    return code;
}

await Menu.RunAsync(scope.ServiceProvider, cts.Token);
await host.StopAsync();
return 0;
