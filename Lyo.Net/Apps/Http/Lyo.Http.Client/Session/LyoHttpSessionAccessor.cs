namespace Lyo.Http.Client.Session;

/// <summary>Flows the current <see cref="ILyoHttpSession" /> to pooled handlers (cookies/UA pin) without capturing a scoped service on the handler instance.</summary>
public static class LyoHttpSessionAccessor
{
    private static readonly AsyncLocal<ILyoHttpSession?> Current = new();

    /// <summary>Session for the in-flight send, or null when the client has not entered a call.</summary>
    public static ILyoHttpSession? Session {
        get => Current.Value;
        set => Current.Value = value;
    }
}
