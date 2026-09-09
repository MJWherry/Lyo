namespace Lyo.Config;

/// <summary>Query HTTP routes for Lyo.Query endpoints mapped over config EF entities (query/get/export only; writes stay on <see cref="IConfigStore" />).</summary>
public static class ConfigQueryRoutes
{
    /// <summary>OpenAPI group / endpoint name.</summary>
    public const string Group = "Config";

    /// <summary>Base route for definition queries (<c>/Config/Definition/QueryProject</c>, etc.).</summary>
    public const string Definition = "Config/Definition";

    /// <summary>Base route for binding queries.</summary>
    public const string Binding = "Config/Binding";

    /// <summary>Named/keyed <c>IEncryptionService</c> / key-store name used by Config.Api and TestApi.</summary>
    public const string EncryptionKeyName = "config-values";
}
