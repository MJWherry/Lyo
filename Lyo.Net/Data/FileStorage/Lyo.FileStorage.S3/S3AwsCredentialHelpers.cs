using Amazon.Runtime;

namespace Lyo.FileStorage.S3;

/// <summary>Shared rules for when <see cref="S3FileStorageOptions" /> static AWS keys should be used vs the default credential chain.</summary>
internal static class S3AwsCredentialHelpers
{
    /// <summary>
    /// Returns <see langword="true" /> and a <see cref="BasicAWSCredentials" /> instance when both keys are non-whitespace; otherwise
    /// <see langword="false" /> (callers should use the default AWS credential chain).
    /// </summary>
    internal static bool TryGetExplicitCredentials(string? accessKeyId, string? secretAccessKey, out BasicAWSCredentials? credentials)
    {
        if (!string.IsNullOrWhiteSpace(accessKeyId) && !string.IsNullOrWhiteSpace(secretAccessKey)) {
            credentials = new(accessKeyId, secretAccessKey);
            return true;
        }

        credentials = null;
        return false;
    }
}
