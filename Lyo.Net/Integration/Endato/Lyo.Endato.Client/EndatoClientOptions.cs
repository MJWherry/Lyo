using Lyo.Http.Client;

namespace Lyo.Endato.Client;

/// <summary>
/// Settings that control <see cref="EndatoClient" />. Inherits <see cref="LyoHttpClientOptions" />; use <see cref="LyoHttpClientOptions.BaseUrl" /> for the Endato API root.
/// </summary>
public class EndatoClientOptions : LyoHttpClientOptions
{
    public new const string SectionName = "EndatoClient";

    public string ApName { get; set; } = null!;

    public string ApPassword { get; set; } = null!;
}
