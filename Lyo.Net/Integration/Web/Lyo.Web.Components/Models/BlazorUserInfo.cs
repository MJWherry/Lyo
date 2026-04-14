namespace Lyo.Web.Components.Models;

public class BlazorUserInfo
{
    public string TokenId { get; init; } = string.Empty;

    public string UserId { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public DateTime SignedInAt { get; init; }

    public DateTime JwtExpiration { get; init; }

    //public IDictionary<string, string> Claims { get; set; } = new Dictionary<string, string>();

    public DateTime LastActivity { get; set; }

    public string CurrentPage { get; set; } = string.Empty;

    public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
}