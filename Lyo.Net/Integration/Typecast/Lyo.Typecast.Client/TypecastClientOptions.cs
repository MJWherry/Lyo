namespace Lyo.Typecast.Client;

/// <summary>Configuration options for Typecast API client.</summary>
public class TypecastClientOptions
{
    /// <summary>The default configuration section name for TypecastClientOptions.</summary>
    public const string SectionName = "TypecastClient";

    /// <summary>Gets or sets the Typecast API key (required).</summary>
    /// <remarks>This is your Typecast API key. Keep this secure and never commit it to source control.</remarks>
    public string ApiKey { get; set; } = null!;

    /// <summary>Gets or sets the Typecast API base URL (optional, defaults to production API).</summary>
    public string ApiBaseUrl { get; set; } = "https://api.typecast.ai";

    /// <summary>Gets or sets whether to ensure HTTP success status codes (default true). When false, responses are returned regardless of status code.</summary>
    public bool EnsureStatusCode { get; set; } = true;
}