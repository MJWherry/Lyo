using Lyo.Authentication.Models.Audit;
using Lyo.Web.Components.DataGrid;
using Lyo.Web.Primitives;
using MudBlazor;

namespace Lyo.Authentication.Web.Components;

/// <summary>Chip specs for auth admin grids (kind, outcome, provider, token kind).</summary>
internal static class AuthChipHelper
{
    public static LyoChipSpec Kind(object? item)
        => LyoChips.FromEnum<AuthAuditEventKind>(ProjectedValueHelper.GetDisplayValue(item, "Kind"), KindColor);

    public static LyoChipSpec Outcome(object? item)
    {
        var text = ProjectedValueHelper.GetDisplayValue(item, "Outcome");
        if (string.Equals(text, "success", StringComparison.OrdinalIgnoreCase))
            return LyoChips.Of("success", Color.Success);

        if (string.Equals(text, "failure", StringComparison.OrdinalIgnoreCase))
            return LyoChips.Of("failure", Color.Error);

        return LyoChips.Of(text, Color.Default, variant: Variant.Outlined);
    }

    public static LyoChipSpec Provider(object? item)
        => LyoChips.Of(ProjectedValueHelper.GetDisplayValue(item, "Provider"), Color.Info, variant: Variant.Outlined);

    public static LyoChipSpec TokenKind(object? item)
    {
        var text = ProjectedValueHelper.GetDisplayValue(item, "Kind");
        if (string.Equals(text, "pat", StringComparison.OrdinalIgnoreCase))
            return LyoChips.Of(text, Color.Primary, variant: Variant.Outlined);

        if (string.Equals(text, "internal", StringComparison.OrdinalIgnoreCase))
            return LyoChips.Of(text, Color.Secondary, variant: Variant.Outlined);

        return LyoChips.Of(text, Color.Default, variant: Variant.Outlined);
    }

    private static Color KindColor(AuthAuditEventKind kind)
        => kind switch {
            AuthAuditEventKind.ExternalLoginRejected or AuthAuditEventKind.TokenRejected or AuthAuditEventKind.RefreshRejected or AuthAuditEventKind.HandoffCodeRejected
                or AuthAuditEventKind.UserDisabled => Color.Error,
            AuthAuditEventKind.TokenRevoked or AuthAuditEventKind.TokenDeleted or AuthAuditEventKind.SignedOut => Color.Warning,
            AuthAuditEventKind.ExternalLoginSucceeded or AuthAuditEventKind.JwtIssued or AuthAuditEventKind.TokenIssued or AuthAuditEventKind.TokenValidated
                or AuthAuditEventKind.RefreshSucceeded or AuthAuditEventKind.UserProvisioned or AuthAuditEventKind.IdentityLinked or AuthAuditEventKind.UserEnabled
                or AuthAuditEventKind.HandoffCodeConsumed => Color.Success,
            var _ => Color.Info
        };
}
