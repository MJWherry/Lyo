using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;

namespace Lyo.FileStorage.S3;

/// <summary>Resolves AWS credentials from static keys, a named profile, or the default provider chain.</summary>
internal static class S3AwsCredentialHelpers
{
    /// <summary>
    /// Yields <see langword="true" /> plus <see cref="BasicAWSCredentials" /> when both keys have text; otherwise <see langword="false" /> so callers fall back to a named
    /// profile or the default AWS credential chain.
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

    /// <summary>
    /// Uses explicit <see cref="BasicAWSCredentials" /> when both keys have text; otherwise a named-profile credential when <paramref name="profileName" /> is set;
    /// otherwise <see langword="null" /> so callers use the default AWS credential chain.
    /// </summary>
    /// <exception cref="InvalidOperationException">Raised when <paramref name="profileName" /> is set but the profile is missing from the shared credentials/config files.</exception>
    internal static AWSCredentials? Resolve(string? accessKeyId, string? secretAccessKey, string? profileName, string? profilesLocation = null)
    {
        if (TryGetExplicitCredentials(accessKeyId, secretAccessKey, out var explicitCredentials))
            return explicitCredentials;

        if (string.IsNullOrWhiteSpace(profileName))
            return null;

        var chain = string.IsNullOrWhiteSpace(profilesLocation) ? new CredentialProfileStoreChain() : new CredentialProfileStoreChain(profilesLocation);
        if (chain.TryGetAWSCredentials(profileName, out var credentials))
            return credentials;

        throw new InvalidOperationException(
            $"Unable to find the AWS profile '{profileName}'. Check the shared credentials file (~/.aws/credentials) and config (~/.aws/config).");
    }
}
