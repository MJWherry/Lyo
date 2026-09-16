namespace Lyo.Web.WebRenderer.Models;

/// <summary>Writes an HTML capture to the file named by <see cref="WebRenderReq.OutputFilePath" />.</summary>
public interface IWebRenderGenerator
{
    /// <summary>
    /// Captures <paramref name="request" /> and writes the file. The implementation owns the browser.
    /// Callers that only need HTML rendering use the in-process renderer instead of this contract.
    /// </summary>
    /// <param name="request">HTML, format, and destination path.</param>
    /// <param name="ct">Token used to cancel the capture.</param>
    /// <returns>The written file, or a failed result when generation cannot complete.</returns>
    Task<WebRenderRes> GenerateAsync(WebRenderReq request, CancellationToken ct = default);
}
