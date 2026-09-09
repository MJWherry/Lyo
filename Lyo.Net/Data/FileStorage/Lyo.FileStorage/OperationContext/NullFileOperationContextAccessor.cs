namespace Lyo.FileStorage.OperationContext;

/// <summary>
/// No-op accessor used when no ambient operation context is configured. <see cref="Current" /> always returns <see langword="null" />, even if a caller tries to set it,
/// so tenant and actor ids cannot leak across requests through the shared singleton.
/// </summary>
public sealed class NullFileOperationContextAccessor : IFileOperationContextAccessor
{
    public static readonly NullFileOperationContextAccessor Instance = new();

    public IFileOperationContext? Current {
        get => null;
        set {
            // Drop the value. This accessor must not keep per-call state on a shared singleton.
        }
    }
}