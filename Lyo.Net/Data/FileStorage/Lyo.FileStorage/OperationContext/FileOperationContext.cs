namespace Lyo.FileStorage.OperationContext;

public sealed record FileOperationContextRecord(string? TenantId, string? ActorId, Guid? CorrelationId = null) : IFileOperationContext;