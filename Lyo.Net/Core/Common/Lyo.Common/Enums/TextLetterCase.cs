namespace Lyo.Common.Enums;

/// <summary>Letter casing for text encodings of binary data (e.g. hexadecimal A–F) and similar displays where only case differs.</summary>
public enum TextLetterCase
{
    /// <summary>Uppercase letters (e.g. matches <see cref="Convert.ToHexString(System.ReadOnlySpan{byte})" /> on .NET 5+).</summary>
    Upper,

    /// <summary>Lowercase letters (e.g. NuGet-style digests, lowercase JSON).</summary>
    Lower
}