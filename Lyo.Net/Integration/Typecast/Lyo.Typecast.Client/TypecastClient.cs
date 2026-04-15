using System.Text.Json;
using System.Text.Json.Serialization;
using Lyo.Api.Client;
using Lyo.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Typecast.Client;

/// <summary>Typecast API client for text-to-speech and voice management.</summary>
public class TypecastClient : ApiClient
{
    private readonly TypecastClientOptions _options;

    /// <summary>Text-to-speech operations manager.</summary>
    public readonly TextToSpeechManager TextToSpeech;

    /// <summary>Voice operations manager.</summary>
    public readonly VoiceManager Voices;

    /// <summary>Initializes a new instance of the TypecastClient class.</summary>
    /// <param name="options">The Typecast client options. Must not be null.</param>
    /// <param name="loggerFactory">Optional logger factory for creating loggers.</param>
    /// <param name="httpClient">Optional HTTP client. If not provided, a new instance will be created.</param>
    public TypecastClient(TypecastClientOptions options, ILoggerFactory? loggerFactory = null, HttpClient? httpClient = null)
        : base(
            loggerFactory?.CreateLogger<TypecastClient>() ?? NullLoggerFactory.Instance.CreateLogger<TypecastClient>(),
            httpClient,
            new() {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
            },
            options)
    {
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        _options = options;
        HttpClient.BaseAddress = CreateBaseAddress(options);
        HttpClient.DefaultRequestHeaders.Add("X-API-KEY", options.ApiKey);
        TextToSpeech = new(this);
        Voices = new(this);
    }

    private static Uri CreateBaseAddress(TypecastClientOptions options)
    {
        var b = options.BaseUrl?.Trim();
        if (string.IsNullOrEmpty(b))
            b = "https://api.typecast.ai";

        return new($"{b.TrimEnd('/')}/");
    }
}
