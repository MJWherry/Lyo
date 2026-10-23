using Lyo.Exceptions;
using PCSC;
using PCSC.Exceptions;

namespace Lyo.Nfc.Pcsc;

/// <summary>
/// PC/SC reader list and tag sessions. On Linux the process needs <c>pcscd</c> and <c>libpcsclite</c>. This speaks ISO 14443 Type 2 pages. It does not speak ISO 15693, so a
/// Tonie figure will not answer.
/// </summary>
public sealed class PcscNfcReader : INfcReader, IDisposable
{
    private readonly ISCardContext _context;
    private bool _disposed;

    /// <summary>Establishes a system PC/SC context.</summary>
    public PcscNfcReader()
    {
        try {
            _context = ContextFactory.Instance.Establish(SCardScope.System);
        }
        catch (NoServiceException) {
            throw new NfcException("PC/SC service is not running. On Linux start pcscd.");
        }
    }

    /// <summary>Releases the PC/SC context.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _context.Dispose();
        _disposed = true;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ListReaders()
    {
        ThrowIfDisposed();
        try {
            var names = _context.GetReaders();
            return names is null || names.Length == 0 ? [] : names;
        }
        catch (NoReadersAvailableException) {
            return [];
        }
        catch (NoServiceException) {
            throw new NfcException("PC/SC service is not running. On Linux start pcscd.");
        }
    }

    /// <inheritdoc />
    public Task<INfcTag> ConnectAsync(string? readerName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        var name = readerName;
        if (string.IsNullOrWhiteSpace(name)) {
            var readers = ListReaders();
            if (readers.Count == 0)
                throw new NfcException("No PC/SC readers found.");

            name = readers[0];
        }

        try {
            var reader = _context.ConnectReader(name, SCardShareMode.Shared, SCardProtocol.Any);
            return Task.FromResult<INfcTag>(new PcscNfcTag(reader));
        }
        catch (NoServiceException) {
            throw new NfcException("PC/SC service is not running. On Linux start pcscd.");
        }
        catch (PCSCException ex) {
            throw new NfcException($"PC/SC could not open reader '{name}': {ex.Message}");
        }
    }

    private void ThrowIfDisposed() => OperationHelpers.ThrowIfDisposed(_disposed, nameof(PcscNfcReader));
}