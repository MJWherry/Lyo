using System.Collections.Generic;
using Lyo.Authentication.Web.Components.Models;

namespace Lyo.Authentication.Web.Components.Options;

/// <summary>Configuration bound from <c>LyoAuthWebComponents</c>. Drives the login page provider list and toggles the optional username/password card.</summary>
public sealed class LyoAuthWebComponentsOptions
{
    /// <summary>Configuration section name (<c>LyoAuthWebComponents</c>).</summary>
    public const string SectionName = "LyoAuthWebComponents";

    /// <summary>Providers rendered on the login page. Each entry becomes a button that calls <c>IAuthSignInLauncher.SignInAsync(<see cref="AuthProviderDescriptor.Name"/>, ...)</c>.</summary>
    public List<AuthProviderDescriptor> Providers { get; set; } = new();

    /// <summary>
    /// When <c>true</c> (default) and an <c>IAuthPasswordSignIn</c> implementation is registered, the login page renders the username/password card. When <c>false</c>, the card
    /// is omitted even if a password handler is registered.
    /// </summary>
    public bool EnablePasswordSignIn { get; set; } = true;

    /// <summary>
    /// When <c>true</c> the password card shows a "Remember me" checkbox whose value is forwarded to <c>IAuthPasswordSignIn.SignInAsync</c>. Default <c>true</c>.
    /// </summary>
    public bool ShowRememberMe { get; set; } = true;
}
