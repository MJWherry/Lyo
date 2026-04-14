using System.Security.Claims;
using Lyo.Common.Enums;
using Lyo.Query.Models.Enums;
using Lyo.Web.Components.Models;
using MudBlazor;

namespace Lyo.Web.Components;

internal static class Extensions
{
    public static BlazorUserInfo ToUserInfo(this ClaimsPrincipal principal)
    {
        var userId = principal.FindFirst("azure_oid")?.Value;
        var name = principal.FindFirst(ClaimTypes.Name)?.Value;
        var tokenId = principal.FindFirst("azure_token_id")?.Value;
        var email = principal.FindFirst(ClaimTypes.Email)?.Value;
        var exp = principal.FindFirstValue("exp");
        //var claims = principal.Claims.ToDictionary(c => c.Type, c => c.Value);
        return new() {
            TokenId = tokenId,
            UserId = userId,
            Name = name,
            Email = email,
            SignedInAt = DateTime.UtcNow,
            LastActivity = DateTime.UtcNow,
            //Claims = claims
            JwtExpiration = DateTimeOffset.FromUnixTimeSeconds(long.Parse(exp)).UtcDateTime,
            CurrentPage = string.Empty
        };
    }

    public static string GetTokenId(this ClaimsPrincipal principal)
        => principal.FindFirst("azure_token_id")?.Value ?? throw new InvalidOperationException("No user identifier found in claims");

    //public static Color GetStateColor(JobState state)
    //    => state switch
    //    {
    //        JobState.Unknown => Color.Default,
    //        JobState.Queued => Color.Warning,
    //        JobState.Running => Color.Info,
    //        JobState.Finished => Color.Success,
    //        JobState.Cancelled => Color.Secondary,
    //        _ => Color.Default
    //    };
    //
    //public static Color GetResultColor(JobRunResult result) 
    //    => result switch
    //    {
    //        JobRunResult.Success => Color.Success,
    //        JobRunResult.PartialSuccess => Color.Error,
    //        JobRunResult.SuccessWithWarnings => Color.Warning,
    //        JobRunResult.Failure => Color.Error,
    //        _ => Color.Default
    //    };

    public static Color GetStatusColor(string status)
        => status switch {
            "Success" => Color.Success,
            "Failure" => Color.Error,
            "Success with warnings" => Color.Warning,
            "Partial Success" => Color.Info,
            "Cancelled" => Color.Secondary,
            "Skipped" => Color.Secondary,
            "Timed out" => Color.Warning,
            var _ => Color.Default
        };

    public static string GetStatusIcon(string status)
        => status switch {
            "Success" => Icons.Material.Filled.CheckCircle,
            "Failure" => Icons.Material.Filled.Error,
            "Success with warnings" => Icons.Material.Filled.Warning,
            "Partial Success" => Icons.Material.Filled.Info,
            "Cancelled" => Icons.Material.Filled.Cancel,
            "Skipped" => Icons.Material.Filled.SkipNext,
            "Timed out" => Icons.Material.Filled.Timer,
            var _ => Icons.Material.Filled.Help
        };

    public static string GetIcon(FileTypeFlags type)
        => type switch {
            FileTypeFlags.Csv => Icons.Custom.FileFormats.FileCode,
            FileTypeFlags.Txt => Icons.Material.Filled.TextFields,
            FileTypeFlags.Html => Icons.Material.Filled.Web,
            FileTypeFlags.Json => Icons.Custom.FileFormats.FileCode,
            FileTypeFlags.Xml => Icons.Custom.FileFormats.FileCode,
            FileTypeFlags.Xlsx => Icons.Custom.FileFormats.FileExcel,
            var _ => Icons.Material.Filled.Description
        };

    public static List<ComparisonOperatorEnum> GetAvailableComparisonOperators(FilterPropertyType type)
        => type switch {
            FilterPropertyType.String => [
                ComparisonOperatorEnum.Contains, ComparisonOperatorEnum.NotContains, ComparisonOperatorEnum.Equals, ComparisonOperatorEnum.NotEquals, ComparisonOperatorEnum.StartsWith, ComparisonOperatorEnum.NotStartsWith,
                ComparisonOperatorEnum.EndsWith, ComparisonOperatorEnum.NotEndsWith, ComparisonOperatorEnum.In, ComparisonOperatorEnum.NotIn
            ],
            FilterPropertyType.Number => [
                ComparisonOperatorEnum.Equals, ComparisonOperatorEnum.NotEquals, ComparisonOperatorEnum.GreaterThan, ComparisonOperatorEnum.GreaterThanOrEqual, ComparisonOperatorEnum.LessThan,
                ComparisonOperatorEnum.LessThanOrEqual, ComparisonOperatorEnum.In, ComparisonOperatorEnum.NotIn
            ],
            FilterPropertyType.Enum => [ComparisonOperatorEnum.Equals, ComparisonOperatorEnum.NotEquals, ComparisonOperatorEnum.In, ComparisonOperatorEnum.NotIn],
            FilterPropertyType.DateTime => [
                ComparisonOperatorEnum.Equals, ComparisonOperatorEnum.NotEquals, ComparisonOperatorEnum.GreaterThan, ComparisonOperatorEnum.GreaterThanOrEqual, ComparisonOperatorEnum.LessThan,
                ComparisonOperatorEnum.LessThanOrEqual
            ],
            FilterPropertyType.Bool => [ComparisonOperatorEnum.Equals, ComparisonOperatorEnum.NotEquals],
            var _ => Enum.GetValues<ComparisonOperatorEnum>().ToList()
        };

    public static bool IsMultiValueComparisonOperator(this ComparisonOperatorEnum comparison) => comparison is ComparisonOperatorEnum.In or ComparisonOperatorEnum.NotIn;
}