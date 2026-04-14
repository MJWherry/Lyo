using System.Text.Json.Serialization;
using Lyo.Api.Client;
using Lyo.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Endato.Client;

public class EndatoClient : ApiClient
{
    private readonly ILoggerFactory _loggerFactory;

    private readonly EndatoClientOptions _options;

    public readonly EnrichmentManager Enrichment;

    public readonly PersonManager Persons;

    public EndatoClient(EndatoClientOptions options, ILoggerFactory? loggerFactory = null, HttpClient? httpClient = null)
        : base(
            loggerFactory?.CreateLogger<EndatoClient>() ?? NullLoggerFactory.Instance.CreateLogger<EndatoClient>(),
            httpClient ?? new HttpClient { BaseAddress = new($"{options.Url}") }, new() { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } },
            new() { BaseUrl = options.Url, EnsureStatusCode = options.EnsureStatusCode })
    {
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        _options = options;
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        HttpClient.BaseAddress = new($"{options.Url}");
        HttpClient.DefaultRequestHeaders.Add("galaxy-ap-password", options.ApPassword);
        HttpClient.DefaultRequestHeaders.Add("galaxy-ap-name", options.ApName);
        Persons = new(this);
        Enrichment = new(this);
    }
}