namespace Lyo.Nfc;

/// <summary>NFC read, write, or frame error. The message names the failed check.</summary>
public sealed class NfcException(string message)
    : Exception(message);