namespace Lyo.Drift.Client;

/// <summary>Settings used to build <see cref="DriftClient" /> routes.</summary>
public sealed class DriftClientOptions
{
    /// <summary>
    /// Optional absolute or root URL prefix put in front of every drift route (for example <c>https://api.example.com</c>). When empty, routes are relative and use
    /// the inner client's base address.
    /// </summary>
    public string? RoutePrefix { get; set; }
}
