namespace Lyo.Job.Client;

/// <summary>Settings used to build <see cref="JobClient" /> routes.</summary>
public sealed class JobClientOptions
{
    /// <summary>
    /// Optional absolute or root URL prefix put in front of every job route (for example <c>https://api.example.com</c>). When empty, routes are relative and use
    /// <see cref="System.Net.Http.HttpClient.BaseAddress" /> on the inner <see cref="Lyo.Api.Client.IApiClient" />.
    /// </summary>
    public string? RoutePrefix { get; set; }
}