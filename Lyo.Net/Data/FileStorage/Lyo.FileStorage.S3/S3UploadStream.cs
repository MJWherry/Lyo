using Amazon.S3;
using Amazon.S3.Model;
using Lyo.Common.Records;
using Lyo.Exceptions;

namespace Lyo.FileStorage.S3;

/// <summary>
/// Buffered S3 write stream that spools to a temp file once it exceeds a memory threshold and uploads on <see cref="DisposeAsync" />. Sync <see cref="Dispose(bool)" /> will
/// throw if unwritten data remains, because <c>PutObject</c> must run asynchronously. Use <c>await using</c> (or call <see cref="DisposeAsync" /> explicitly) to flush.
/// </summary>
internal sealed class S3UploadStream : Stream
{
    private const long DefaultInMemoryThresholdBytes = 4L * 1024 * 1024; // 4 MiB
    private const long DefaultMultipartThresholdBytes = 64L * 1024 * 1024; // 64 MiB
    private const long DefaultMultipartPartSizeBytes = 8L * 1024 * 1024; // 8 MiB per part
    private readonly string _bucketName;
    private readonly CancellationToken _ct;
    private readonly long _inMemoryThresholdBytes;
    private readonly long _multipartPartSizeBytes;
    private readonly long _multipartThresholdBytes;
    private readonly string _objectKey;
    private readonly S3FileStorageOptions _options;
    private readonly IAmazonS3 _s3Client;

    private string? _contentType = FileTypeInfo.Unknown.MimeType;
    private bool _disposed;
    private Stream _innerStream;
    private string? _spoolPath;
    private bool _spooled;
    private bool _uploaded;

    public override bool CanRead => _innerStream.CanRead;

    public override bool CanSeek => _innerStream.CanSeek;

    public override bool CanWrite => _innerStream.CanWrite;

    public override long Length => _innerStream.Length;

    public override long Position {
        get => _innerStream.Position;
        set => _innerStream.Position = value;
    }

    public S3UploadStream(IAmazonS3 s3Client, string bucketName, string objectKey, S3FileStorageOptions options, CancellationToken ct)
    {
        _s3Client = ArgumentHelpers.ThrowIfNullReturn(s3Client);
        _bucketName = bucketName;
        _objectKey = objectKey;
        _options = ArgumentHelpers.ThrowIfNullReturn(options);
        _ct = ct;
        _inMemoryThresholdBytes = DefaultInMemoryThresholdBytes;
        _multipartThresholdBytes = DefaultMultipartThresholdBytes;
        _multipartPartSizeBytes = Math.Max(5L * 1024 * 1024, DefaultMultipartPartSizeBytes);
        _innerStream = new MemoryStream();
    }

    /// <summary>Sets the Content-Type header used when uploading. Has no effect after the upload has run.</summary>
    public void SetContentType(string? contentType)
    {
        if (!string.IsNullOrWhiteSpace(contentType))
            _contentType = contentType;
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed) {
            base.Dispose(disposing);
            return;
        }

        _disposed = true;
        if (disposing) {
            if (!_uploaded && _innerStream.Length > 0) {
                throw new NotSupportedException(
                    $"{nameof(S3UploadStream)} requires async disposal to upload pending content. Call DisposeAsync (or use 'await using') instead of sync Dispose.");
            }

            _innerStream.Dispose();
            CleanupSpoolFile();
        }

        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        if (_disposed) {
            await base.DisposeAsync().ConfigureAwait(false);
            return;
        }

        _disposed = true;
        try {
            if (!_uploaded && _innerStream.Length > 0) {
                await _innerStream.FlushAsync(_ct).ConfigureAwait(false);
                _innerStream.Position = 0;
                if (_innerStream.Length >= _multipartThresholdBytes)
                    await UploadMultipartAsync().ConfigureAwait(false);
                else
                    await UploadSinglePutAsync().ConfigureAwait(false);

                _uploaded = true;
            }

            await _innerStream.DisposeAsync().ConfigureAwait(false);
        }
        finally {
            CleanupSpoolFile();
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }

    public override void Flush() => _innerStream.Flush();

    public override int Read(byte[] buffer, int offset, int count) => _innerStream.Read(buffer, offset, count);

    public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);

    public override void SetLength(long value) => _innerStream.SetLength(value);

    public override void Write(byte[] buffer, int offset, int count)
    {
        EnsureCapacity(count);
        _innerStream.Write(buffer, offset, count);
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ct)
    {
        EnsureCapacity(count);
        await _innerStream.WriteAsync(buffer, offset, count, ct).ConfigureAwait(false);
    }

    public override async Task FlushAsync(CancellationToken ct) => await _innerStream.FlushAsync(ct).ConfigureAwait(false);

    private void EnsureCapacity(int additionalBytes)
    {
        if (_spooled)
            return;

        if (_innerStream.Length + additionalBytes < _inMemoryThresholdBytes)
            return;

        // Promote MemoryStream to FileStream backing on the same temp path.
        var spool = CreateSpoolFile();
        _innerStream.Position = 0;
        _innerStream.CopyTo(spool);
        _innerStream.Dispose();
        _innerStream = spool;
        _spooled = true;
    }

    private FileStream CreateSpoolFile()
    {
        _spoolPath = Path.Combine(Path.GetTempPath(), $"lyo-s3up-{Guid.NewGuid():N}.tmp");
        return new(_spoolPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.DeleteOnClose);
    }

    private void CleanupSpoolFile()
    {
        // FileOptions.DeleteOnClose handles removal automatically when the FileStream is disposed; this is a belt-and-braces fallback.
        if (string.IsNullOrEmpty(_spoolPath))
            return;

        try {
            if (File.Exists(_spoolPath))
                File.Delete(_spoolPath);
        }
        catch {
            // best effort
        }
    }

    private async Task UploadSinglePutAsync()
    {
        var putRequest = new PutObjectRequest {
            BucketName = _bucketName,
            Key = _objectKey,
            InputStream = _innerStream,
            ContentType = _contentType ?? FileTypeInfo.Unknown.MimeType
        };

        S3UploadServerSideEncryption.ApplyToPutObject(putRequest, _options);
        await _s3Client.PutObjectAsync(putRequest, _ct).ConfigureAwait(false);
    }

    private async Task UploadMultipartAsync()
    {
        var initiate = new InitiateMultipartUploadRequest { BucketName = _bucketName, Key = _objectKey, ContentType = _contentType ?? FileTypeInfo.Unknown.MimeType };
        S3UploadServerSideEncryption.ApplyToInitiateMultipart(initiate, _options);
        var init = await _s3Client.InitiateMultipartUploadAsync(initiate, _ct).ConfigureAwait(false);
        var uploadId = init.UploadId;
        var partETags = new List<PartETag>();
        var partNumber = 1;
        try {
            var totalLength = _innerStream.Length;
            var remaining = totalLength;
            while (remaining > 0) {
                var partSize = (int)Math.Min(_multipartPartSizeBytes, remaining);
                var partResponse = await _s3Client.UploadPartAsync(
                        new() {
                            BucketName = _bucketName,
                            Key = _objectKey,
                            UploadId = uploadId,
                            PartNumber = partNumber,
                            PartSize = partSize,
                            InputStream = _innerStream,
                            IsLastPart = remaining - partSize == 0
                        }, _ct)
                    .ConfigureAwait(false);

                partETags.Add(new(partNumber, partResponse.ETag));
                partNumber++;
                remaining -= partSize;
            }

            await _s3Client.CompleteMultipartUploadAsync(
                    new() {
                        BucketName = _bucketName,
                        Key = _objectKey,
                        UploadId = uploadId,
                        PartETags = partETags
                    }, _ct)
                .ConfigureAwait(false);
        }
        catch {
            try {
                await _s3Client.AbortMultipartUploadAsync(new() { BucketName = _bucketName, Key = _objectKey, UploadId = uploadId }, CancellationToken.None).ConfigureAwait(false);
            }
            catch {
                // best effort
            }

            throw;
        }
    }
}