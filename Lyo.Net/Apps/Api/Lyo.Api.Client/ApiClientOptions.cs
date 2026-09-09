using Lyo.Http.Client;

namespace Lyo.Api.Client;

/// <summary>
/// HTTP transport settings for <see cref="ApiClient" />. Prefer <see cref="LyoHttpClientOptions" /> on new vendor clients.
/// </summary>
public class ApiClientOptions : LyoHttpClientOptions
{
    /// <summary>Default configuration section for ApiClientOptions.</summary>
    public new const string SectionName = "ApiClient";
}
