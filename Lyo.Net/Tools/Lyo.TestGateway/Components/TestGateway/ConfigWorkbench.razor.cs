using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Lyo.Config;
using Lyo.Images;
using Lyo.Images.Models;
using Lyo.TestGateway.Components;
using Lyo.Web.Components;
using Lyo.Web.Primitives;
using Lyo.Web.Components.DataGrid;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using MudBlazor;
using static Microsoft.AspNetCore.Components.Web.RenderMode;

namespace Lyo.TestGateway.Components.TestGateway;

public partial class ConfigWorkbench
{
    private const string SampleKey = "sampleFeature";
    private int _managementKey;
    private bool _seeding;

    private async Task SeedSampleAsync()
    {
        _seeding = true;
        try {
            var existing = await Store.GetDefinitionAsync(AppConfigEntity.AppEntityType, SampleKey);
            if (existing != null) {
                Snackbar.Add($"Sample definition '{SampleKey}' already exists.", Severity.Info);
                return;
            }

            var payload = new JsonObject {
                ["enabled"] = true,
                ["note"] = "seeded"
            };
            await Store.SaveDefinitionAsync(new() {
                SubjectEntityType = AppConfigEntity.AppEntityType,
                Key = SampleKey,
                ForValueType = ConfigValue.GetTypeName(typeof(JsonObject)),
                Description = "Sample JSON object used by the Test Gateway config workbench.",
                DefaultValue = ConfigValue.From(payload)
            });
            Snackbar.Add($"Seeded '{SampleKey}' for {AppConfigEntity.AppEntityType}.", Severity.Success);
            _managementKey++;
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _seeding = false;
        }
    }
}
