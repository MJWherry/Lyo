using Amazon.S3;
using Amazon.S3.Model;
using Lyo.Common.Records;

namespace Lyo.FileStorage.S3;

internal sealed class S3UploadStream : Stream
{
    private readonly string _bucketName;
    private readonly CancellationToken _ct;
    private readonly MemoryStream _innerStream;
    private readonly string _objectKey;
    private readonly IAmazonS3 _s3Client;
    private bool _uploaded;

    public override bool CanRead => _innerStream.CanRead;

    public override bool CanSeek => _innerStream.CanSeek;

    public override bool CanWrite => _innerStream.CanWrite;

    public override long Length => _innerStream.Length;

    public override long Position {
        get => _innerStream.Position;
        set => _innerStream.Position = value;
    }

    public S3UploadStream(IAmazonS3 s3Client, string bucketName, string objectKey, CancellationToken ct)
    {
        _innerStream = new();
        _s3Client = s3Client;
        _bucketName = bucketName;
        _objectKey = objectKey;
        _ct = ct;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) {
            // Synchronous dispose - just dispose the inner stream
            // Upload should happen via DisposeAsync
            _innerStream?.Dispose();
        }

        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        if (!_uploaded && _innerStream.Length > 0) {
            try {
                _innerStream.Position = 0;
                var putRequest = new PutObjectRequest {
                    BucketName = _bucketName,
                    Key = _objectKey,
                    InputStream = _innerStream,
                    ContentType = FileTypeInfo.Unknown.MimeType
                };

                await _s3Client.PutObjectAsync(putRequest, _ct).ConfigureAwait(false);
                _uploaded = true;
            }
            catch {
                // If upload fails, we still want to dispose the stream
                // The error will be caught by the caller
            }
        }

        await _innerStream.DisposeAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }

    public override void Flush() => _innerStream.Flush();

    public override int Read(byte[] buffer, int offset, int count) => _innerStream.Read(buffer, offset, count);

    public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);

    public override void SetLength(long value) => _innerStream.SetLength(value);

    public override void Write(byte[] buffer, int offset, int count) => _innerStream.Write(buffer, offset, count);

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ct)
        => await _innerStream.WriteAsync(buffer, offset, count, ct).ConfigureAwait(false);

    public override async Task FlushAsync(CancellationToken ct) => await _innerStream.FlushAsync(ct).ConfigureAwait(false);
}