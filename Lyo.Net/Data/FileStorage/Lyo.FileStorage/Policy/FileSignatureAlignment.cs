using Lyo.Common.Metadata.Records;
using Lyo.Exceptions;

namespace Lyo.FileStorage.Policy;

/// <summary>Checks that a sniffed header agrees with the file name and content type the caller claimed.</summary>
public static class FileSignatureAlignment
{
    /// <summary>
    /// Throws when the header disagrees with the claimed type. The claim is the filename extension when that extension is known, otherwise the declared content type. A claimed
    /// type with a signature must match that signature (or its family). A claimed type with no signature is rejected when the header is some other known type.
    /// </summary>
    /// <param name="sniffer">Header identifier.</param>
    /// <param name="header">Leading bytes of the payload.</param>
    /// <param name="originalFileName">Caller file name. The extension wins over <paramref name="declaredContentType" /> when it maps to a catalog row.</param>
    /// <param name="declaredContentType">Caller content type, used when the file name has no known extension.</param>
    /// <exception cref="FilePolicyRejectedException">The header does not line up with the claim.</exception>
    public static void ThrowIfMismatch(IFileTypeSniffer sniffer, ReadOnlySpan<byte> header, string? originalFileName, string? declaredContentType)
    {
        ArgumentHelpers.ThrowIfNull(sniffer);
        var sniffed = sniffer.Identify(header);
        var fromName = FileTypeInfo.FromFilePath(originalFileName);
        var claimed = fromName != FileTypeInfo.Unknown ? fromName : FileTypeInfo.FromMimeType(declaredContentType);
        if (FileSignatureFamilies.HasRequiredSignature(claimed)) {
            if (!FileSignatureFamilies.Agrees(sniffed, claimed))
                throw new FilePolicyRejectedException($"File header does not match claimed type '{claimed.Name}'.");

            return;
        }

        if (sniffed != FileTypeInfo.Unknown)
            throw new FilePolicyRejectedException($"File header is {sniffed.Name}, which does not match claimed type '{claimed.Name}'.");
    }
}