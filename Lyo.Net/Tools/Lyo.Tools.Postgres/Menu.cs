using Lyo.Audit.Postgres.Database;
using Lyo.ChangeTracker.Postgres.Database;
using Lyo.Comic.Postgres.Database;
using Lyo.Comment.Postgres.Database;
using Lyo.Config.Postgres.Database;
using Lyo.ContactUs.Postgres.Database;
using Lyo.Discord.Postgres.Database;
using Lyo.Email.Postgres.Database;
using Lyo.Endato.Postgres.Database;
using Lyo.Favorite.Postgres.Database;
using Lyo.FileMetadataStore.Postgres.Database;
using Lyo.HomeInventory.Postgres.Database;
using Lyo.Job.Postgres.Database;
using Lyo.Note.Postgres.Database;
using Lyo.People.Postgres.Database;
using Lyo.Rating.Postgres.Database;
using Lyo.ShortUrl.Postgres.Database;
using Lyo.Sms.Postgres.Database;
using Lyo.Sms.Twilio.Postgres.Database;
using Lyo.Tag.Postgres.Database;
using Lyo.Tools.Postgres.Seeds;
using Lyo.Web.Reporting.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace Lyo.Tools.Postgres;

internal static class Menu
{
    private const string BackLabel = "← Back";

    private static readonly MigrationStatus RollbackSentinel = new("__rollback__", false);
    private static readonly MigrationStatus CancelSentinel = new("__cancel__", false);

    public static async Task RunAsync(IServiceProvider sp, CancellationToken ct)
    {
        var connStr = sp.GetRequiredService<ConnectionStringProvider>();
        while (!ct.IsCancellationRequested) {
            AnsiConsole.Clear();
            WriteHeader(connStr);
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>().Title("What would you like to do?").AddChoices("Seeds", "Migrations", "Change Connection String", "Exit"));

            if (ct.IsCancellationRequested || choice == "Exit")
                break;

            try {
                switch (choice) {
                    case "Seeds":
                        await SeedsMenuAsync(sp, ct);
                        break;
                    case "Migrations":
                        await MigrationsMenuAsync(sp.GetRequiredService<MigrationRunner>(), ct);
                        break;
                    case "Change Connection String":
                        ChangeConnectionString(connStr);
                        break;
                }
            }
            catch (OperationCanceledException) {
                break;
            }
            catch (Exception ex) {
                WriteError(ex);
                Pause();
            }
        }
    }

    private static void ChangeConnectionString(ConnectionStringProvider connStr)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[blue]Change Connection String[/]").RuleStyle("grey"));
        AnsiConsole.MarkupLine($"  Current: [grey]{Markup.Escape(connStr.GetMasked())}[/]\n");
        var input = AnsiConsole.Prompt(new TextPrompt<string>("New connection string [grey](blank to cancel)[/]:").AllowEmpty());
        if (!string.IsNullOrWhiteSpace(input)) {
            connStr.ConnectionString = input;
            AnsiConsole.MarkupLine("[green]Connection string updated.[/]");
        }
        else
            AnsiConsole.MarkupLine("[grey]No change.[/]");

        Pause();
    }

    private static async Task SeedsMenuAsync(IServiceProvider sp, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested) {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule("[blue]Seeds[/]").RuleStyle("grey"));
            AnsiConsole.WriteLine();
            var choice = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Select a seeder:").AddChoices("Comic Database", "People Database", BackLabel));
            if (choice == BackLabel)
                break;

            try {
                switch (choice) {
                    case "Comic Database":
                        await SeedComicAsync(sp, ct);
                        break;
                    case "People Database":
                        await SeedPeopleAsync(sp, ct);
                        break;
                }
            }
            catch (OperationCanceledException) {
                break;
            }
            catch (Exception ex) {
                WriteError(ex);
                Pause();
            }
        }
    }

    private static async Task SeedComicAsync(IServiceProvider sp, CancellationToken ct)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[blue]Comic Database Seeder[/]").RuleStyle("grey"));
        AnsiConsole.WriteLine();
        var count = AnsiConsole.Prompt(new TextPrompt<int>("Number of series to generate:").DefaultValue(20));
        var seedInput = AnsiConsole.Prompt(new TextPrompt<string>("[grey](Optional)[/] Random seed [grey](blank = random)[/]:").AllowEmpty());
        int? seed = int.TryParse(seedInput, out var s) ? s : null;
        AnsiConsole.WriteLine();
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .SpinnerStyle(Style.Parse("green"))
            .StartAsync("Seeding comic database...", async _ => await sp.GetRequiredService<ComicDbSeeder>().SeedAsync(count, seed, ct));

        AnsiConsole.MarkupLine("[green]Done![/]");
        Pause();
    }

    private static async Task SeedPeopleAsync(IServiceProvider sp, CancellationToken ct)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[blue]People Database Seeder[/]").RuleStyle("grey"));
        AnsiConsole.WriteLine();
        var count = AnsiConsole.Prompt(new TextPrompt<int>("Number of persons to generate:").DefaultValue(50));
        var seedInput = AnsiConsole.Prompt(new TextPrompt<string>("[grey](Optional)[/] Random seed [grey](blank = random)[/]:").AllowEmpty());
        int? seed = int.TryParse(seedInput, out var s) ? s : null;
        AnsiConsole.WriteLine();
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .SpinnerStyle(Style.Parse("green"))
            .StartAsync("Seeding people database...", async _ => await sp.GetRequiredService<PeopleDbSeeder>().SeedAsync(count, seed, ct));

        AnsiConsole.MarkupLine("[green]Done![/]");
        Pause();
    }

    private static async Task MigrationsMenuAsync(MigrationRunner runner, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested) {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule("[blue]Migrations[/]").RuleStyle("grey"));
            AnsiConsole.WriteLine();
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>().Title("Select a context:")
                    .PageSize(20)
                    .AddChoices(
                        "Run All (Latest)", "Audit", "ChangeTracker", "Comic", "Comment", "Config", "ContactUs", "Discord", "Email", "Endato", "Favorite", "FileMetadataStore",
                        "HomeInventory", "Job", "Note", "People", "Rating", "Reporting", "ShortUrl", "Sms", "SmsTwilio", "Tag", BackLabel));

            if (choice == BackLabel)
                break;

            try {
                if (choice == "Run All (Latest)") {
                    await AnsiConsole.Status()
                        .Spinner(Spinner.Known.Star)
                        .SpinnerStyle(Style.Parse("green"))
                        .StartAsync("Running all migrations...", async _ => await runner.RunAllAsync(ct));

                    AnsiConsole.MarkupLine("[green]Done![/]");
                    Pause();
                }
                else
                    await DispatchContextMenuAsync(choice, runner, ct);
            }
            catch (OperationCanceledException) {
                break;
            }
            catch (Exception ex) {
                WriteError(ex);
                Pause();
            }
        }
    }

    private static Task DispatchContextMenuAsync(string label, MigrationRunner runner, CancellationToken ct)
        => label switch {
            "Audit" => ContextMenuAsync<AuditDbContext>(runner, "audit", label, ct),
            "ChangeTracker" => ContextMenuAsync<ChangeTrackerDbContext>(runner, "change_tracker", label, ct),
            "Comic" => ContextMenuAsync<ComicDbContext>(runner, "comic", label, ct),
            "Comment" => ContextMenuAsync<CommentDbContext>(runner, "comment", label, ct),
            "Config" => ContextMenuAsync<ConfigDbContext>(runner, "config", label, ct),
            "ContactUs" => ContextMenuAsync<ContactUsDbContext>(runner, "contact", label, ct),
            "Discord" => ContextMenuAsync<DiscordDbContext>(runner, "discord", label, ct),
            "Email" => ContextMenuAsync<EmailDbContext>(runner, "email", label, ct),
            "Endato" => ContextMenuAsync<EndatoDbContext>(runner, "endato", label, ct),
            "Favorite" => ContextMenuAsync<FavoriteDbContext>(runner, "favorite", label, ct),
            "FileMetadataStore" => ContextMenuAsync<FileMetadataStoreDbContext>(runner, "filestore", label, ct),
            "HomeInventory" => ContextMenuAsync<HomeInventoryDbContext>(runner, "home_inventory", label, ct),
            "Job" => ContextMenuAsync<JobContext>(runner, "job", label, ct),
            "Note" => ContextMenuAsync<NoteDbContext>(runner, "note", label, ct),
            "People" => ContextMenuAsync<PeopleDbContext>(runner, "people", label, ct),
            "Rating" => ContextMenuAsync<RatingDbContext>(runner, "rating", label, ct),
            "Reporting" => ContextMenuAsync<ReportingDbContext>(runner, "report", label, ct),
            "ShortUrl" => ContextMenuAsync<ShortUrlDbContext>(runner, "url", label, ct),
            "Sms" => ContextMenuAsync<SmsDbContext>(runner, "sms", label, ct),
            "SmsTwilio" => ContextMenuAsync<TwilioSmsDbContext>(runner, "sms", label, ct),
            "Tag" => ContextMenuAsync<TagDbContext>(runner, "tag", label, ct),
            var _ => Task.CompletedTask
        };

    private static async Task ContextMenuAsync<TContext>(MigrationRunner runner, string schema, string label, CancellationToken ct)
        where TContext : DbContext
    {
        while (!ct.IsCancellationRequested) {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[blue]{label} Migrations[/]").RuleStyle("grey"));
            AnsiConsole.WriteLine();
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>().Title("Select an action:").AddChoices("Migrate to Latest", "Migrate to Target...", "View Status", "View Current Version", BackLabel));

            if (choice == BackLabel)
                break;

            try {
                switch (choice) {
                    case "Migrate to Latest":
                        await AnsiConsole.Status()
                            .Spinner(Spinner.Known.Star)
                            .SpinnerStyle(Style.Parse("green"))
                            .StartAsync($"Migrating {label} to latest...", async _ => await runner.MigrateLatestAsync<TContext>(schema, ct));

                        AnsiConsole.MarkupLine("[green]Done![/]");
                        Pause();
                        break;
                    case "Migrate to Target...":
                        await MigrateToTargetAsync<TContext>(runner, schema, label, ct);
                        break;
                    case "View Status":
                        await PrintStatusAsync<TContext>(runner, schema, label, ct);
                        break;
                    case "View Current Version":
                        await PrintCurrentVersionAsync<TContext>(runner, schema, label, ct);
                        break;
                }
            }
            catch (OperationCanceledException) {
                break;
            }
            catch (Exception ex) {
                WriteError(ex);
                Pause();
            }
        }
    }

    private static async Task MigrateToTargetAsync<TContext>(MigrationRunner runner, string schema, string label, CancellationToken ct)
        where TContext : DbContext
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule($"[blue]{label} — Migrate to Target[/]").RuleStyle("grey"));
        AnsiConsole.WriteLine();
        IReadOnlyList<MigrationStatus> statuses = [];
        await AnsiConsole.Status().Spinner(Spinner.Known.Dots).StartAsync("Fetching migrations...", async _ => statuses = await runner.GetStatusAsync<TContext>(schema, ct));
        if (statuses.Count == 0) {
            AnsiConsole.MarkupLine("[grey]No migrations defined.[/]");
            Pause();
            return;
        }

        var choices = new List<MigrationStatus> { RollbackSentinel };
        choices.AddRange(statuses);
        choices.Add(CancelSentinel);
        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<MigrationStatus>().Title($"Select target for [blue]{label}[/]:")
                .PageSize(20)
                .UseConverter(s => s.Name switch {
                    "__rollback__" => "[grey][ Roll back all ][/]",
                    "__cancel__" => "[ Cancel ]",
                    var _ => $"{(s.IsApplied ? "[green]✓ APPLIED[/]" : "[yellow]· PENDING[/]")}  {s.Name}"
                })
                .AddChoices(choices));

        if (selected.Name == "__cancel__")
            return;

        string target;
        if (selected.Name == "__rollback__") {
            if (!AnsiConsole.Confirm("[red]Roll back ALL migrations?[/]"))
                return;

            target = "0";
        }
        else
            target = selected.Name;

        AnsiConsole.WriteLine();
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .SpinnerStyle(Style.Parse("green"))
            .StartAsync($"Migrating {label} → {target}...", async _ => await runner.MigrateToAsync<TContext>(schema, target, ct));

        AnsiConsole.MarkupLine("[green]Done![/]");
        Pause();
    }

    private static async Task PrintStatusAsync<TContext>(MigrationRunner runner, string schema, string label, CancellationToken ct)
        where TContext : DbContext
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule($"[blue]{label} Migration Status[/]").RuleStyle("grey"));
        AnsiConsole.WriteLine();
        IReadOnlyList<MigrationStatus> statuses = [];
        await AnsiConsole.Status().Spinner(Spinner.Known.Dots).StartAsync("Fetching status...", async _ => statuses = await runner.GetStatusAsync<TContext>(schema, ct));
        if (statuses.Count == 0)
            AnsiConsole.MarkupLine("[grey]No migrations defined.[/]");
        else {
            var table = new Table().RoundedBorder().BorderColor(Color.Grey).AddColumn("Status").AddColumn("Migration");
            foreach (var s in statuses)
                table.AddRow(s.IsApplied ? "[green]✓ APPLIED[/]" : "[yellow]· PENDING[/]", Markup.Escape(s.Name));

            AnsiConsole.Write(table);
        }

        Pause();
    }

    private static async Task PrintCurrentVersionAsync<TContext>(MigrationRunner runner, string schema, string label, CancellationToken ct)
        where TContext : DbContext
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule($"[blue]{label} — Current Version[/]").RuleStyle("grey"));
        AnsiConsole.WriteLine();
        string? version = null;
        await AnsiConsole.Status().Spinner(Spinner.Known.Dots).StartAsync("Fetching version...", async _ => version = await runner.GetCurrentVersionAsync<TContext>(schema, ct));
        if (version is null)
            AnsiConsole.MarkupLine("[yellow]No migrations applied yet.[/]");
        else
            AnsiConsole.MarkupLine($"Current version: [green]{Markup.Escape(version)}[/]");

        Pause();
    }

    private static void WriteHeader(ConnectionStringProvider connStr)
    {
        AnsiConsole.Write(new Rule("[bold blue]Lyo Postgres Tools[/]").RuleStyle("grey"));
        AnsiConsole.MarkupLine($"  Connection: [grey]{Markup.Escape(connStr.GetMasked())}[/]");
        AnsiConsole.WriteLine();
    }

    private static void WriteError(Exception ex) => AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");

    private static void Pause()
    {
        AnsiConsole.MarkupLine("[grey]\nPress any key to continue...[/]");
        Console.ReadKey(true);
    }
}