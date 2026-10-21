using Lyo.Diagnostic.StackTrace;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lyo.Diagnostic.Web.Components;

public partial class StackTraceDecodePanel
{
    private List<string> _prefixChips = [];

    private string _raw = string.Empty;

    private bool _restrictUserPrefixes = true;

    private List<string> _thirdPartyChips = [];

    [Parameter]
    public EventCallback<DecodedStackTrace> OnDecoded { get; set; }

    [Parameter]
    public int RawTraceLines { get; set; } = 12;

    private Task OnPrefixChipsChanged(IEnumerable<string> values)
    {
        _prefixChips = values.ToList();
        return Task.CompletedTask;
    }

    private Task OnThirdPartyChipsChanged(IEnumerable<string> values)
    {
        _thirdPartyChips = values.ToList();
        return Task.CompletedTask;
    }

    private static List<string> TrimmedChips(IEnumerable<string> chips) => chips.Where(static s => !string.IsNullOrWhiteSpace(s)).Select(static s => s.Trim()).ToList();

    private async Task DecodeAsync()
    {
        try {
            var decoder = new StackTraceDecoder(
                new() {
                    UserCodePrefixes = TrimmedChips(_prefixChips), ExtraSystemPrefixes = TrimmedChips(_thirdPartyChips), RestrictUserCodeToListedPrefixes = _restrictUserPrefixes
                });

            var decoded = await decoder.DecodeAsync(_raw);
            await OnDecoded.InvokeAsync(decoded);
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }
}