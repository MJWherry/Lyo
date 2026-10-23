namespace Lyo.Rfid;

/// <summary>
/// Sends one ISO 15693 command and returns the tag response. Both buffers include the CRC-16 the caller already appended. A PN5180 or other 15693 reader implements this.
/// Type 2 NFC readers do not.
/// </summary>
public interface IIso15693Transceiver
{
    /// <param name="frame">Command bytes including the ISO 15693 CRC.</param>
    /// <param name="cancellationToken">Cancels before the frame is sent.</param>
    /// <returns>Tag response including flags and CRC.</returns>
    Task<ReadOnlyMemory<byte>> TransceiveAsync(ReadOnlyMemory<byte> frame, CancellationToken cancellationToken);
}