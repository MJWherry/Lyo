using Lyo.Formatter;
using Lyo.Formatter.Web.Components;
using Microsoft.AspNetCore.Components;

namespace Lyo.TestGateway.Components.Pages;

public partial class FormatterTest
{
    private readonly Dictionary<string, object?> _context = CourtFormatterContext.Values;

    private LyoFormatterLiveSession _session = default!;

    protected override string PageName { get; set; } = "Formatter";

    protected override void OnPageInitialized()
    {
        _session = new(Formatter) {
            Template = "Hello, {Name}!\nYou have {Count:N0} items as of {Date:yyyy-MM-dd}.\n{client.contact.firstName} {client.contact.lastName} ({client.contact.emailAddress}) — {client.address.city}, {client.address.country}\nOrder {Order.Id}: {Order.Status}, {Order.Total:N2}.",
            Context = _context
        };
        _session.RefreshPreview();
    }

    private static string FormatContextValue(object? value) => value switch {
        DateTime date => date.ToString("yyyy-MM-dd"),
        IReadOnlyDictionary<string, object?> nested => "{ " + string.Join(", ", nested.Select(p => $"{p.Key} = {FormatContextValue(p.Value)}")) + " }",
        null => "null",
        _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? ""
    };
}
