using Microsoft.AspNetCore.Components;

namespace Lyo.Billing.Web.Components.Components.Forms;

public partial class ClientPicker
{
    private ClientLookup? _value;

    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    public Guid? Value { get; set; }

    [Parameter]
    public EventCallback<Guid?> ValueChanged { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        if (Value is not { } id || id == Guid.Empty) {
            if (_value != null && Value == null)
                _value = null;

            return;
        }

        if (_value?.Id == id)
            return;

        var res = await ApiClient.QueryProjectAsync<ProjectedQueryRes<ClientLookup>>("Billing/Clients", new() { Keys = [[id]], Amount = 1, Select = ["Id", "DisplayName"] });
        _value = res?.Items?.FirstOrDefault();
    }

    private async Task<IEnumerable<ClientLookup>> SearchAsync(string? value, CancellationToken ct)
    {
        var res = await ApiClient.QueryProjectAsync<ProjectedQueryRes<ClientLookup>>("Billing/Clients", new() { Amount = 40, Select = ["Id", "DisplayName"] }, ct: ct);
        var items = res?.Items ?? [];
        if (string.IsNullOrWhiteSpace(value))
            return items;

        return items.Where(c => c.DisplayName != null && c.DisplayName.Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    private async Task OnChanged(ClientLookup? client)
    {
        _value = client;
        await ValueChanged.InvokeAsync(client?.Id);
    }
}