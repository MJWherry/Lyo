using Lyo.Tts.Models;

namespace Lyo.Tts.AwsPolly;

/// <summary>Adapts <see cref="AwsPollyTtsService" /> to <see cref="ITtsService" /> for DI.</summary>
public sealed class AwsPollyTtsAppService(AwsPollyTtsService inner) : ITtsService
{
    /// <inheritdoc />
    public async Task<TtsSynthesisResult> SynthesizeAsync(string text, string? voiceId = null, CancellationToken cancellationToken = default)
    {
        var result = await inner.SynthesizeAsync(text, voiceId, cancellationToken).ConfigureAwait(false);
        if (result.IsSuccess && result.AudioData != null)
            return new(true, result.AudioData, null);

        var err = result.Errors is { Count: > 0 } ? result.Errors[0].Message : "TTS synthesis failed.";
        return new(false, null, err);
    }
}