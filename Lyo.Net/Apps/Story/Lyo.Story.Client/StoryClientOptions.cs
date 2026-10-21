namespace Lyo.Story.Client;

/// <summary>Settings used to build <see cref="StoryClient" /> routes.</summary>
public sealed class StoryClientOptions
{
    /// <summary>Configuration section this type binds from.</summary>
    public const string SectionName = "StoryClientOptions";

    /// <summary>Optional URL prefix put in front of every story route. When empty, routes are relative and use <see cref="System.Net.Http.HttpClient.BaseAddress" />.</summary>
    public string? RoutePrefix { get; set; }

    /// <summary>Rejects a prefix that would make a malformed route. Called by the <c>AddStoryClient</c> overloads before registration.</summary>
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
}