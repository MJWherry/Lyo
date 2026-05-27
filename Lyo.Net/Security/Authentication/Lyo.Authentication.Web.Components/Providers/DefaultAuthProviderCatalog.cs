using System.Collections.Generic;
using Lyo.Authentication.Web.Components.Abstractions;
using Lyo.Authentication.Web.Components.Models;
using Lyo.Authentication.Web.Components.Options;
using Lyo.Exceptions;
using Microsoft.Extensions.Options;

namespace Lyo.Authentication.Web.Components.Providers;

/// <summary>
/// Configuration-bound <see cref="IAuthProviderCatalog"/>. Reads its data from <see cref="LyoAuthWebComponentsOptions.Providers"/>. Registered by default when callers invoke
/// <c>services.AddLyoAuthWebComponents(IConfiguration)</c>.
/// </summary>
public sealed class DefaultAuthProviderCatalog : IAuthProviderCatalog
{
    private readonly IReadOnlyList<AuthProviderDescriptor> _providers;

    /// <summary>Creates a catalog from the bound options.</summary>
    public DefaultAuthProviderCatalog(IOptions<LyoAuthWebComponentsOptions> options)
    {
        ArgumentHelpers.ThrowIfNull(options);
        _providers = options.Value.Providers.ToArray();
    }

    /// <inheritdoc/>
    public IReadOnlyList<AuthProviderDescriptor> List() => _providers;
}
