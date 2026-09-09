using System.Diagnostics;

namespace Lyo.Authentication.Models.Records;

/// <summary>Result of <see cref="Services.Opaque.IApiTokenIssuer.IssueAsync" />: wire-form plaintext (shown once) plus the row persisted to the store.</summary>
/// <param name="Plaintext">Full wire-form token (e.g. <c>lyo_pat_live_01hxy8k2qf9_4f3b...</c>). Shown to the caller exactly once, then discarded.</param>
/// <param name="Record">Persisted record (hashed secret only).</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record IssuedApiToken(string Plaintext, ApiTokenRecord Record)
{
    public override string ToString() => $"IssuedApiToken: id={Record.Id}, kind={Record.Kind}, ring={Record.Ring}";
}