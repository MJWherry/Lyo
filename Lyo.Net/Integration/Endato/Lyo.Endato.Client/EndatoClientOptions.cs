using System.Diagnostics;
using Lyo.Http.Client;

namespace Lyo.Endato.Client;

/// <summary>Settings that control <see cref="EndatoClient" />. Inherits <see cref="LyoHttpClientOptions" />; use <see cref="LyoHttpClientOptions.BaseUrl" /> for the Endato API root.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class EndatoClientOptions : LyoHttpClientOptions
{
    public new const string SectionName = "EndatoClient";

    public string ApName { get; set; } = null!;

    public string ApPassword { get; set; } = null!;

    /// <summary>Account name and whether a password is set, plus the shared HTTP options. The password is omitted.</summary>
    public override string ToString() => $"ApName={ApName} ApPasswordSet={!string.IsNullOrWhiteSpace(ApPassword)} {base.ToString()}";
}