using Lyo.Exceptions;

namespace Lyo.Web.Automation.Client;

/// <summary>Settings used to build <see cref="AutomationPlanClient" /> routes.</summary>
public sealed class AutomationPlanClientOptions
{
    /// <summary>Configuration section this type binds from.</summary>
    public const string SectionName = "AutomationPlanClientOptions";

    /// <summary>Optional URL prefix put in front of every run route. When empty, routes are relative and use <see cref="System.Net.Http.HttpClient.BaseAddress" />.</summary>
    public string? RoutePrefix { get; set; }

    /// <summary>Rejects a prefix that would make a malformed route.</summary>
    public void Validate()
    {
        if (RoutePrefix is null)
            return;

        ArgumentHelpers.ThrowIf(RoutePrefix.Trim().Length == 0, "RoutePrefix must be null or a non-whitespace value.", nameof(RoutePrefix));
        ArgumentHelpers.ThrowIf(RoutePrefix.Contains("://", StringComparison.Ordinal), "RoutePrefix is a path prefix, not an absolute URL.", nameof(RoutePrefix));
    }
}
