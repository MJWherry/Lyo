namespace Lyo.FileStorage.OperationContext;

/// <summary>Async-local operation context for correlating file storage work with tenants and actors. Register as singleton.</summary>
public sealed class FileOperationContextAccessor : IFileOperationContextAccessor
{
    private static readonly AsyncLocal<IFileOperationContext?> Context = new();

    public IFileOperationContext? Current {
        get => Context.Value;
        set => Context.Value = value;
    }
}