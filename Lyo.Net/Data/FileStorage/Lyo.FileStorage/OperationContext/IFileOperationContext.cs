namespace Lyo.FileStorage.OperationContext;

public interface IFileOperationContext
{
    string? TenantId { get; }

    string? ActorId { get; }

    Guid? CorrelationId { get; }
}