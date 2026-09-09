using System.Diagnostics;
using Lyo.Common.Metadata.Extensions;
using Lyo.Common.Metadata.Records;
using Lyo.Exceptions;

namespace Lyo.Typecast.Client.Models.TextToSpeech.Request;

/// <summary>Fluent builder that creates Typecast text-to-speech requests.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class TypecastTtsRequestBuilder
{
    private readonly TypecastTtsRequest _request = new();

    /// <summary>Assigns the voice ID for the request.</summary>
    /// <param name="voiceId">Voice id of the form 'tc_' plus a unique suffix (for example 'tc_60e5426de8b95f1d3000d7b5').</param>
    /// <returns>Same builder so calls can be chained.</returns>
    public TypecastTtsRequestBuilder WithVoiceId(string voiceId)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(voiceId);
        _request.VoiceId = voiceId;
        return this;
    }

    /// <summary>Assigns the text to synthesize.</summary>
    /// <param name="text">Text to synthesize; 1–2000 characters.</param>
    /// <returns>Same builder so calls can be chained.</returns>
    public TypecastTtsRequestBuilder WithText(string text)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(text);
        _request.Text = text;
        return this;
    }

    /// <summary>Assigns the voice model to use for synthesis by string.</summary>
    /// <param name="model">Model id such as "ssfm-v30" or "ssfm-v21".</param>
    /// <returns>Same builder so calls can be chained.</returns>
    public TypecastTtsRequestBuilder WithModel(string model)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(model);
        _request.Model = model;
        return this;
    }

    /// <summary>Assigns the language code.</summary>
    /// <param name="language">ISO 639-3 language code (for example "eng", "kor", "jpn").</param>
    /// <returns>Same builder so calls can be chained.</returns>
    public TypecastTtsRequestBuilder WithLanguage(string language)
    {
        // Turn the string into LanguageCodeInfo via extensions
        var normalized = language.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return this;

        // Prefer ISO 639-3; that is what Typecast sends
        var langInfo = normalized.FromISO639_3();
        if (langInfo == LanguageCodeInfo.Unknown) {
            // Use ISO 639-1 when the 639-3 lookup misses
            langInfo = normalized.FromISO639_1();
        }

        _request.Language = langInfo != LanguageCodeInfo.Unknown ? langInfo : null;
        return this;
    }

    /// <summary>Assigns the prompt (emotion and style settings).</summary>
    /// <param name="prompt">Prompt / emotion settings.</param>
    /// <returns>Same builder so calls can be chained.</returns>
    public TypecastTtsRequestBuilder WithPrompt(Prompt prompt)
    {
        ArgumentHelpers.ThrowIfNull(prompt);
        _request.Prompt = prompt;
        return this;
    }

    /// <summary>Fills prompt settings via a callback.</summary>
    /// <param name="configure">Callback that fills prompt settings.</param>
    /// <returns>Same builder so calls can be chained.</returns>
    public TypecastTtsRequestBuilder WithPrompt(Action<Prompt> configure)
    {
        ArgumentHelpers.ThrowIfNull(configure);
        var prompt = new Prompt();
        configure(prompt);
        _request.Prompt = prompt;
        return this;
    }

    /// <summary>Turns on Typecast smart prompting with optional before/after text context.</summary>
    /// <param name="previousText">Leading-context text.</param>
    /// <param name="nextText">Following-context text.</param>
    /// <returns>Same builder so calls can be chained.</returns>
    public TypecastTtsRequestBuilder WithSmartPrompt(string? previousText = null, string? nextText = null)
    {
        _request.Prompt = new() {
            EmotionType = "smart", PreviousText = string.IsNullOrWhiteSpace(previousText) ? null : previousText, NextText = string.IsNullOrWhiteSpace(nextText) ? null : nextText
        };

        return this;
    }

    /// <summary>Assigns the output settings (volume, pitch, tempo, format).</summary>
    /// <param name="output">Audio output settings.</param>
    /// <returns>Same builder so calls can be chained.</returns>
    public TypecastTtsRequestBuilder WithOutput(OutputSettings output)
    {
        ArgumentHelpers.ThrowIfNull(output);
        _request.Output = output;
        return this;
    }

    /// <summary>Fills output settings via a callback.</summary>
    /// <param name="configure">Callback that fills output settings.</param>
    /// <returns>Same builder so calls can be chained.</returns>
    public TypecastTtsRequestBuilder WithOutput(Action<OutputSettings> configure)
    {
        ArgumentHelpers.ThrowIfNull(configure);
        var output = new OutputSettings();
        configure(output);
        _request.Output = output;
        return this;
    }

    /// <summary>Assigns the random seed for controlling speech generation variations.</summary>
    /// <param name="seed">Seed used for random variation.</param>
    /// <returns>Same builder so calls can be chained.</returns>
    public TypecastTtsRequestBuilder WithSeed(int seed)
    {
        _request.Seed = seed;
        return this;
    }

    /// <summary>Assembles the TTS request.</summary>
    /// <returns>Completed TextToSpeechRequest.</returns>
    public TypecastTtsRequest Build()
    {
        OperationHelpers.ThrowIfNullOrWhiteSpace(_request.VoiceId, "VoiceId is required.");
        OperationHelpers.ThrowIfNullOrWhiteSpace(_request.Text, "Text is required.");
        return _request;
    }

    /// <summary>Constructs a new builder instance.</summary>
    /// <returns>Fresh TextToSpeechRequestBuilder.</returns>
    public static TypecastTtsRequestBuilder New() => new();

    /// <summary>Constructs a new builder instance with voice ID and text.</summary>
    /// <param name="voiceId">Id of the voice.</param>
    /// <param name="text">Utterance to turn into speech.</param>
    /// <returns>Fresh TextToSpeechRequestBuilder.</returns>
    public static TypecastTtsRequestBuilder Create(string voiceId, string text)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(voiceId);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(text);
        return New().WithVoiceId(voiceId).WithText(text);
    }

    public override string ToString()
        => $"VoiceId: {_request.VoiceId}, Text: {(_request.Text.Length > 50 ? _request.Text.Substring(0, 50) + "..." : _request.Text)}, Model: {_request.Model}";
}