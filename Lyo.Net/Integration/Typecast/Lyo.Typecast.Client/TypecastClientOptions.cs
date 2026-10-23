using System.Diagnostics;
using Lyo.Http.Client;

namespace Lyo.Typecast.Client;

/// <summary>
/// Settings that control Typecast API client. Inherits <see cref="LyoHttpClientOptions" /> for HTTP transport; use <see cref="LyoHttpClientOptions.BaseUrl" /> for the
/// Typecast service API root.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public class TypecastClientOptions : LyoHttpClientOptions
{
    /// <summary>TypecastClientOptions the default configuration section name.</summary>
    public new const string SectionName = "TypecastClient";

    /// <summary>Holds the Typecast API key (required).</summary>
    public string ApiKey { get; set; } = null!;

    /// <summary>Fills defaults for the Typecast API.</summary>
    public TypecastClientOptions() => BaseUrl = "https://api.typecast.ai";

    /// <summary>Whether an API key is set, plus the shared HTTP options. The key value is omitted.</summary>
    public override string ToString() => $"ApiKeySet={!string.IsNullOrWhiteSpace(ApiKey)} {base.ToString()}";
}