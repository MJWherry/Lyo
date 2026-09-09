namespace Lyo.TestGateway;

/// <summary>
/// Sample formatter data owned by the gateway, filling in for the values a real host would bind at runtime.
/// </summary>
/// <remarks>
/// Court job workers attach a client via <c>ctx.AddContext("client", client)</c>, which is what lets templates like
/// <c>{client.contact.emailAddress}</c> resolve. Until that bind happens the shape is unknown, so the host publishes one sample and every formatter editor in
/// the gateway can suggest those paths while a template is drafted.
/// <para>
/// Wired in <c>Program.cs</c> via <c>AddLyoFormatterValueEditor</c> and reused on the Formatter test page, so the page and the parameter editors share
/// the same keys.
/// </para>
/// </remarks>
public static class CourtFormatterContext
{
    /// <summary>Example keys available to a gateway template. Values are dummy data; autocomplete only needs the shape.</summary>
    public static Dictionary<string, object?> Values { get; } = new(StringComparer.OrdinalIgnoreCase) {
        ["Name"] = "Ada",
        ["Count"] = 1234,
        ["Date"] = new DateTime(2026, 8, 16, 14, 30, 0, DateTimeKind.Utc),
        // Same AddContext shape that JobWorkerParameterFormatTests asserts.
        ["client"] = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) {
            ["name"] = "Acme Legal",
            ["caseNumber"] = "2026-CV-01842",
            ["contact"] = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) {
                ["firstName"] = "Ada",
                ["lastName"] = "Lovelace",
                ["emailAddress"] = "ada@analytical.engine",
                ["phoneNumber"] = "+1 555 0100"
            },
            ["address"] = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) {
                ["line1"] = "12 Great College Street",
                ["city"] = "London",
                ["postalCode"] = "SW1P 3SH",
                ["country"] = "UK"
            }
        },
        ["Order"] = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) {
            ["Id"] = 1842,
            ["Status"] = "Shipped",
            ["Total"] = 99.50m,
            ["ShipTo"] = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) {
                ["City"] = "London",
                ["Country"] = "UK"
            }
        }
    };
}
