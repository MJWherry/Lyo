namespace Lyo.Job.Client;

/// <summary>Options for <see cref="JobClient" /> route construction.</summary>
public sealed class JobClientOptions
{
    /// <summary>
    /// Optional absolute or root URL prefix prepended to every job route (e.g. <c>https://api.example.com</c>). When empty, routes are relative and rely on
    /// <see cref="System.Net.Http.HttpClient.BaseAddress" /> on the underlying <see cref="Lyo.Api.Client.IApiClient" />.
    /// </summary>
    public string? RoutePrefix { get; set; }
}
