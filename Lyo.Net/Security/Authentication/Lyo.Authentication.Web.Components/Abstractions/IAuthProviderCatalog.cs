using Lyo.Authentication.Web.Components.Models;

namespace Lyo.Authentication.Web.Components.Abstractions;

/// <summary>Source of the provider buttons rendered on the login page. Default config-bound impl reads <c>LyoAuthWebComponents:Providers</c>; hosts may swap in a richer impl.</summary>
public interface IAuthProviderCatalog
{
    /// <summary>The providers to render on the login page, in display order.</summary>
    IReadOnlyList<AuthProviderDescriptor> List();
}