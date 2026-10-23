namespace Lyo.ClamAv.Client;

/// <summary>Scans a stream through clamav-rest and reads the engine version.</summary>
public interface IClamAvClient
{
    /// <summary>Posts <paramref name="content" /> to the scan endpoint.</summary>
    /// <param name="content">Readable payload. The caller keeps ownership of the stream.</param>
    /// <param name="fileName">File name sent as the multipart file part.</param>
    /// <param name="cancellationToken">Cancels the HTTP call.</param>
    Task<ClamScanResult> ScanAsync(Stream content, string fileName, CancellationToken cancellationToken = default);

    /// <summary>Reads the plain-text engine version.</summary>
    /// <param name="cancellationToken">Cancels the HTTP call.</param>
    Task<string> GetVersionAsync(CancellationToken cancellationToken = default);
}
