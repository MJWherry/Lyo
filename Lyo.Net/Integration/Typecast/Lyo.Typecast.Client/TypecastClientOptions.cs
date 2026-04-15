using Lyo.Api.Client;

namespace Lyo.Typecast.Client;

/// <summary>Configuration options for Typecast API client. Inherits <see cref="ApiClientOptions" /> for HTTP transport; use <see cref="ApiClientOptions.BaseUrl" /> for the Typecast API root.</summary>
public class TypecastClientOptions : ApiClientOptions
{
    /// <summary>The default configuration section name for TypecastClientOptions.</summary>
    public new const string SectionName = "TypecastClient";

    /// <summary>Initializes defaults for the Typecast API.</summary>
    public TypecastClientOptions() => BaseUrl = "https://api.typecast.ai";

    /// <summary>Gets or sets the Typecast API key (required).</summary>
    /// <remarks>This is your Typecast API key. Keep this secure and never commit it to source control.</remarks>
    public string ApiKey { get; set; } = null!;
}
