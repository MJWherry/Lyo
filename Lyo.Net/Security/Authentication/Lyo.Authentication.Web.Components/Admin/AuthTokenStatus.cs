using Lyo.Web.Components.DataGrid;
using MudBlazor;

namespace Lyo.Authentication.Web.Components;

/// <summary>Computes token status chips from projected <c>RevokedTimestamp</c> / <c>ExpiresTimestamp</c> (UTC).</summary>
public static class AuthTokenStatus
{
    /// <summary>Revoked, then expired, otherwise active.</summary>
    public static (string Label, Color Color) FromProjected(object? item)
    {
        if (LyoDateTimeDisplay.ToDateTime(ProjectedValueHelper.GetValue(item, "RevokedTimestamp")) is not null)
            return ("Revoked", Color.Error);

        var expires = LyoDateTimeDisplay.ToDateTime(ProjectedValueHelper.GetValue(item, "ExpiresTimestamp"));
        if (expires is { } exp && exp <= DateTime.UtcNow)
            return ("Expired", Color.Warning);

        return ("Active", Color.Success);
    }
}
