using System.Diagnostics;

namespace Lyo.Algorithms.Client;

/// <summary>Settings used to build <see cref="AlgorithmClient" /> routes.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class AlgorithmClientOptions
{
    /// <summary>Configuration section this type binds from.</summary>
    public const string SectionName = "AlgorithmClientOptions";

    /// <summary>Optional URL prefix put in front of every catalog route. When empty, routes are relative and use <see cref="System.Net.Http.HttpClient.BaseAddress" />.</summary>
    public string? RoutePrefix { get; set; }

    /// <summary>Rejects a prefix that would make a malformed route. Called by the <c>AddAlgorithmClient</c> overloads before registration.</summary>
    /// <exception cref="ArgumentException">The prefix is whitespace-only or contains a scheme separator (set a base address on the HttpClient instead).</exception>
    public void Validate()
    {
        if (RoutePrefix is null)
            return;

        if (RoutePrefix.Length > 0 && RoutePrefix.Trim().Length == 0)
            throw new ArgumentException($"{nameof(RoutePrefix)} must be null or a non-whitespace value.", nameof(RoutePrefix));

        if (RoutePrefix.Contains("://", StringComparison.Ordinal))
            throw new ArgumentException($"{nameof(RoutePrefix)} is a path prefix, not an absolute URL. Set HttpClient.BaseAddress for the host.", nameof(RoutePrefix));
    }

    /// <summary>Route prefix used in front of catalog routes.</summary>
    public override string ToString() => $"RoutePrefix={RoutePrefix}";
}