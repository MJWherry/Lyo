namespace Lyo.Web.Components;

/// <summary>General browser interop helpers (clipboard, downloads, timezone).</summary>
public interface IJsInterop
{
    Task SendToClipboard(string text);

    Task<TimeZoneInfo> GetClientTimeZoneInfo();

    Task DownloadFileFromStream(Stream stream, string fileName, string fileType);

#if NET6_0_OR_GREATER
    Task DownloadFileFromStreamReference(Stream stream, string fileName, string fileType);
#endif

    Task DownloadLargeFileFromStream(Stream stream, string fileName, string fileType, int chunkSize = 1024 * 1024);

    Task DownloadFileWithCallback(Func<Stream> streamFactory, string fileName, string fileType);

    Task DownloadFileWithCallback(Func<Task<Stream>> streamFactory, string fileName, string fileType);

    Task DownloadFile(byte[] data, string fileName, string fileType);
}