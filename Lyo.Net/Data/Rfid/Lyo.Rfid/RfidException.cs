namespace Lyo.Rfid;

/// <summary>ISO 15693 read, write, or frame error. The message names the failed check.</summary>
public sealed class RfidException(string message)
    : Exception(message);