using System.Net.Http.Json;
using Lyo.Api.Client;
using Lyo.People.Models;
using Lyo.Reporting.Models.Request;
using Lyo.Reporting.Models.Response;
using Lyo.Seed;
using Lyo.TestGateway.Components;
using Lyo.TestGateway.Models;
using Lyo.TestGateway.Seeds;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using MudBlazor;
using static Microsoft.AspNetCore.Components.Web.RenderMode;

namespace Lyo.TestGateway.Components.Pages;

public partial class SeedPage
{
    private int _count = 50;
    private string _seedText = "";
    private bool _skipIfNotEmpty;
    private bool _skipReportsIfNotEmpty;
    private bool _running;
    private string? _lastMessage;
    private Severity _lastSeverity = Severity.Info;

    protected override string PageName { get; set; } = "Seed";

    private async Task SeedPeopleAsync()
    {
        _running = true;
        _lastMessage = null;
        try {
            int? randomSeed = int.TryParse(_seedText, out var s) ? s : null;
            var catalog = new SeedApiCatalog();
            catalog.Map<PersonReq>("Person");
            var transport = new ApiSeedTransport(ApiClient, catalog);
            var result = await SeedRunner.SeedAsync(
                new PeopleApiSeedContributor(),
                transport,
                new() {
                    Count = _count,
                    RandomSeed = randomSeed,
                    Conflict = _skipIfNotEmpty ? SeedConflictMode.SkipIfNotEmpty : SeedConflictMode.Append
                });
            if (result.Skipped) {
                _lastSeverity = Severity.Info;
                _lastMessage = "Skipped — Person already has rows.";
                Snackbar.Add(_lastMessage, Severity.Info);
                return;
            }

            if (!result.Success) {
                _lastSeverity = Severity.Error;
                _lastMessage = string.Join("; ", result.Errors);
                Snackbar.Add(_lastMessage, Severity.Error);
                return;
            }

            _lastSeverity = Severity.Success;
            _lastMessage = $"Seeded {string.Join(", ", result.Counts.Select(kv => $"{kv.Key}={kv.Value}"))} (source {PeopleSourceTypes.Seed}).";
            Snackbar.Add(_lastMessage, Severity.Success);
        }
        catch (Exception ex) {
            _lastSeverity = Severity.Error;
            _lastMessage = ex.Message;
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _running = false;
        }
    }

    private async Task SeedReportsAsync()
    {
        _running = true;
        _lastMessage = null;
        try {
            var catalog = new SeedApiCatalog();
            catalog.Map<ReportDefinitionReq, ReportDefinitionRes>(Lyo.Reporting.Models.Constants.Rest.Reporting.Definitions);
            var transport = new ApiSeedTransport(ApiClient, catalog);
            var result = await SeedRunner.SeedAsync(
                new ReportingApiSeedContributor(),
                transport,
                new() {
                    Conflict = _skipReportsIfNotEmpty ? SeedConflictMode.SkipIfNotEmpty : SeedConflictMode.Append
                });
            if (result.Skipped) {
                _lastSeverity = Severity.Info;
                _lastMessage = "Skipped — report definitions already exist.";
                Snackbar.Add(_lastMessage, Severity.Info);
                return;
            }

            if (!result.Success) {
                _lastSeverity = Severity.Error;
                _lastMessage = string.Join("; ", result.Errors);
                Snackbar.Add(_lastMessage, Severity.Error);
                return;
            }

            _lastSeverity = Severity.Success;
            _lastMessage = $"Seeded {string.Join(", ", result.Counts.Select(kv => $"{kv.Key}={kv.Value}"))}.";
            Snackbar.Add(_lastMessage, Severity.Success);
        }
        catch (Exception ex) {
            _lastSeverity = Severity.Error;
            _lastMessage = ex.Message;
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _running = false;
        }
    }
}
