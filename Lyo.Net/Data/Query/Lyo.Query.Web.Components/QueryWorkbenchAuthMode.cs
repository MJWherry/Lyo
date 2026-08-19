namespace Lyo.Query.Web.Components;

/// <summary>How the query workbench runner attaches credentials to the POST.</summary>
public enum QueryWorkbenchAuthMode
{
    /// <summary>No extra header.</summary>
    None = 0,

    /// <summary><c>Authorization: Bearer {token}</c>. Token is <see cref="QueryWorkbenchRunConfiguration.AuthHeaderValue"/>.</summary>
    Bearer = 1,

    /// <summary>Arbitrary header: <see cref="QueryWorkbenchRunConfiguration.AuthHeaderName"/> = <see cref="QueryWorkbenchRunConfiguration.AuthHeaderValue"/>.</summary>
    Header = 2
}
