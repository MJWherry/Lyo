namespace Lyo.FileStorage.Policy;

public sealed class AllowAllFileContentPolicy : IFileContentPolicy
{
    public static readonly AllowAllFileContentPolicy Instance = new();

    public Task ValidateAsync(FileSavePolicyContext context, CancellationToken ct = default) => Task.CompletedTask;
}