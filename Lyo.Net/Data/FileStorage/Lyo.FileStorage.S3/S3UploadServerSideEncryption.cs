using Amazon.S3;
using Amazon.S3.Model;

namespace Lyo.FileStorage.S3;

internal static class S3UploadServerSideEncryption
{
    /// <summary>
    /// Returns the HTTP headers that an AWS-V4-signed PUT must include verbatim when uploading to a presigned URL produced by <see cref="ApplyToPresignedPut"/>.
    /// Returns <see langword="null"/> when no signed headers are required.
    /// </summary>
    /// <remarks>
    /// S3 V4 signing covers headers signed at presign time. If we sign SSE headers / content-type, the client MUST send those exact values, or S3 returns
    /// <c>SignatureDoesNotMatch</c>. Empty dictionary signals no additional headers are required beyond <c>Host</c>.
    /// </remarks>
    internal static IReadOnlyDictionary<string, string>? BuildRequiredPutHeaders(S3FileStorageOptions o, string? signedContentType)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(o.ServerSideEncryption)) {
            var trimmed = o.ServerSideEncryption.Trim();
            if (trimmed.Equals("AES256", StringComparison.OrdinalIgnoreCase))
                headers["x-amz-server-side-encryption"] = "AES256";
            else if (trimmed.Equals("aws:kms", StringComparison.OrdinalIgnoreCase)) {
                headers["x-amz-server-side-encryption"] = "aws:kms";
                if (!string.IsNullOrWhiteSpace(o.ServerSideEncryptionAwsKmsKeyId))
                    headers["x-amz-server-side-encryption-aws-kms-key-id"] = o.ServerSideEncryptionAwsKmsKeyId.Trim();
            }
            else if (trimmed.Equals("aws:kms:dsse", StringComparison.OrdinalIgnoreCase)) {
                headers["x-amz-server-side-encryption"] = "aws:kms:dsse";
                if (!string.IsNullOrWhiteSpace(o.ServerSideEncryptionAwsKmsKeyId))
                    headers["x-amz-server-side-encryption-aws-kms-key-id"] = o.ServerSideEncryptionAwsKmsKeyId.Trim();
            }
        }

        if (!string.IsNullOrWhiteSpace(signedContentType))
            headers["Content-Type"] = signedContentType.Trim();

        return headers.Count == 0 ? null : headers;
    }


    internal static void ApplyToPutObject(PutObjectRequest req, S3FileStorageOptions o) => ApplyDestination(req, o);

    internal static void ApplyToCopyDestination(CopyObjectRequest req, S3FileStorageOptions o) => ApplyDestination(req, o);

    internal static void ApplyToInitiateMultipart(InitiateMultipartUploadRequest req, S3FileStorageOptions o) => ApplyDestination(req, o);

    internal static void ApplyToPresignedPut(GetPreSignedUrlRequest req, S3FileStorageOptions o)
    {
        ArgumentNullException.ThrowIfNull(req);
        if (req.Verb != HttpVerb.PUT)
            return;

        ApplyDestination(req, o);
    }

    private static void ApplyDestination(PutObjectRequest req, S3FileStorageOptions o)
        => ApplyDestinationCore(
            o,
            method => req.ServerSideEncryptionMethod = method,
            kms => req.ServerSideEncryptionKeyManagementServiceKeyId = kms);

    private static void ApplyDestination(CopyObjectRequest req, S3FileStorageOptions o)
        => ApplyDestinationCore(
            o,
            method => req.ServerSideEncryptionMethod = method,
            kms => req.ServerSideEncryptionKeyManagementServiceKeyId = kms);

    private static void ApplyDestination(InitiateMultipartUploadRequest req, S3FileStorageOptions o)
        => ApplyDestinationCore(
            o,
            method => req.ServerSideEncryptionMethod = method,
            kms => req.ServerSideEncryptionKeyManagementServiceKeyId = kms);

    private static void ApplyDestination(GetPreSignedUrlRequest req, S3FileStorageOptions o)
        => ApplyDestinationCore(
            o,
            method => req.ServerSideEncryptionMethod = method,
            kms => req.ServerSideEncryptionKeyManagementServiceKeyId = kms);

    private static void ApplyDestinationCore(
        S3FileStorageOptions o,
        Action<ServerSideEncryptionMethod> assignMethod,
        Action<string?> assignKmsKey)
    {
        if (string.IsNullOrWhiteSpace(o.ServerSideEncryption))
            return;

        var trimmed = o.ServerSideEncryption.Trim();

        var method = trimmed.Equals("AES256", StringComparison.OrdinalIgnoreCase)
            ? ServerSideEncryptionMethod.AES256
            : trimmed.Equals("aws:kms", StringComparison.OrdinalIgnoreCase)
                ? ServerSideEncryptionMethod.AWSKMS
                : trimmed.Equals("aws:kms:dsse", StringComparison.OrdinalIgnoreCase)
                    ? ServerSideEncryptionMethod.AWSKMSDSSE
                    : throw new InvalidOperationException(
                        $"Unsupported ServerSideEncryption '{o.ServerSideEncryption}'. Use AES256, aws:kms, or aws:kms:dsse.");

        assignMethod(method);

        var kmsKey = string.IsNullOrWhiteSpace(o.ServerSideEncryptionAwsKmsKeyId) ? null : o.ServerSideEncryptionAwsKmsKeyId.Trim();
        if ((method == ServerSideEncryptionMethod.AWSKMS || method == ServerSideEncryptionMethod.AWSKMSDSSE) &&
            kmsKey != null)
            assignKmsKey(kmsKey);
    }
}
