using System.Text.Json;

namespace Lyo.Http.Client.Plan;

/// <summary>Runtime options for a plan run (bindings, cookie file, download root). Secrets stay here, not in committed plan JSON.</summary>
public sealed class HttpClientPlanRuntime
{
    /// <summary>String bindings for <c>{{var}}</c> and extract outputs.</summary>
    public Dictionary<string, string> Bindings { get; } = new(StringComparer.Ordinal);

    /// <summary>JSON blobs (item lists, JSON-LD) keyed by variable name.</summary>
    public Dictionary<string, string> JsonBindings { get; } = new(StringComparer.Ordinal);

    /// <summary>String lists (download URL lists).</summary>
    public Dictionary<string, List<string>> ListBindings { get; } = new(StringComparer.Ordinal);

    /// <summary>Optional path to a cookie JSON file loaded at start / used by <c>loadCookies</c>.</summary>
    public string? CookieFile { get; set; }

    /// <summary>Root for relative download destinations.</summary>
    public string? DownloadDirectory { get; set; }

    /// <summary>Optional JSON serializer used for extract item snapshots.</summary>
    public JsonSerializerOptions? SerializerOptions { get; set; }
}

/// <summary>Outcome of a plan run.</summary>
public sealed class HttpClientPlanRunResult
{
    /// <summary>True when every step succeeded or continued past a handled failure.</summary>
    public bool Success { get; init; }

    /// <summary>Final bindings snapshot.</summary>
    public required HttpClientPlanRuntime Runtime { get; init; }

    /// <summary>Last HTTP response body, when any request ran.</summary>
    public string? LastBody { get; init; }

    /// <summary>Last status code.</summary>
    public int? LastStatusCode { get; init; }

    /// <summary>Failure message when <see cref="Success" /> is false. Includes inner exception messages when present.</summary>
    public string? Error { get; init; }

    /// <summary>Thrown exception for the failed step, when the failure was an exception rather than an HTTP status.</summary>
    public Exception? Exception { get; init; }
}
