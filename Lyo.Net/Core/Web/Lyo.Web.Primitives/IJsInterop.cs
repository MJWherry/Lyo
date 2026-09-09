namespace Lyo.Web.Primitives;

/// <summary>Browser interop helpers for clipboard, downloads, and timezone.</summary>
public interface IJsInterop
{
    Task SendToClipboard(string text);

    /// <summary>Reads plain text from the browser clipboard (needs a user gesture and permission).</summary>
    Task<string> ReadClipboardTextAsync();

    /// <summary>Reads the browser IANA zone from <c>lyoTimeZone.js</c> (<c>Intl.DateTimeFormat</c>). Unknown ids fall back to UTC.</summary>
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