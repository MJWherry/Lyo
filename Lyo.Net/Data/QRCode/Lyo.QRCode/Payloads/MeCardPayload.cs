using System.Diagnostics;
using System.Text;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.QRCode.Payloads;

/// <summary>meCard one-line contact format (<c>MECARD:</c>).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class MeCardPayload : IQrPayload
{
    /// <summary>Creates a meCard payload.</summary>
    public MeCardPayload(string displayName, string? telephone = null, string? email = null)
    {
        ArgumentHelpers.ThrowIfNull(displayName);
        DisplayName = displayName.Trim();
        Telephone = telephone?.Trim();
        Email = email?.Trim();
    }

    /// <summary>Display name (<c>N:</c>).</summary>
    public string DisplayName { get; }

    /// <summary>Optional <c>TEL:</c>.</summary>
    public string? Telephone { get; }

    /// <summary>Optional <c>EMAIL:</c>.</summary>
    public string? Email { get; }

    /// <inheritdoc />
    public override string ToString()
        => $"MeCardPayload N={DisplayName}, TEL={Telephone ?? "(none)"}, EMAIL={Email ?? "(none)"}";

    /// <inheritdoc />
    public string ToQrString()
    {
        ArgumentHelpers.ThrowIf(string.IsNullOrWhiteSpace(DisplayName), "Display name cannot be empty.", nameof(DisplayName));

        if (DisplayName.Contains(';') || DisplayName.Contains('\r') || DisplayName.Contains('\n'))
            throw new InvalidFormatException("Display name cannot contain semicolons or newlines in meCard.", nameof(DisplayName), DisplayName, "Jane Doe");

        var sb = new StringBuilder("MECARD:");
        sb.Append("N:").Append(DisplayName).Append(';');

        if (!string.IsNullOrWhiteSpace(Telephone)) {
            if (Telephone.Contains(';'))
                throw new InvalidFormatException("Telephone cannot contain ';' in meCard.", nameof(Telephone), Telephone);

            sb.Append("TEL:").Append(Telephone).Append(';');
        }

        if (!string.IsNullOrWhiteSpace(Email)) {
            if (Email.Contains(';'))
                throw new InvalidFormatException("Email cannot contain ';' in meCard.", nameof(Email), Email);

            sb.Append("EMAIL:").Append(Email).Append(';');
        }

        sb.Append(";;");
        return sb.ToString();
    }
}
