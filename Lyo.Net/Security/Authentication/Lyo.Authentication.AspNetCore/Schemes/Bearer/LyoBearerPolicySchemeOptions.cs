using Lyo.Common.Core.Net;
using Microsoft.AspNetCore.Authentication;

namespace Lyo.Authentication.AspNetCore.Schemes.Bearer;

/// <summary>Options for the dispatcher policy scheme. Inherits the framework <see cref="PolicySchemeOptions.ForwardDefaultSelector" /> hook.</summary>
public sealed class LyoBearerPolicySchemeOptions : PolicySchemeOptions
{
    /// <summary>Header inspected for the credential. Default = <c>Authorization</c>.</summary>
    public string HeaderName { get; set; } = LyoHttpHeaders.Authorization;

    /// <summary>Expected scheme inside <see cref="HeaderName" />. Default = <c>Bearer</c>.</summary>
    public string Scheme { get; set; } = "Bearer";

    /// <summary>Alternate header that may carry the credential with no scheme prefix. Default = <c>X-Api-Key</c>. Set <c>null</c> to skip this header.</summary>
    public string? AlsoAccept { get; set; } = LyoHttpHeaders.ApiKey;
}