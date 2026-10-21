namespace Lyo.OpenSubtitles.Client;

/// <summary>Authenticated user quota from <c>GET /infos/user</c>.</summary>
public sealed class OpenSubtitlesUser
{
    /// <summary>OpenSubtitles user id.</summary>
    public int UserId { get; set; }

    /// <summary>Downloads left in the current quota window.</summary>
    public int RemainingDownloads { get; set; }
}