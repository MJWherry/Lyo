using Lyo.Api.ApiEndpoint;
using Lyo.Reporting.Postgres.Database;

namespace Lyo.Reporting.Api;

/// <summary>
/// Per-surface auth for reporting endpoints. Every surface starts as <see cref="EndpointAuth.RequireAuthorization()" /> (signed-in user). Hosts must choose
/// <see cref="EndpointAuth.Anonymous()" /> on purpose. Set policies for Worker/Discord Generate (for example <c>"ReportingGenerate"</c>).
/// </summary>
public sealed class ReportingApiOptions
{
    /// <summary>Auth on Definition CRUD (and Export when that feature is on). Defaults to requiring authorization.</summary>
    public EndpointAuth? DefinitionAuth { get; init; } = EndpointAuth.RequireAuthorization();

    /// <summary>Auth on Generation query/get (read-only). Defaults to requiring authorization.</summary>
    public EndpointAuth? GenerationAuth { get; init; } = EndpointAuth.RequireAuthorization();

    /// <summary>Auth on POST Generation/Generate and POST Generation/{id}/Rerun. Defaults to requiring authorization.</summary>
    public EndpointAuth? GenerateAuth { get; init; } = EndpointAuth.RequireAuthorization();

    /// <summary>Auth on GET Generation/{id}/Download. Defaults to requiring authorization.</summary>
    public EndpointAuth? DownloadAuth { get; init; } = EndpointAuth.RequireAuthorization();

    /// <summary>
    /// Host factory that opens a readable stream for a generation's stored output (for example FileStorage lookup by <see cref="ReportGeneration.OutputFileId" />). The
    /// Download route is mapped only when this is set. Return null if the output is gone.
    /// </summary>
    public Func<ReportDownloadContext, CancellationToken, Task<Stream?>>? DownloadStreamFactory { get; init; }

    /// <summary>
    /// Whether an unauthenticated caller may set <c>CreatedBy</c> on a generate request. Defaults to false: the field is audit data, and trusting it from an anonymous request
    /// lets a caller pin a generation on anyone. Authenticated requests always use the signed-in identity, whatever this flag is. Turn it on only for trusted
    /// service-to-service callers that have no user identity.
    /// </summary>
    public bool AllowAnonymousCreatedBy { get; init; }

    /// <summary>
    /// Builds options that apply the same <paramref name="auth" /> to every surface (Definitions, Generations, Generate/Rerun, Download). Use the object initializer when
    /// surfaces need different policies (for example a stricter <see cref="GenerateAuth" />).
    /// </summary>
    public static ReportingApiOptions WithAuth(EndpointAuth? auth, Func<ReportDownloadContext, CancellationToken, Task<Stream?>>? downloadStreamFactory = null)
        => new() {
            DefinitionAuth = auth,
            GenerationAuth = auth,
            GenerateAuth = auth,
            DownloadAuth = auth,
            DownloadStreamFactory = downloadStreamFactory
        };
}

/// <summary>Context given to <see cref="ReportingApiOptions.DownloadStreamFactory" /> for a downloadable generation.</summary>
public sealed class ReportDownloadContext
{
    public required Guid GenerationId { get; init; }

    public required Guid OutputFileId { get; init; }

    public string? ContentType { get; init; }

    public string? FileName { get; init; }

    public string? PathPrefix { get; init; }

    public required IServiceProvider Services { get; init; }
}