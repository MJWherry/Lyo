namespace Lyo.Http.Client;

/// <summary>Keyed <see cref="ILyoHttpClient" /> instances. Each key is built once and reused.</summary>
public interface ILyoHttpClientFactory
{
    /// <summary>Builds the client for <paramref name="key" />. A second registration of the same key throws.</summary>
    void Register(string key, Action<LyoHttpClientOptions> configure);

    /// <summary>Builds the client returned for a key that was never registered.</summary>
    void RegisterDefault(Action<LyoHttpClientOptions> configure);

    /// <summary>Returns the client for <paramref name="key" />. An unknown key logs a warning and returns the default client.</summary>
    ILyoHttpClient Get(string key);
}