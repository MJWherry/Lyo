namespace Lyo.Common.Core.Net;

/// <summary>Browser grammar <see cref="UserAgentGenerator" /> stamps into a User-Agent string.</summary>
public enum UserAgentFormat
{
    /// <summary><c>Chrome/{version}</c> with the AppleWebKit token.</summary>
    Chromium = 0,

    /// <summary>Chromium grammar plus <c>Edg/{version}</c>.</summary>
    Edge = 1,

    /// <summary><c>Firefox/{version}</c> with a Gecko <c>rv:</c> token.</summary>
    Firefox = 2,

    /// <summary><c>Version/{version}</c> with the Safari WebKit token.</summary>
    Safari = 3
}