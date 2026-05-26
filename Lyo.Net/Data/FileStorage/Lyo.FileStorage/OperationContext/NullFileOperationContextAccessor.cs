namespace Lyo.FileStorage.OperationContext;

/// <summary>
/// No-op accessor used when no ambient operation context is configured. <see cref="Current" /> always returns <see langword="null" /> regardless of any set attempt,
/// preventing accidental cross-request leakage of tenant/actor identifiers via the shared singleton.
/// </summary>
public sealed class NullFileOperationContextAccessor : IFileOperationContextAccessor
{
    public static readonly NullFileOperationContextAccessor Instance = new();

    public IFileOperationContext? Current {
        get => null;
        set {
            // Intentionally discard — null accessor must not retain mutable per-call state on a shared singleton.
        }
    }
}