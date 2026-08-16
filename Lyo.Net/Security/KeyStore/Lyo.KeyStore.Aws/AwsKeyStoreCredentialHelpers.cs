using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;

namespace Lyo.KeyStore.Aws;

/// <summary>Resolves AWS credentials from static keys, a named shared-credentials profile, or the default chain.</summary>
internal static class AwsKeyStoreCredentialHelpers
{
    /// <summary>
    /// Returns explicit <see cref="BasicAWSCredentials" /> when both keys are non-whitespace; otherwise a named-profile credential when <paramref name="profileName" /> is set;
    /// otherwise <see langword="null" /> so callers use the default AWS credential chain.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="profileName" /> is set but the profile is not in the shared credentials/config files.</exception>
    internal static AWSCredentials? Resolve(string? accessKeyId, string? secretAccessKey, string? profileName, string? profilesLocation = null)
    {
        if (!string.IsNullOrWhiteSpace(accessKeyId) && !string.IsNullOrWhiteSpace(secretAccessKey))
            return new BasicAWSCredentials(accessKeyId, secretAccessKey);

        if (string.IsNullOrWhiteSpace(profileName))
            return null;

        var chain = string.IsNullOrWhiteSpace(profilesLocation) ? new CredentialProfileStoreChain() : new CredentialProfileStoreChain(profilesLocation);
        if (chain.TryGetAWSCredentials(profileName, out var credentials))
            return credentials;

        throw new InvalidOperationException(
            $"Unable to find the AWS profile '{profileName}'. Check the shared credentials file (~/.aws/credentials) and config (~/.aws/config).");
    }
}
