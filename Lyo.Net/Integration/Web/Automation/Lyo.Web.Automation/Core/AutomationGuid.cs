namespace Lyo.Web.Automation;

/// <summary>Time-ordered identifiers for automation runs and steps when the runtime supports RFC 9562 version 7 GUIDs.</summary>
public static class AutomationGuid
{
    /// <summary>Creates a new time-ordered GUID (version 7 on .NET 9+); otherwise falls back to <see cref="Guid.NewGuid" />.</summary>
    public static Guid CreateTimeOrdered()
    {
#if NET9_0_OR_GREATER
        return Guid.CreateVersion7();
#else
        return Guid.NewGuid();
#endif
    }
}
