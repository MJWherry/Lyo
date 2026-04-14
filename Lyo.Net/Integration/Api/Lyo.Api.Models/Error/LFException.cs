namespace Lyo.Api.Models.Error;

public class LFException(string errorCode, string? message = null, Exception? innerException = null)
    : Exception(message ?? "An unknown error occurred", innerException)
{
    public string ErrorCode { get; } = errorCode;
}
