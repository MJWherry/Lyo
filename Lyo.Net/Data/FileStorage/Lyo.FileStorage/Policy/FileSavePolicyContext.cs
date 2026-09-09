namespace Lyo.FileStorage.Policy;

public sealed class FileSavePolicyContext
{
    public required long ByteLength { get; init; }

    public string? ContentType { get; init; }

    public string? OriginalFileName { get; init; }

    public string? TenantId { get; init; }
}