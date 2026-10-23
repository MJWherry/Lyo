using Lyo.Algorithms.Seed;
using Lyo.Audiobook.Seed;
using Lyo.Audit.Seed;
using Lyo.Authentication.Seed;
using Lyo.Billing.Seed;
using Lyo.ChangeTracker.Seed;
using Lyo.Comment.Seed;
using Lyo.Config.Seed;
using Lyo.ContactUs.Seed;
using Lyo.Discord.Seed;
using Lyo.Drift.Seed;
using Lyo.Email.Seed;
using Lyo.Endato.Seed;
using Lyo.Favorite.Seed;
using Lyo.FileMetadataStore.Seed;
using Lyo.FileSystemWatcher.Seed;
using Lyo.Geolocation.Seed;
using Lyo.HomeInventory.Seed;
using Lyo.Job.Seed;
using Lyo.Law.Seed;
using Lyo.Leaderboard.Seed;
using Lyo.Ledger.Seed;
using Lyo.Music.Seed;
using Lyo.Note.Seed;
using Lyo.PackageMetadata.Seed;
using Lyo.Podcast.Seed;
using Lyo.Rating.Seed;
using Lyo.Recipe.Seed;
using Lyo.Reporting.Seed;
using Lyo.Seed;
using Lyo.ShortUrl.Seed;
using Lyo.Sms.Seed;
using Lyo.Sms.Twilio.Seed;
using Lyo.StateMachine.Seed;
using Lyo.Tag.Seed;
using Lyo.Tools.Postgres;
using Lyo.Tools.Postgres.Seeds;
using Lyo.Validation.Seed;
using Lyo.Video.Seed;
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
        services.AddSeedContributor<BookEfSeedContributor>();
        services.AddSeedContributor<StoryEfSeedContributor>();
        services.AddSeedContributor<CommentEfSeedContributor>();
        services.AddSeedContributor<FavoriteEfSeedContributor>();
        services.AddSeedContributor<NoteEfSeedContributor>();
        services.AddSeedContributor<RatingEfSeedContributor>();
        services.AddSeedContributor<TagEfSeedContributor>();
        services.AddSeedContributor<ContactUsEfSeedContributor>();
        services.AddSeedContributor<ShortUrlEfSeedContributor>();
        services.AddSeedContributor<HomeInventoryEfSeedContributor>();
        services.AddSeedContributor<RecipeEfSeedContributor>();
        services.AddSeedContributor<ConfigEfSeedContributor>();
        services.AddSeedContributor<JobEfSeedContributor>();
        services.AddSeedContributor<ReportingEfSeedContributor>();
        services.AddSeedContributor<BillingEfSeedContributor>();
        services.AddSeedContributor<LedgerEfSeedContributor>();
        services.AddSeedContributor<LawEfSeedContributor>();
        services.AddSeedContributor<MusicEfSeedContributor>();
        services.AddSeedContributor<AudiobookEfSeedContributor>();
        services.AddSeedContributor<PodcastEfSeedContributor>();
        services.AddSeedContributor<VideoEfSeedContributor>();
        services.AddSeedContributor<AlgorithmEfSeedContributor>();
        services.AddSeedContributor<LeaderboardEfSeedContributor>();
        services.AddSeedContributor<AuthenticationEfSeedContributor>();
        services.AddSeedContributor<GeolocationEfSeedContributor>();
        services.AddSeedContributor<ValidationEfSeedContributor>();
        services.AddSeedContributor<StateMachineEfSeedContributor>();
        services.AddSeedContributor<PackageMetadataEfSeedContributor>();
        services.AddSeedContributor<FileMetadataEfSeedContributor>();
        services.AddSeedContributor<FileSystemWatcherEfSeedContributor>();
        services.AddSeedContributor<EmailEfSeedContributor>();
        services.AddSeedContributor<SmsEfSeedContributor>();
        services.AddSeedContributor<TwilioSmsEfSeedContributor>();
        services.AddSeedContributor<DiscordEfSeedContributor>();
        services.AddSeedContributor<EndatoEfSeedContributor>();
        services.AddSeedContributor<DriftEfSeedContributor>();
        services.AddSeedContributor<AuditEfSeedContributor>();
        services.AddSeedContributor<ChangeTrackerEfSeedContributor>();
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