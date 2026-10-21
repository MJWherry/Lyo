using Lyo.Authentication.Models.Records;

namespace Lyo.Authentication.Sso.Protocol;

/// <summary>Result of a completed external login. <see cref="Issued" /> was minted once.</summary>
/// <param name="Issued">Lyo access token and optional refresh token.</param>
/// <param name="ReturnUrl">Return target captured at login start.</param>
/// <param name="Mode"><c>browser</c> or <c>api</c>.</param>
/// <param name="Provider">Provider that completed the login.</param>
public sealed record ExternalLoginResult(IssuedLyoJwt Issued, string ReturnUrl, string Mode, string Provider);