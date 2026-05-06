using Lyo.Common.Enums;
using Lyo.Exceptions;

namespace Lyo.Hashing;

/// <summary>Byte-array hex formatting that delegates to <see cref="HexEncoding" /> (<c>Lyo.Hashing</c> package).</summary>
public static class ByteArrayHexExtensions
{
    extension(byte[] bytes)
    {
        /// <summary>Lowercase hexadecimal (historical default when this lived on <see cref="Extensions" />).</summary>
        public string ToHexString()
        {
            ArgumentHelpers.ThrowIfNull(bytes);
            return HexEncoding.ToHexString(bytes, TextLetterCase.Lower);
        }
    }
}