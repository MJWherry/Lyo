using Lyo.Exceptions;
using Lyo.Http.Client;

namespace Lyo.ClamAv.Client;

/// <summary>Settings for <see cref="ClamAvClient" />. Bind from section <c>ClamAv</c>. <see cref="LyoHttpClientOptions.BaseUrl" /> is the clamav-rest root.</summary>
public sealed class ClamAvClientOptions : LyoHttpClientOptions
{
    /// <summary>Configuration section name.</summary>
    public new const string SectionName = "ClamAv";

    /// <summary>Relative scan path. Default <c>scan</c>.</summary>
    public string ScanPath { get; set; } = "scan";

    /// <summary>Relative version path. Default <c>version</c>.</summary>
    public string VersionPath { get; set; } = "version";

    /// <summary>Requires <see cref="LyoHttpClientOptions.BaseUrl" /> plus both paths.</summary>
    public override void Validate()
    {
        base.Validate();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(BaseUrl);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(ScanPath);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(VersionPath);
    }
}
