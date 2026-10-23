using Lyo.Common.Metadata.Records;

namespace Lyo.FileStorage.Policy;

/// <summary>Maps well-known file headers to <see cref="FileTypeInfo" /> rows. Shared containers are reported as the container (<c>ZIP</c>, <c>DOC</c>, <c>MKV</c>, <c>OGG</c>).</summary>
public sealed class MagicFileTypeSniffer : IFileTypeSniffer
{
    /// <inheritdoc />
    public FileTypeInfo Identify(ReadOnlySpan<byte> header)
    {
        if (header.IsEmpty)
            return FileTypeInfo.Unknown;

        FileTypeInfo? best = null;
        var bestScore = -1;
        foreach (var type in FileTypeInfo.All) {
            if (type.SignatureFamily != null)
                continue;

            foreach (var signature in type.HeaderSignatures) {
                if (signature.Matches(header) && signature.Specificity > bestScore) {
                    best = type;
                    bestScore = signature.Specificity;
                }
            }
        }

        return best ?? FileTypeInfo.Unknown;
    }
}