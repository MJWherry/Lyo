using System.Text.Json;
using System.Text.Json.Serialization;
using Lyo.Http.Client;
using Lyo.Common.Core.Extensions;
using Lyo.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Typecast.Client;

/// <summary>Text-to-speech and voice management typecast API client.</summary>
public class TypecastClient : LyoHttpClient
{
    /// <summary>Manager for TTS calls.</summary>
    public readonly TextToSpeechManager TextToSpeech;

    /// <summary>Manager for voice calls.</summary>
    public readonly VoiceManager Voices;

    private readonly TypecastClientOptions _options;

    /// <summary>Constructs the TypecastClient class.</summary>
    /// <param name="options">Typecast client options; required.</param>
    /// <param name="loggerFactory">Optional factory used to create loggers.</param>
    /// <param name="httpClient">Optional HttpClient; a new one is created when omitted.</param>
    public TypecastClient(TypecastClientOptions options, ILoggerFactory? loggerFactory = null, HttpClient? httpClient = null)
        : base(
            loggerFactory?.CreateLogger<TypecastClient>() ?? NullLoggerFactory.Instance.CreateLogger<TypecastClient>(), httpClient,
            new() {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
            }, options)
    {
        ArgumentHelpers.ThrowIfNull(options);
        _options = options;
        HttpClient.BaseAddress = CreateBaseAddress(options);
        HttpClient.DefaultRequestHeaders.Add("X-API-KEY", options.ApiKey);
        TextToSpeech = new(this);
        Voices = new(this);
    }

    private static Uri CreateBaseAddress(TypecastClientOptions options)
    {
        var b = options.BaseUrl.OrDefault("https://api.typecast.ai").Trim();
        return new($"{b.TrimEnd('/')}/");
    }
}