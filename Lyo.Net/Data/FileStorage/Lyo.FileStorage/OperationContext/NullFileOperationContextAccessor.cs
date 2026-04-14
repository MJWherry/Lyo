namespace Lyo.FileStorage.OperationContext;

public sealed class NullFileOperationContextAccessor : IFileOperationContextAccessor
{
    public static readonly NullFileOperationContextAccessor Instance = new();

    public IFileOperationContext? Current { get; set; }
}