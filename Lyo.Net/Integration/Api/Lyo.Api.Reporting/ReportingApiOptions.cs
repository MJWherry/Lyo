using Lyo.Api.ApiEndpoint;
using Lyo.Reporting.Postgres.Database;

namespace Lyo.Api.Reporting;

/// <summary>
/// Per-surface authorization for reporting endpoints. All surfaces default to
/// <see cref="EndpointAuth.RequireAuthorization()"/> (authenticated user); hosts must opt into
/// <see cref="EndpointAuth.Anonymous()"/> explicitly. Hosts should set policies for Worker/Discord
/// Generate (e.g. <c>"ReportingGenerate"</c>).
/// </summary>
public sealed class ReportingApiOptions
{
    /// <summary>Auth applied to Definition CRUD (+ Export when enabled). Defaults to requiring authorization.</summary>
    public EndpointAuth? DefinitionAuth { get; init; } = EndpointAuth.RequireAuthorization();

    /// <summary>Auth applied to Generation query/get (read-only). Defaults to requiring authorization.</summary>
    public EndpointAuth? GenerationAuth { get; init; } = EndpointAuth.RequireAuthorization();

    /// <summary>Auth applied to POST Generation/Generate and POST Generation/{id}/Rerun. Defaults to requiring authorization.</summary>
    public EndpointAuth? GenerateAuth { get; init; } = EndpointAuth.RequireAuthorization();

    /// <summary>Auth applied to GET Generation/{id}/Download. Defaults to requiring authorization.</summary>
    public EndpointAuth? DownloadAuth { get; init; } = EndpointAuth.RequireAuthorization();

    /// <summary>
    /// Host-supplied factory that opens a readable stream for a generation's persisted output
    /// (e.g. FileStorage lookup by <see cref="ReportGeneration.OutputFileId"/>). The Download endpoint
    /// is only mapped when this is set. Return null when the output no longer exists.
    /// </summary>
    public Func<ReportDownloadContext, CancellationToken, Task<Stream?>>? DownloadStreamFactory { get; init; }

    /// <summary>
    /// Creates options applying the same <paramref name="auth"/> to every surface
    /// (Definitions, Generations, Generate/Rerun, Download). Use the object initializer form when
    /// surfaces need different policies (e.g. a stricter <see cref="GenerateAuth"/>).
    /// </summary>
    public static ReportingApiOptions WithAuth(
        EndpointAuth? auth,
        Func<ReportDownloadContext, CancellationToken, Task<Stream?>>? downloadStreamFactory = null) => new() {
        DefinitionAuth = auth,
        GenerationAuth = auth,
        GenerateAuth = auth,
        DownloadAuth = auth,
        DownloadStreamFactory = downloadStreamFactory
    };
}

/// <summary>Context passed to <see cref="ReportingApiOptions.DownloadStreamFactory"/> for a downloadable generation.</summary>
public sealed class ReportDownloadContext
{
    public required Guid GenerationId { get; init; }

    public required Guid OutputFileId { get; init; }

    public string? ContentType { get; init; }

    public string? FileName { get; init; }

    public string? PathPrefix { get; init; }

    public required IServiceProvider Services { get; init; }
}
