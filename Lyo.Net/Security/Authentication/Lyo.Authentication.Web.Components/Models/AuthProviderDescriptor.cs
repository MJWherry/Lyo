namespace Lyo.Authentication.Web.Components.Models;

/// <summary>Display metadata for a single OIDC provider button on the login page.</summary>
/// <param name="Name">Canonical name as registered on the API (e.g. <c>google</c>, <c>keycloak:my-realm</c>). Sent to <c>IAuthSignInLauncher.SignInAsync</c>.</param>
/// <param name="DisplayName">Human-friendly button label (e.g. <c>Sign in with Google</c>).</param>
/// <param name="IconKey">Optional MudBlazor icon constant (e.g. <c>Icons.Material.Filled.AccountCircle</c>) rendered before the label. Defaults to a generic account icon when unset.</param>
public sealed record AuthProviderDescriptor(string Name, string DisplayName, string? IconKey = null);