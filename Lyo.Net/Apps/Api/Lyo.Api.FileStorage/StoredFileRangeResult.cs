using Lyo.Common.Core.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Lyo.Api.FileStorage;

/// <summary>416 for one well-formed byte range that starts at or past the end of a seekable media file.</summary>
internal sealed class UnsatisfiableStoredFileResult(long total) : IResult
{
    /// <inheritdoc />
    public Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = StatusCodes.Status416RangeNotSatisfiable;
        httpContext.Response.Headers.AcceptRanges = "bytes";
        httpContext.Response.Headers.ContentRange = $"bytes */{total}";
        return Task.CompletedTask;
    }
}

/// <summary>206 body for one plain media byte range. The stream is already the slice; it is not seekable, so range processing stays off.</summary>
internal sealed class PartialStoredFileResult(Stream stream, string mime, string? fileName, bool inline, long start, long endInclusive, long total) : IResult
{
    /// <inheritdoc />
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        var response = httpContext.Response;
        httpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
        response.StatusCode = StatusCodes.Status206PartialContent;
        response.ContentType = mime;
        response.ContentLength = endInclusive - start + 1;
        response.Headers.AcceptRanges = "bytes";
        response.Headers.ContentRange = $"bytes {start}-{endInclusive}/{total}";
        response.Headers.ContentDisposition = ContentDisposition.Build(fileName, inline);
        await response.StartAsync(httpContext.RequestAborted).ConfigureAwait(false);
        await using (stream)
            await stream.CopyToAsync(response.Body, 16 * 1024, httpContext.RequestAborted).ConfigureAwait(false);
    }
}
