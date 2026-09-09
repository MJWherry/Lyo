#if HAS_SPAN
using System.Buffers;
#endif

namespace Lyo.QRCode.Encoding.Iso;

internal sealed partial class QRIsoEncoder
{
    /// <summary>
    /// One QR codeword block. The symbol is split into blocks for error correction. Each block holds data codewords followed by error-correction codewords.
    /// </summary>
    private readonly struct CodewordBlock
    {
        /// <summary>Builds a CodewordBlock from data-codeword offsets and ECC words.</summary>
        /// <param name="codeWordsOffset">Offset of the data codewords in the main BitArray. Data codewords hold the payload.</param>
        /// <param name="codeWordsLength">Length in bits of the data codewords in the main BitArray.</param>
        /// <param name="eccWords">Error-correction codewords for this block. Used to recover data if the symbol is damaged.</param>
        public CodewordBlock(int codeWordsOffset, int codeWordsLength, ArraySegment<byte> eccWords)
        {
            CodeWordsOffset = codeWordsOffset;
            CodeWordsLength = codeWordsLength;
            ECCWords = eccWords;
        }

        /// <summary>Offset of the data codewords in the BitArray.</summary>
        public int CodeWordsOffset { get; }

        /// <summary>Length of the data codewords in the BitArray.</summary>
        public int CodeWordsLength { get; }

        /// <summary>Error-correction codewords for this block.</summary>
        public ArraySegment<byte> ECCWords { get; }

        private static List<CodewordBlock>? _codewordBlocks;

        public static List<CodewordBlock> GetList(int capacity) => Interlocked.Exchange(ref _codewordBlocks, null) ?? new List<CodewordBlock>(capacity);

        public static void ReturnList(List<CodewordBlock> list)
        {
#if HAS_SPAN
            foreach (var item in list)
                ArrayPool<byte>.Shared.Return(item.ECCWords.Array!);
#endif
            list.Clear();
            Interlocked.CompareExchange(ref _codewordBlocks, list, null);
        }
    }
}