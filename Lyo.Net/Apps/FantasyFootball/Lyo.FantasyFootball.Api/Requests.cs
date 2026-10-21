using Lyo.Exceptions;
using Microsoft.AspNetCore.Http;

namespace Lyo.FantasyFootball.Api;

/// <summary>ESPN cookies for one request. A blank value keeps the cookie configured on the host.</summary>
public sealed class FantasyFootballCredentials
{
    /// <summary>Request header for <see cref="EspnS2" />.</summary>
    public const string EspnS2Header = "X-Espn-S2";

    /// <summary>Request header for <see cref="Swid" />.</summary>
    public const string SwidHeader = "X-Swid";

    /// <summary>Value of the <c>espn_s2</c> cookie.</summary>
    public string? EspnS2 { get; set; }

    /// <summary>Value of the <c>SWID</c> cookie.</summary>
    public string? Swid { get; set; }

    /// <summary>Reads the cookies from request headers. Null when both headers are blank.</summary>
    public static FantasyFootballCredentials? From(HttpRequest request)
    {
        ArgumentHelpers.ThrowIfNull(request);
        return Normalize(new() { EspnS2 = request.Headers[EspnS2Header].ToString(), Swid = request.Headers[SwidHeader].ToString() });
    }

    /// <summary>
    /// Trims a pasted cookie. A value that already contains <c>espn_s2=</c> and <c>SWID=</c> is split. A bare SWID guid is wrapped in braces. Blank stays blank so the host
    /// cookie is kept.
    /// </summary>
    public static FantasyFootballCredentials? Normalize(FantasyFootballCredentials? credentials)
    {
        if (credentials == null)
            return null;

        var espnS2 = credentials.EspnS2;
        var swid = credentials.Swid;
        TakeCookiePair(ref espnS2, ref swid);
        espnS2 = CleanToken(espnS2, "espn_s2");
        swid = CleanSwid(swid);
        if (espnS2 == null && swid == null)
            return null;

        return new() { EspnS2 = espnS2, Swid = swid };
    }

    private static void TakeCookiePair(ref string? espnS2, ref string? swid)
    {
        TakeNamed(espnS2, ref espnS2, ref swid);
        TakeNamed(swid, ref espnS2, ref swid);
    }

    private static void TakeNamed(string? source, ref string? espnS2, ref string? swid)
    {
        if (string.IsNullOrWhiteSpace(source) || !source.Contains('=', StringComparison.Ordinal))
            return;

        string? foundS2 = null;
        string? foundSwid = null;
        foreach (var part in source.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
            var eq = part.IndexOf('=');
            if (eq <= 0)
                continue;

            var name = part[..eq].Trim();
            var value = part[(eq + 1)..].Trim();
            if (name.Equals("espn_s2", StringComparison.OrdinalIgnoreCase))
                foundS2 = value;
            else if (name.Equals("SWID", StringComparison.OrdinalIgnoreCase))
                foundSwid = value;
        }

        if (foundS2 != null)
            espnS2 = foundS2;

        if (foundSwid != null)
            swid = foundSwid;
    }

    private static string? CleanToken(string? value, string name)
    {
        value = Strip(value);
        if (value == null)
            return null;

        var prefix = name + "=";
        if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            value = Strip(value[prefix.Length..]);

        return value;
    }

    private static string? CleanSwid(string? value)
    {
        value = CleanToken(value, "SWID");
        if (value == null)
            return null;

        var bare = value.Trim().TrimStart('{').TrimEnd('}');
        if (Guid.TryParse(bare, out var _))
            return "{" + bare + "}";

        return value;
    }

    private static string? Strip(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        value = value.Trim().Trim('"').Trim();
        if (string.IsNullOrWhiteSpace(value))
            return null;

        try {
            return Uri.UnescapeDataString(value);
        }
        catch (UriFormatException) {
            return value;
        }
    }
}

/// <summary>Body for a message posted to a topic that is already open.</summary>
public sealed class PostChatMessageRequest
{
    /// <summary>Text the other members will see.</summary>
    public string Content { get; set; } = "";
}

/// <summary>Body for a message sent to an existing direct message.</summary>
public sealed class PostDirectMessageRequest
{
    /// <summary>Member SWID or a unique display name.</summary>
    public string Recipient { get; set; } = "";

    /// <summary>Text the other member will see.</summary>
    public string Content { get; set; } = "";
}

/// <summary>Body for a trade proposal. <see cref="SendPlayerIds" /> leave this team. <see cref="ReceivePlayerIds" /> come from the other team.</summary>
public sealed class ProposeTradeRequest
{
    /// <summary>Team making the offer.</summary>
    public int TeamId { get; set; }

    /// <summary>Team receiving the offer.</summary>
    public int OtherTeamId { get; set; }

    /// <summary>Scoring period ESPN should attach to the proposal.</summary>
    public int ScoringPeriodId { get; set; }

    /// <summary>Players this team gives up.</summary>
    public IReadOnlyList<int> SendPlayerIds { get; set; } = [];

    /// <summary>Players this team asks for.</summary>
    public IReadOnlyList<int> ReceivePlayerIds { get; set; } = [];

    /// <summary>Optional note.</summary>
    public string? Comment { get; set; }
}

/// <summary>Body for accept, decline, uphold, or veto.</summary>
public sealed class RespondTradeRequest
{
    /// <summary>Team answering the proposal.</summary>
    public int TeamId { get; set; }

    /// <summary>Scoring period ESPN should attach to the response.</summary>
    public int ScoringPeriodId { get; set; }

    /// <summary><c>accept</c>, <c>decline</c>, <c>uphold</c>, or <c>veto</c>.</summary>
    public string Action { get; set; } = "";

    /// <summary>Optional note. Declines and vetoes send it when it is set.</summary>
    public string? Comment { get; set; }
}