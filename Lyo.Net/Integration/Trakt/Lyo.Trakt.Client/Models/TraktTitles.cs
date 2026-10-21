namespace Lyo.Trakt.Client;

/// <summary>Ids Trakt returns for a movie, show, or episode.</summary>
public sealed class TraktIds
{
    /// <summary>Trakt numeric id.</summary>
    public int? Trakt { get; set; }

    /// <summary>Trakt slug.</summary>
    public string? Slug { get; set; }

    /// <summary>IMDb id, including the <c>tt</c> prefix.</summary>
    public string? Imdb { get; set; }

    /// <summary>TMDB numeric id.</summary>
    public int? Tmdb { get; set; }

    /// <summary>TVDB numeric id.</summary>
    public int? Tvdb { get; set; }
}

/// <summary>Movie summary.</summary>
public sealed class TraktMovie
{
    /// <summary>Display title.</summary>
    public string? Title { get; set; }

    /// <summary>External ids.</summary>
    public TraktIds? Ids { get; set; }
}

/// <summary>Show summary. <see cref="Status" /> is the API phrase, including values with spaces such as <c>returning series</c>.</summary>
public sealed class TraktShow
{
    /// <summary>Display title.</summary>
    public string? Title { get; set; }

    /// <summary>Airing status as returned by Trakt.</summary>
    public string? Status { get; set; }

    /// <summary>External ids.</summary>
    public TraktIds? Ids { get; set; }
}

/// <summary>One episode from a season listing.</summary>
public sealed class TraktEpisode
{
    /// <summary>Season number.</summary>
    public int? Season { get; set; }

    /// <summary>Episode number within the season.</summary>
    public int? Number { get; set; }

    /// <summary>Episode title.</summary>
    public string? Title { get; set; }
}