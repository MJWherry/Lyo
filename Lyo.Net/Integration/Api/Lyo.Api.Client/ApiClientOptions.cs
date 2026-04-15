namespace Lyo.Api.Client;

/// <summary>
/// HTTP transport and JSON request options for <see cref="ApiClient" />. Also the shared base type for integration-specific option classes (e.g. Discord, Typecast) that subclass
/// <see cref="ApiClient" /> so compression, Accept-Encoding, and <see cref="BaseUrl" /> bind consistently from configuration.
/// </summary>
public class ApiClientOptions
{
    /// <summary>The default configuration section name for ApiClientOptions.</summary>
    public const string SectionName = "ApiClient";

    /// <summary>Gets or sets the base URL for API requests (e.g. "https://api.example.com/"). When set, relative URIs passed to methods are resolved against this address.</summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Gets or sets whether to call <see cref="System.Net.Http.HttpResponseMessage.EnsureSuccessStatusCode" /> on responses. When true (default), non-success status codes throw
    /// <see cref="System.Net.Http.HttpRequestException" />. When false, responses are returned regardless of status code, allowing APIs that return error details in the response body
    /// with correct status codes to be handled by the caller.
    /// </summary>
    public bool EnsureStatusCode { get; set; } = true;

    /// <summary>Gets or sets accepted response encodings advertised via the Accept-Encoding header. Defaults to gzip/deflate and br on modern targets.</summary>
#if NETSTANDARD2_0
    public string[] AcceptEncodings { get; set; } = ["gzip", "deflate"];
#else
    public string[] AcceptEncodings { get; set; } = ["gzip", "deflate", "br"];
#endif

    /// <summary>Gets or sets whether automatic response decompression should be enabled when ApiClient creates its own HttpClient.</summary>
    public bool EnableAutoResponseDecompression { get; set; } = true;

    /// <summary>Gets or sets the compression type for JSON request bodies.</summary>
    public ApiRequestCompressionType RequestCompression { get; set; } = ApiRequestCompressionType.None;

    /// <summary>Gets or sets the minimum JSON payload size in bytes before request compression is applied.</summary>
    public int RequestCompressionMinBytes { get; set; } = 1024;
}