using Microsoft.AspNetCore.Authentication;

namespace Lyo.Authentication.AspNetCore.Schemes.Bearer;

/// <summary>Options for the dispatcher policy scheme. Inherits the <see cref="PolicySchemeOptions.ForwardDefaultSelector"/> hook from the framework.</summary>
public sealed class LyoBearerPolicySchemeOptions : PolicySchemeOptions
{
    /// <summary>The header to inspect for the credential. Default = <c>Authorization</c>.</summary>
    public string HeaderName { get; set; } = "Authorization";

    /// <summary>The expected scheme inside <see cref="HeaderName"/>. Default = <c>Bearer</c>.</summary>
    public string Scheme { get; set; } = "Bearer";

    /// <summary>An alternate header that may carry the credential directly (no scheme prefix). Default = <c>X-Api-Key</c>. Set to <c>null</c> to disable sniffing of this header.</summary>
    public string? AlsoAccept { get; set; } = "X-Api-Key";
}
