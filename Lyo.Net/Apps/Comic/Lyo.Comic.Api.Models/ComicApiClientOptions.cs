using Lyo.Api.Client;

namespace Lyo.Comic.Api.Models;

/// <summary>Configuration for the Comic API HTTP client. Bind from the "ComicApi" configuration section.</summary>
public sealed class ComicApiClientOptions : ApiClientOptions
{
    public new const string SectionName = "ComicApi";
}
