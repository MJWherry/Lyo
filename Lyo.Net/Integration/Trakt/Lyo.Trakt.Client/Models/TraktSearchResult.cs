namespace Lyo.Trakt.Client;

/// <summary>One search or id-lookup row. The populated title property matches <see cref="Type" />.</summary>
public sealed class TraktSearchResult
{
    /// <summary>Row kind: movie, show, episode, person, or list.</summary>
    public string? Type { get; set; }

    /// <summary>Search score, when the endpoint sends one.</summary>
    public double? Score { get; set; }

    /// <summary>Movie payload when <see cref="Type" /> is movie.</summary>
    public TraktMovie? Movie { get; set; }

    /// <summary>Show payload when <see cref="Type" /> is show.</summary>
    public TraktShow? Show { get; set; }
}