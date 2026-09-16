namespace Lyo.Nfc;

/// <summary>
/// Lists PC/SC (or other) readers and opens a tag session. A null reader name means the first reader the implementation finds.
/// </summary>
public interface INfcReader
{
    /// <summary>Reader names the host can see. Empty when none are attached. Does not open a tag.</summary>
    IReadOnlyList<string> ListReaders();

    /// <summary>
    /// Waits for a tag on <paramref name="readerName"/>, or on the first reader when that argument is null or empty.
    /// </summary>
    /// <param name="readerName">Reader name from <see cref="ListReaders"/>, or null for the first reader.</param>
    /// <param name="cancellationToken">Cancels before the reader is opened.</param>
    Task<INfcTag> ConnectAsync(string? readerName, CancellationToken cancellationToken);
}
