using Lyo.Http.Client;

namespace Lyo.Api.Client;

/// <summary>Type forward so existing <c>using Lyo.Api.Client</c> handler tests and hosts keep compiling. Prefer <see cref="Lyo.Http.Client.LyoHttpClientHandler" />.</summary>
[Obsolete("Use Lyo.Http.Client.LyoHttpClientHandler.")]
public class LyoHttpClientHandler : Lyo.Http.Client.LyoHttpClientHandler
{
    /// <inheritdoc />
    public LyoHttpClientHandler(LyoHttpClientOptions options)
        : base(options) { }
}
