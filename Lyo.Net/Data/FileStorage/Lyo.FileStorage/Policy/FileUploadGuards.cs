using Lyo.FileStorage.Models;

namespace Lyo.FileStorage.Policy;

/// <summary>Shared header gate used by streamed saves, rename, and direct-upload finalize.</summary>
internal static class FileUploadGuards
{
    /// <summary>True when signature alignment or a sniffed-type list needs the leading bytes.</summary>
    public static bool RequiresHeaderInspection(FileStorageServiceBaseOptions options)
        => options.VerifyFileSignature || options.AllowedSniffedFileTypes is not null || options.DeniedSniffedFileTypes is not null;

    /// <summary>Runs signature alignment and the sniffed-type list against one header.</summary>
    public static void EnforceHeader(FileStorageServiceBaseOptions options, IFileTypeSniffer sniffer, ReadOnlySpan<byte> header, string? originalFileName, string? contentType)
    {
        if (options.VerifyFileSignature)
            FileSignatureAlignment.ThrowIfMismatch(sniffer, header, originalFileName, contentType);

        if (options.AllowedSniffedFileTypes is not null || options.DeniedSniffedFileTypes is not null)
            FileSniffedTypeFilter.ThrowIfRejected(options, sniffer.Identify(header));
    }
}
