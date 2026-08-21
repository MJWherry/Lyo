using System.Diagnostics;

namespace Lyo.FileStorage.OperationContext;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record FileOperationContextRecord(string? TenantId, string? ActorId, Guid? CorrelationId = null) : IFileOperationContext
{
    /// <inheritdoc />
    public override string ToString()
        => $"FileOperationContextRecord: TenantId={TenantId ?? "(none)"}, ActorId={ActorId ?? "(none)"}, CorrelationId={CorrelationId?.ToString() ?? "(none)"}";
}
