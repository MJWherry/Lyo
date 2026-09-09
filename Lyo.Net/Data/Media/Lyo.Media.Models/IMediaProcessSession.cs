using Lyo.Result;

namespace Lyo.Media.Models;

/// <summary>
/// A running convert whose stdout is readable before the process exits. Dispose cancels the process.
/// Use this for PCM pumps and HTTP response bodies. Do not apply a host process timeout to the session;
/// a live stream can run for hours and is cancelled with the token or by disposing.
/// </summary>
/// <remarks>
/// <see cref="StandardOutput" /> is single-consumer. Do not read it from two threads. Dispose cancels; a parallel read may fault.
/// </remarks>
public interface IMediaProcessSession : IAsyncDisposable
{
    /// <summary>Process stdout. Read while the process is still running. Completes when the process exits or the session is disposed.</summary>
    Stream StandardOutput { get; }

    /// <summary>Completes when the process exits. Success means exit code 0.</summary>
    Task<Result<bool>> Completion { get; }
}
