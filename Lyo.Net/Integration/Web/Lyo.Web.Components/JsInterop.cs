using Microsoft.JSInterop;

namespace Lyo.Web.Components;

/// <inheritdoc />
public sealed class JsInterop(IJSRuntime js) : IJsInterop
{
    /// <inheritdoc />
    public async Task SendToClipboard(string text) => await js.InvokeVoidAsync("navigator.clipboard.writeText", text);

    /// <inheritdoc />
    public async Task<TimeZoneInfo> GetClientTimeZoneInfo()
    {
        var clientTimeZone = await js.InvokeAsync<string>("getClientTimeZone");
        return TimeZoneInfo.FindSystemTimeZoneById(clientTimeZone);
    }

    /// <inheritdoc />
    public async Task DownloadFileFromStream(Stream stream, string fileName, string fileType)
    {
        if (stream.CanSeek)
            stream.Position = 0;

        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        var data = memoryStream.ToArray();
        var base64 = Convert.ToBase64String(data);
        await js.InvokeVoidAsync("downloadFileFromBase64", base64, fileType, fileName);
    }

#if NET6_0_OR_GREATER
    /// <inheritdoc />
    public async Task DownloadFileFromStreamReference(Stream stream, string fileName, string fileType)
    {
        if (stream.CanSeek)
            stream.Position = 0;

        using var streamRef = new DotNetStreamReference(stream);
        await js.InvokeVoidAsync("downloadFileFromStream", streamRef, fileType, fileName);
    }
#endif

    /// <inheritdoc />
    public async Task DownloadLargeFileFromStream(Stream stream, string fileName, string fileType, int chunkSize = 1024 * 1024)
    {
        if (stream.CanSeek)
            stream.Position = 0;

        await js.InvokeVoidAsync("initializeChunkedDownload", fileName, fileType);
        var buffer = new byte[chunkSize];
        int bytesRead;
        var chunkIndex = 0;
        while ((bytesRead = await stream.ReadAsync(buffer, 0, chunkSize)) > 0) {
            var chunk = new byte[bytesRead];
            Array.Copy(buffer, chunk, bytesRead);
            var base64Chunk = Convert.ToBase64String(chunk);
            await js.InvokeVoidAsync("appendChunkToDownload", base64Chunk, chunkIndex);
            chunkIndex++;
        }

        await js.InvokeVoidAsync("finalizeChunkedDownload");
    }

    /// <inheritdoc />
    public async Task DownloadFileWithCallback(Func<Stream> streamFactory, string fileName, string fileType)
    {
        await using var stream = streamFactory();
        await DownloadFileFromStream(stream, fileName, fileType);
    }

    /// <inheritdoc />
    public async Task DownloadFileWithCallback(Func<Task<Stream>> streamFactory, string fileName, string fileType)
    {
        await using var stream = await streamFactory();
        await DownloadFileFromStream(stream, fileName, fileType);
    }

    /// <inheritdoc />
    public async Task DownloadFile(byte[] data, string fileName, string fileType)
    {
        var base64 = Convert.ToBase64String(data);
        await js.InvokeVoidAsync("downloadFileFromBase64", base64, fileType, fileName);
    }
}