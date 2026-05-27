namespace Lyo.Authentication.Services.Jwt;

/// <summary>Constants for the Ed25519 signature scheme as used by the Lyo JWT stack.</summary>
public static class Ed25519Constants
{
    /// <summary>The byte length of an Ed25519 private seed (32).</summary>
    public const int PrivateSeedLength = 32;

    /// <summary>The byte length of an Ed25519 public key (32).</summary>
    public const int PublicKeyLength = 32;

    /// <summary>The byte length of an Ed25519 signature (64).</summary>
    public const int SignatureLength = 64;
}
