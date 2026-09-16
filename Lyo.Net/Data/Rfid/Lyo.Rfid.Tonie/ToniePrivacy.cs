namespace Lyo.Rfid.Tonie;

/// <summary>
/// SLIX-L privacy password used when programming a blank tag. <see cref="NxpDefault"/> is the NXP production value <c>0F0F0F0F</c> from the SLIX-L datasheet.
/// Identifier <see cref="PrivacyIdentifier"/> selects that password on GET RANDOM and SET PASSWORD.
/// </summary>
public static class ToniePrivacy
{
    /// <summary>Password identifier for the privacy password (datasheet table, <c>04h</c>).</summary>
    public const byte PrivacyIdentifier = 0x04;

    /// <summary>Factory privacy password. Four bytes, each <c>0x0F</c>.</summary>
    public static ReadOnlyMemory<byte> NxpDefault { get; } = new byte[] { 0x0F, 0x0F, 0x0F, 0x0F };
}
