namespace Lyo.Leaderboard.Api.Models;

/// <summary>
/// Stable actors used when Leaderboard routes are mapped with <c>requireAuthentication: false</c> (TestApi workbench). Send <see cref="HeaderName" /> = <see cref="Guest" />
/// to act as the second workbench user (redeem / contributor submit).
/// </summary>
public static class LeaderboardAnonymousActor
{
    /// <summary>Request header the API reads when the caller is unauthenticated.</summary>
    public const string HeaderName = "X-Leaderboard-Actor";

    /// <summary>Header value that selects the guest workbench actor.</summary>
    public const string Guest = "guest";

    /// <summary>Fixed user id for the default anonymous workbench owner.</summary>
    public static readonly Guid WorkbenchUserId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    /// <summary>Fixed user id for the anonymous guest used to redeem and submit pending entries.</summary>
    public static readonly Guid GuestUserId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff");

    /// <summary>Default anonymous owner for the TestApi workbench.</summary>
    public static LeaderboardActor Workbench() => new(WorkbenchUserId, "workbench@local", "Workbench");

    /// <summary>Second anonymous actor so redeem/submit can run without a second login.</summary>
    public static LeaderboardActor GuestActor() => new(GuestUserId, "guest@local", "Guest");
}