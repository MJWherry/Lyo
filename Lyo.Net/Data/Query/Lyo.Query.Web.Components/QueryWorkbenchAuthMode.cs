namespace Lyo.Query.Web.Components;

/// <summary>How the query workbench runner attaches credentials to the POST.</summary>
public enum QueryWorkbenchAuthMode
{
    /// <summary>No extra header is attached.</summary>
    None = 0,

    /// <summary><c>Authorization: Bearer {token}</c>. Token comes from <see cref="QueryWorkbenchRunConfiguration.AuthHeaderValue"/>.</summary>
    Bearer = 1,

    /// <summary>Custom header: <see cref="QueryWorkbenchRunConfiguration.AuthHeaderName"/> = <see cref="QueryWorkbenchRunConfiguration.AuthHeaderValue"/>.</summary>
    Header = 2
}
