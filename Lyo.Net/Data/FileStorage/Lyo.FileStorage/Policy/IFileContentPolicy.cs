namespace Lyo.FileStorage.Policy;

public interface IFileContentPolicy
{
    Task ValidateAsync(FileSavePolicyContext context, CancellationToken ct = default);
}