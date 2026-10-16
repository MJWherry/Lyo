namespace Lyo.Trakt.Client;

/// <summary>One row from <c>GET /movies/trending</c>.</summary>
public sealed class TraktTrendingMovie
{
    /// <summary>Watchers in the trending window.</summary>
    public int Watchers { get; set; }

    /// <summary>The trending movie.</summary>
    public TraktMovie? Movie { get; set; }
}
