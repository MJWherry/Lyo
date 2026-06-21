using System.Net;
using System.Reflection;
using Amazon.S3;
using Amazon.S3.Model;

namespace Lyo.FileStorage.S3.Tests.Support;

/// <summary>
/// Lightweight <see cref="DispatchProxy" />-based stub for <see cref="IAmazonS3" />. Records the requests issued by <c>S3UploadStream</c>-style call sites and lets each
/// handler return a custom response without pulling in a mocking framework. Any method not explicitly handled returns a default-shaped <see cref="Task" />/
/// <see cref="Task{TResult}" /> instead of throwing, so tests stay focused on the methods that matter.
/// </summary>
public class FakeAmazonS3 : DispatchProxy
{
    public List<PutObjectRequest> PutObjectRequests { get; } = [];

    public List<InitiateMultipartUploadRequest> InitiateRequests { get; } = [];

    public List<UploadPartRequest> UploadPartRequests { get; } = [];

    public List<CompleteMultipartUploadRequest> CompleteRequests { get; } = [];

    public List<AbortMultipartUploadRequest> AbortRequests { get; } = [];

    public List<GetPreSignedUrlRequest> PreSignedUrlRequests { get; } = [];

    public List<GetObjectMetadataRequest> GetObjectMetadataRequests { get; } = [];

    public List<DeleteObjectRequest> DeleteObjectRequests { get; } = [];

    public List<GetObjectRequest> GetObjectRequests { get; } = [];

    public Func<PutObjectRequest, PutObjectResponse>? OnPutObject { get; set; }

    public Func<InitiateMultipartUploadRequest, InitiateMultipartUploadResponse>? OnInitiateMultipart { get; set; }

    public Func<UploadPartRequest, UploadPartResponse>? OnUploadPart { get; set; }

    public Func<CompleteMultipartUploadRequest, CompleteMultipartUploadResponse>? OnCompleteMultipart { get; set; }

    public Func<AbortMultipartUploadRequest, AbortMultipartUploadResponse>? OnAbortMultipart { get; set; }

    public Func<GetPreSignedUrlRequest, string>? OnGetPreSignedUrl { get; set; }

    public Func<GetObjectMetadataRequest, GetObjectMetadataResponse>? OnGetObjectMetadata { get; set; }

    public Func<DeleteObjectRequest, DeleteObjectResponse>? OnDeleteObject { get; set; }

    public Func<GetObjectRequest, GetObjectResponse>? OnGetObject { get; set; }

    /// <summary>If non-null, the next call to the matching <c>On*</c> handler throws this exception (and is then cleared).</summary>
    public Exception? ThrowOnNextUploadPart { get; set; }

    public Exception? ThrowOnNextComplete { get; set; }

    public static IAmazonS3 Create(out FakeAmazonS3 fake)
    {
        var proxy = Create<IAmazonS3, FakeAmazonS3>();
        fake = (FakeAmazonS3)proxy;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod is null)
            throw new InvalidOperationException("DispatchProxy invoked without target method.");

        var name = targetMethod.Name;
        switch (name) {
            case "PutObjectAsync" when args is { Length: 2 } && args[0] is PutObjectRequest put: {
                PutObjectRequests.Add(put);
                var response = OnPutObject?.Invoke(put) ?? new PutObjectResponse();
                return Task.FromResult(response);
            }
            case "InitiateMultipartUploadAsync" when args is { Length: 2 } && args[0] is InitiateMultipartUploadRequest init: {
                InitiateRequests.Add(init);
                var response = OnInitiateMultipart?.Invoke(init) ?? new InitiateMultipartUploadResponse { UploadId = "uploadid-test" };
                return Task.FromResult(response);
            }
            case "UploadPartAsync" when args is { Length: 2 } && args[0] is UploadPartRequest part: {
                if (ThrowOnNextUploadPart is { } ex) {
                    ThrowOnNextUploadPart = null;
                    return Task.FromException<UploadPartResponse>(ex);
                }

                UploadPartRequests.Add(part);
                var response = OnUploadPart?.Invoke(part) ?? new UploadPartResponse { ETag = $"\"etag-{part.PartNumber}\"" };
                return Task.FromResult(response);
            }
            case "CompleteMultipartUploadAsync" when args is { Length: 2 } && args[0] is CompleteMultipartUploadRequest complete: {
                if (ThrowOnNextComplete is { } ex) {
                    ThrowOnNextComplete = null;
                    return Task.FromException<CompleteMultipartUploadResponse>(ex);
                }

                CompleteRequests.Add(complete);
                var response = OnCompleteMultipart?.Invoke(complete) ?? new CompleteMultipartUploadResponse();
                return Task.FromResult(response);
            }
            case "AbortMultipartUploadAsync" when args is { Length: 2 } && args[0] is AbortMultipartUploadRequest abort: {
                AbortRequests.Add(abort);
                var response = OnAbortMultipart?.Invoke(abort) ?? new AbortMultipartUploadResponse();
                return Task.FromResult(response);
            }
            case "GetPreSignedURLAsync" when args is { Length: 1 } && args[0] is GetPreSignedUrlRequest presign: {
                PreSignedUrlRequests.Add(presign);
                var url = OnGetPreSignedUrl?.Invoke(presign) ?? $"https://s3.test/{presign.BucketName}/{presign.Key}?X-Amz-Signature=fake";
                return Task.FromResult(url);
            }
            case "GetObjectMetadataAsync" when args is { Length: 3 } && args[0] is string bucket && args[1] is string key: {
                var metaReq = new GetObjectMetadataRequest { BucketName = bucket, Key = key };
                GetObjectMetadataRequests.Add(metaReq);
                if (OnGetObjectMetadata?.Invoke(metaReq) is { } meta)
                    return Task.FromResult(meta);

                return Task.FromException<GetObjectMetadataResponse>(new AmazonS3Exception("Not Found") { StatusCode = HttpStatusCode.NotFound });
            }
            case "DeleteObjectAsync" when args is { Length: 3 } && args[0] is string delBucket && args[1] is string delKey: {
                var delReq = new DeleteObjectRequest { BucketName = delBucket, Key = delKey };
                DeleteObjectRequests.Add(delReq);
                var response = OnDeleteObject?.Invoke(delReq) ?? new DeleteObjectResponse();
                return Task.FromResult(response);
            }
            case "GetObjectAsync" when args is { Length: 3 } && args[0] is string getBucket && args[1] is string getKey: {
                var getReq = new GetObjectRequest { BucketName = getBucket, Key = getKey };
                GetObjectRequests.Add(getReq);
                if (OnGetObject?.Invoke(getReq) is { } obj)
                    return Task.FromResult(obj);

                return Task.FromResult(new GetObjectResponse { ResponseStream = new MemoryStream(new byte[64]) });
            }
        }

        return DefaultReturnValue(targetMethod);
    }

    /// <summary>
    /// Default fallback: returns a <see cref="Task" />-shaped value for async methods, a default-constructed response otherwise. Tests should override <c>OnX</c> for any method
    /// whose behavior matters.
    /// </summary>
    private static object? DefaultReturnValue(MethodInfo method)
    {
        var rt = method.ReturnType;
        if (rt == typeof(Task))
            return Task.CompletedTask;

        if (rt == typeof(ValueTask))
            return ValueTask.CompletedTask;

        if (rt.IsGenericType) {
            var def = rt.GetGenericTypeDefinition();
            if (def == typeof(Task<>) || def == typeof(ValueTask<>)) {
                var inner = rt.GenericTypeArguments[0];
                var defaultValue = inner.IsValueType ? Activator.CreateInstance(inner) : null;
                if (inner == typeof(string))
                    defaultValue = "";

                if (def == typeof(Task<>))
                    return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(inner).Invoke(null, [defaultValue]);
            }
        }

        if (rt == typeof(void))
            return null;

        return rt.IsValueType ? Activator.CreateInstance(rt) : null;
    }
}