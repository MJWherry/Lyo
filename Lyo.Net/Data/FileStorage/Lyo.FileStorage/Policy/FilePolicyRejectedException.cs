namespace Lyo.FileStorage.Policy;

public sealed class FilePolicyRejectedException : Exception
{
    public FilePolicyRejectedException(string message)
        : base(message) { }

    public FilePolicyRejectedException(string message, Exception inner)
        : base(message, inner) { }
}