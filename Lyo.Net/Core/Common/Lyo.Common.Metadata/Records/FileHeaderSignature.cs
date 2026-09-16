using Lyo.Exceptions;

namespace Lyo.Common.Metadata.Records;

/// <summary>One byte run inside a <see cref="FileHeaderSignature" />. Offset is from the start of the file.</summary>
public readonly struct FileHeaderMark
{
    /// <summary>Copies <paramref name="magic" /> so the mark outlives the caller's span.</summary>
    /// <param name="offset">Zero-based index of the first magic byte.</param>
    /// <param name="magic">Bytes that must appear at <paramref name="offset" />.</param>
    public FileHeaderMark(int offset, ReadOnlySpan<byte> magic)
    {
        ArgumentHelpers.ThrowIfNegative(offset);
        ArgumentHelpers.ThrowIf(magic.IsEmpty, "Magic bytes are required.");
        Offset = offset;
        Magic = magic.ToArray();
    }

    /// <summary>Zero-based index of <see cref="Magic" />.</summary>
    public int Offset { get; }

    /// <summary>Bytes that must match at <see cref="Offset" />.</summary>
    public ReadOnlyMemory<byte> Magic { get; }
}

/// <summary>
/// One complete file-header alternative. A <see cref="FileTypeInfo" /> holds a list of these when the type has several different headers. An entry has one mark for a
/// contiguous prefix, or two when a second discriminator sits later in the header.
/// </summary>
public sealed class FileHeaderSignature
{
    private readonly FileHeaderMark[] _marks;

    private FileHeaderSignature(FileHeaderMark[] marks)
    {
        _marks = marks;
        var specificity = 0;
        foreach (var mark in marks)
            specificity += mark.Magic.Length;

        Specificity = specificity;
    }

    /// <summary>Marks that must all match for this alternative.</summary>
    public IReadOnlyList<FileHeaderMark> Marks => _marks;

    /// <summary>Sum of mark lengths. A longer match wins over a shorter one that also matches.</summary>
    public int Specificity { get; }

    /// <summary>One mark at <paramref name="offset" />.</summary>
    public static FileHeaderSignature At(int offset, ReadOnlySpan<byte> magic) => new([new FileHeaderMark(offset, magic)]);

    /// <summary>Two marks. Both must match. Used when the type is decided by bytes that are not adjacent, such as a RIFF form type.</summary>
    public static FileHeaderSignature At(int offset, ReadOnlySpan<byte> magic, int secondOffset, ReadOnlySpan<byte> secondMagic)
        => new([new FileHeaderMark(offset, magic), new FileHeaderMark(secondOffset, secondMagic)]);

    /// <summary>True when every mark is present in <paramref name="header" />.</summary>
    public bool Matches(ReadOnlySpan<byte> header)
    {
        foreach (var mark in _marks) {
            var magic = mark.Magic.Span;
            if ((uint)mark.Offset > (uint)header.Length || header.Length - mark.Offset < magic.Length)
                return false;

            if (!header.Slice(mark.Offset, magic.Length).SequenceEqual(magic))
                return false;
        }

        return _marks.Length > 0;
    }
}
