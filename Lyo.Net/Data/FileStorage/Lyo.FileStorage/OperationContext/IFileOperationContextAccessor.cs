namespace Lyo.FileStorage.OperationContext;

public interface IFileOperationContextAccessor
{
    IFileOperationContext? Current { get; set; }
}