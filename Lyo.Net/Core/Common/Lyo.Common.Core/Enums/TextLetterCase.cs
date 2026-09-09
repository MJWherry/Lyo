namespace Lyo.Common.Core.Enums;

/// <summary>Upper or lower letters when encoding binary as text (for example hex A–F) and other displays that differ only by case.</summary>
public enum TextLetterCase
{
    /// <summary>Uppercase letters (matches <see cref="Convert.ToHexString(System.ReadOnlySpan{byte})" /> on .NET 5+).</summary>
    Upper,

    /// <summary>Lowercase letters (NuGet-style digests, lowercase JSON).</summary>
    Lower
}