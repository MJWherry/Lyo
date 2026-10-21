using System.Text.RegularExpressions;
using Lyo.Common.Core.Extensions;
using Lyo.Exceptions;
using Lyo.FileStorage.Models;

namespace Lyo.FileStorage.Policy;

/// <summary>Applies the configured file-name whitelist or blacklist.</summary>
public static class FileNamePatternFilter
{
    /// <summary>Match timeout so a hostile pattern cannot stall an upload.</summary>
    public static readonly TimeSpan MatchTimeout = TimeSpan.FromMilliseconds(100);

    /// <summary>Rejects <paramref name="originalFileName" /> when a file-name list is configured and the name is missing or fails that list. Null lists allow every name.</summary>
    /// <param name="options">Storage options that carry the patterns.</param>
    /// <param name="originalFileName">Name supplied by the caller.</param>
    /// <exception cref="FilePolicyRejectedException">The name is missing or does not satisfy the configured list.</exception>
    public static void ThrowIfRejected(FileStorageServiceBaseOptions options, string? originalFileName)
    {
        ArgumentHelpers.ThrowIfNull(options);
        var allowed = options.AllowedFileNamePatterns;
        var denied = options.DeniedFileNamePatterns;
        if (allowed is null && denied is null)
            return;

        if (originalFileName.IsNullOrWhitespace())
            throw new FilePolicyRejectedException("Original file name is required for this storage configuration.");

        if (allowed is not null) {
            if (allowed.Count == 0 || !allowed.Any(pattern => IsMatch(pattern, originalFileName)))
                throw new FilePolicyRejectedException($"File name '{originalFileName}' is not allowed.");

            return;
        }

        if (denied!.Any(pattern => IsMatch(pattern, originalFileName)))
            throw new FilePolicyRejectedException($"File name '{originalFileName}' is not allowed.");
    }

    private static bool IsMatch(string pattern, string originalFileName)
    {
        try {
            return Regex.IsMatch(originalFileName, pattern, RegexOptions.CultureInvariant, MatchTimeout);
        }
        catch (RegexMatchTimeoutException) {
            throw new FilePolicyRejectedException($"File name '{originalFileName}' could not be checked against the configured pattern in time.");
        }
    }
}