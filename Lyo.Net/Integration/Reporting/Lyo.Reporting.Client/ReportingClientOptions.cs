namespace Lyo.Reporting.Client;

/// <summary>Options for <see cref="ReportingClient" /> route construction.</summary>
public sealed class ReportingClientOptions
{
    /// <summary>Optional URL prefix prepended to every reporting route. When empty, routes are relative and rely on <see cref="System.Net.Http.HttpClient.BaseAddress" />.</summary>
    public string? RoutePrefix { get; set; }
}