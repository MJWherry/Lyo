using System.Diagnostics;
using Lyo.Common;
using Lyo.Common.Records;
using Lyo.Exceptions;

namespace Lyo.Typecast.Client.Models.TextToSpeech.Request;

/// <summary>Builder for creating Typecast text-to-speech requests.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class TypecastTtsRequestBuilder
{
    private readonly TypecastTtsRequest _request = new();

    /// <summary>Sets the voice ID for the request.</summary>
    /// <param name="voiceId">Voice ID in format 'tc_' followed by a unique identifier (e.g., 'tc_60e5426de8b95f1d3000d7b5').</param>
    /// <returns>The builder instance for chaining.</returns>
    public TypecastTtsRequestBuilder WithVoiceId(string voiceId)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(voiceId, nameof(voiceId));
        _request.VoiceId = voiceId;
        return this;
    }

    /// <summary>Sets the text to synthesize.</summary>
    /// <param name="text">Text to convert to speech. Minimum 1 character, maximum 2000 characters.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TypecastTtsRequestBuilder WithText(string text)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(text, nameof(text));
        _request.Text = text;
        return this;
    }

    /// <summary>Sets the voice model to use for synthesis by string.</summary>
    /// <param name="model">Model string (e.g., "ssfm-v30", "ssfm-v21").</param>
    /// <returns>The builder instance for chaining.</returns>
    public TypecastTtsRequestBuilder WithModel(string model)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(model, nameof(model));
        _request.Model = model;
        return this;
    }

    /// <summary>Sets the language code.</summary>
    /// <param name="language">Language code following ISO 639-3 standard (e.g., "eng", "kor", "jpn").</param>
    /// <returns>The builder instance for chaining.</returns>
    public TypecastTtsRequestBuilder WithLanguage(string language)
    {
        // Parse string to LanguageCodeInfo using extension methods
        var normalized = language.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return this;

        // Try ISO 639-3 first (Typecast uses ISO 639-3)
        var langInfo = normalized.FromISO639_3();
        if (langInfo == LanguageCodeInfo.Unknown) {
            // Fallback to ISO 639-1
            langInfo = normalized.FromISO639_1();
        }

        _request.Language = langInfo != LanguageCodeInfo.Unknown ? langInfo : null;
        return this;
    }

    /// <summary>Sets the prompt (emotion and style settings).</summary>
    /// <param name="prompt">Prompt configuration.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TypecastTtsRequestBuilder WithPrompt(Prompt prompt)
    {
        ArgumentHelpers.ThrowIfNull(prompt, nameof(prompt));
        _request.Prompt = prompt;
        return this;
    }

    /// <summary>Configures the prompt using an action.</summary>
    /// <param name="configure">Action to configure the prompt.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TypecastTtsRequestBuilder WithPrompt(Action<Prompt> configure)
    {
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        var prompt = new Prompt();
        configure(prompt);
        _request.Prompt = prompt;
        return this;
    }

    /// <summary>Enables Typecast smart prompting with optional before/after text context.</summary>
    /// <param name="previousText">Text before the target utterance.</param>
    /// <param name="nextText">Text after the target utterance.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TypecastTtsRequestBuilder WithSmartPrompt(string? previousText = null, string? nextText = null)
    {
        _request.Prompt = new() {
            EmotionType = "smart", PreviousText = string.IsNullOrWhiteSpace(previousText) ? null : previousText, NextText = string.IsNullOrWhiteSpace(nextText) ? null : nextText
        };

        return this;
    }

    /// <summary>Sets the output settings (volume, pitch, tempo, format).</summary>
    /// <param name="output">Output settings configuration.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TypecastTtsRequestBuilder WithOutput(OutputSettings output)
    {
        ArgumentHelpers.ThrowIfNull(output, nameof(output));
        _request.Output = output;
        return this;
    }

    /// <summary>Configures the output settings using an action.</summary>
    /// <param name="configure">Action to configure the output settings.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TypecastTtsRequestBuilder WithOutput(Action<OutputSettings> configure)
    {
        ArgumentHelpers.ThrowIfNull(configure, nameof(configure));
        var output = new OutputSettings();
        configure(output);
        _request.Output = output;
        return this;
    }

    /// <summary>Sets the random seed for controlling speech generation variations.</summary>
    /// <param name="seed">Random seed value.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TypecastTtsRequestBuilder WithSeed(int seed)
    {
        _request.Seed = seed;
        return this;
    }

    /// <summary>Builds the text-to-speech request.</summary>
    /// <returns>The configured TextToSpeechRequest.</returns>
    public TypecastTtsRequest Build()
    {
        OperationHelpers.ThrowIfNullOrWhiteSpace(_request.VoiceId, "VoiceId is required.");
        OperationHelpers.ThrowIfNullOrWhiteSpace(_request.Text, "Text is required.");
        return _request;
    }

    /// <summary>Creates a new builder instance.</summary>
    /// <returns>A new TextToSpeechRequestBuilder.</returns>
    public static TypecastTtsRequestBuilder New() => new();

    /// <summary>Creates a new builder instance with voice ID and text.</summary>
    /// <param name="voiceId">Voice ID.</param>
    /// <param name="text">Text to synthesize.</param>
    /// <returns>A new TextToSpeechRequestBuilder.</returns>
    public static TypecastTtsRequestBuilder Create(string voiceId, string text)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(voiceId, nameof(voiceId));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(text, nameof(text));
        return New().WithVoiceId(voiceId).WithText(text);
    }

    public override string ToString()
        => $"VoiceId: {_request.VoiceId}, Text: {(_request.Text.Length > 50 ? _request.Text.Substring(0, 50) + "..." : _request.Text)}, Model: {_request.Model}";
}