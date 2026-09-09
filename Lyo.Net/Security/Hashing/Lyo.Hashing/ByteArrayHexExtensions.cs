using Lyo.Common.Core.Enums;
using Lyo.Exceptions;

namespace Lyo.Hashing;

/// <summary>Hex formatting for byte arrays, delegated to <see cref="HexEncoding" /> (<c>Lyo.Hashing</c> package).</summary>
public static class ByteArrayHexExtensions
{
    extension(byte[] bytes)
    {
        /// <summary>Lowercase hex — the historic default when this lived on <see cref="Extensions" />.</summary>
        public string ToHexString()
        {
            ArgumentHelpers.ThrowIfNull(bytes);
            return HexEncoding.ToHexString(bytes, TextLetterCase.Lower);
        }
    }
}