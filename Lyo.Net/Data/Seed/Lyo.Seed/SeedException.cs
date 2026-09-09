namespace Lyo.Seed;

/// <summary>Raised when a seed graph cannot be written (unmapped API type, transport mismatch, missing replace callback, or bulk failures).</summary>
public sealed class SeedException(string message) : Exception(message);
