namespace Lyo.TextEncoding;

/// <summary>Thrown when charset resolution or binary-codec payload handling fails.</summary>
public sealed class EncodingException : Exception
{
    /// <summary>Mints with a message.</summary>
    public EncodingException(string message)
        : base(message) { }

    /// <summary>Mints with a message and inner exception.</summary>
    public EncodingException(string message, Exception innerException)
        : base(message, innerException) { }
}