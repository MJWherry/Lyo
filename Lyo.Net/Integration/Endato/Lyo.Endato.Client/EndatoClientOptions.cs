using Lyo.Api.Client;

namespace Lyo.Endato.Client;

/// <summary>Configuration options for <see cref="EndatoClient" />. Inherits <see cref="ApiClientOptions" />; use <see cref="ApiClientOptions.BaseUrl" /> for the Endato API root.</summary>
public class EndatoClientOptions : ApiClientOptions
{
    public new const string SectionName = "EndatoClient";

    public string ApName { get; set; } = null!;

    public string ApPassword { get; set; } = null!;
}
