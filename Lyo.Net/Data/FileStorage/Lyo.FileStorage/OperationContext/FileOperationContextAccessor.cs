namespace Lyo.FileStorage.OperationContext;

/// <summary>Async-local context that ties file storage work to tenants and actors. Register as a singleton.</summary>
public sealed class FileOperationContextAccessor : IFileOperationContextAccessor
{
    private static readonly AsyncLocal<IFileOperationContext?> Context = new();

    public IFileOperationContext? Current {
        get => Context.Value;
        set => Context.Value = value;
    }
}