using System.Collections.Concurrent;
using System.Net;
using Amazon.S3;
using Amazon.S3.Model;

namespace Lyo.FileStorage.S3.Tests.Support;

/// <summary>Dictionary-backed S3 bucket used by <see cref="S3FileSystem" /> tests (prefix + delimiter listing included).</summary>
public sealed class InMemoryS3Bucket
{
    private readonly ConcurrentDictionary<string, (byte[] Data, DateTimeOffset LastModified)> _objects = new(StringComparer.Ordinal);

    /// <summary>Creates an <see cref="IAmazonS3" /> that reads and writes this in-memory bucket.</summary>
    public IAmazonS3 CreateClient()
    {
        var client = FakeAmazonS3.Create(out var fake);
        fake.OnPutObject = req => {
            var data = ReadBody(req);
            _objects[req.Key] = (data, DateTimeOffset.UtcNow);
            return new();
        };
        fake.OnGetObject = req => {
            if (!_objects.TryGetValue(req.Key, out var entry))
                throw new AmazonS3Exception("Not Found") { StatusCode = HttpStatusCode.NotFound };

            return new GetObjectResponse {
                ResponseStream = new MemoryStream(entry.Data, writable: false),
                ContentLength = entry.Data.Length,
                LastModified = entry.LastModified.UtcDateTime
            };
        };
        fake.OnGetObjectMetadata = req => {
            if (!_objects.TryGetValue(req.Key, out var entry))
                throw new AmazonS3Exception("Not Found") { StatusCode = HttpStatusCode.NotFound };

            return new() { ContentLength = entry.Data.Length, LastModified = entry.LastModified.UtcDateTime };
        };
        fake.OnDeleteObject = req => {
            _objects.TryRemove(req.Key, out _);
            return new();
        };
        fake.OnCopyObject = req => {
            if (!_objects.TryGetValue(req.SourceKey, out var entry))
                throw new AmazonS3Exception("Not Found") { StatusCode = HttpStatusCode.NotFound };

            _objects[req.DestinationKey] = (entry.Data.ToArray(), DateTimeOffset.UtcNow);
            return new();
        };
        fake.OnListObjectsV2 = List;
        return client;
    }

    private ListObjectsV2Response List(ListObjectsV2Request req)
    {
        var prefix = req.Prefix ?? "";
        var delimiter = req.Delimiter;
        var objects = new List<S3Object>();
        var prefixes = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (key, entry) in _objects) {
            if (!key.StartsWith(prefix, StringComparison.Ordinal))
                continue;

            if (string.IsNullOrEmpty(delimiter)) {
                objects.Add(new() { Key = key, Size = entry.Data.Length, LastModified = entry.LastModified.UtcDateTime });
                continue;
            }

            var rest = key[prefix.Length..];
            var slash = rest.IndexOf(delimiter, StringComparison.Ordinal);
            if (slash < 0) {
                objects.Add(new() { Key = key, Size = entry.Data.Length, LastModified = entry.LastModified.UtcDateTime });
                continue;
            }

            var common = prefix + rest[..(slash + delimiter.Length)];
            if (seen.Add(common))
                prefixes.Add(common);
        }

        return new() { S3Objects = objects, CommonPrefixes = prefixes, IsTruncated = false };
    }

    private static byte[] ReadBody(PutObjectRequest req)
    {
        if (req.InputStream != null) {
            using var ms = new MemoryStream();
            req.InputStream.CopyTo(ms);
            return ms.ToArray();
        }

        return req.ContentBody == null ? [] : System.Text.Encoding.UTF8.GetBytes(req.ContentBody);
    }
}
