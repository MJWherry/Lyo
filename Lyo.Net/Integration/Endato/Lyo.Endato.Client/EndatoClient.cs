using Lyo.Api.Client;
using Lyo.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Endato.Client;

public class EndatoClient : ApiClient
{
    public readonly EnrichmentManager Enrichment;

    public readonly PersonManager Persons;
    private readonly EndatoClientOptions _options;

    public EndatoClient(EndatoClientOptions options, ILoggerFactory? loggerFactory = null, HttpClient? httpClient = null)
        : base(loggerFactory?.CreateLogger<EndatoClient>() ?? NullLoggerFactory.Instance.CreateLogger<EndatoClient>(), httpClient, EndatoJsonSerializerOptions.Create(), options)
    {
        ArgumentHelpers.ThrowIfNull(options);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.BaseUrl, nameof(options.BaseUrl));
        _options = options;
        HttpClient.DefaultRequestHeaders.Add("galaxy-ap-password", options.ApPassword);
        HttpClient.DefaultRequestHeaders.Add("galaxy-ap-name", options.ApName);
        Persons = new(this);
        Enrichment = new(this);
    }
}