using Lyo.Common.Metadata.Records;
using Lyo.Exceptions;
using Lyo.FileStorage.Models;

namespace Lyo.FileStorage.Policy;

/// <summary>Applies the configured sniffed-type whitelist or blacklist.</summary>
public static class FileSniffedTypeFilter
{
    /// <summary>
    /// Rejects <paramref name="sniffed" /> when a sniffed-type list is configured and the type fails that list. An unrecognized header fails a whitelist and passes a blacklist.
    /// Names in the same signature family match one another.
    /// </summary>
    /// <param name="options">Storage options that carry the lists.</param>
    /// <param name="sniffed">Type returned by <see cref="IFileTypeSniffer.Identify" />.</param>
    /// <exception cref="FilePolicyRejectedException">The sniffed type is not permitted.</exception>
    public static void ThrowIfRejected(FileStorageServiceBaseOptions options, FileTypeInfo sniffed)
    {
        ArgumentHelpers.ThrowIfNull(options);
        ArgumentHelpers.ThrowIfNull(sniffed);
        var allowed = options.AllowedSniffedFileTypes;
        var denied = options.DeniedSniffedFileTypes;
        if (allowed is null && denied is null)
            return;

        if (allowed is not null) {
            if (allowed.Count == 0 || !allowed.Any(name => FileSignatureFamilies.MatchesConfiguredName(sniffed, name)))
                throw new FilePolicyRejectedException($"Sniffed file type '{sniffed.Name}' is not allowed.");

            return;
        }

        if (denied!.Any(name => FileSignatureFamilies.MatchesConfiguredName(sniffed, name)))
            throw new FilePolicyRejectedException($"Sniffed file type '{sniffed.Name}' is not allowed.");
    }
}