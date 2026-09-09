using System.Net.Http.Json;
using Lyo.Common.Metadata.Records;
using Lyo.Typecast.Client.Enums;
using Lyo.Typecast.Client.Models.TextToSpeech.Request;
using Lyo.Typecast.Client.Models.Voices.Response;
using Lyo.Web.Components;
using Lyo.Web.Primitives;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.Tts.Typecast.Web.Components;

public partial class TtsWorkbench
{
    private bool _busy;
    private string _audioFormat = "mp3";
    private byte[]? _audioBytes;
    private string? _audioSource;
    private int _pitch;
    private string _previousText = string.Empty;
    private string _nextText = string.Empty;
    private string _selectedModel = TypecastModel.SsfmV30;
    private string? _selectedVoiceId;
    private int? _seed;
    private string _text = "Hello from the test gateway.";
    private double _tempo = 1.0;
    private int _volume = 100;
    private bool _voicesLoaded;

    private IEnumerable<Voice> AvailableVoices => TtsService.GetVoicesForModel(_selectedModel).OrderBy(i => i.VoiceName);

    private Voice? SelectedVoice => AvailableVoices.FirstOrDefault(i => i.VoiceId == _selectedVoiceId);

    private IReadOnlyList<string> SelectedVoiceTags => SelectedVoice?.UseCases.Where(i => !string.IsNullOrWhiteSpace(i)).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? [];

    protected override async Task OnInitializedAsync()
    {
        _selectedModel = TtsOptions.DefaultModel;
        await ReloadVoicesAsync();
    }

    private async Task ReloadVoicesAsync()
    {
        _busy = true;
        try {
            _voicesLoaded = await TtsService.LoadVoicesAsync();
            if (!_voicesLoaded) {
                SetStatus("Typecast returned no voices.", Severity.Warning);
                return;
            }

            _selectedVoiceId = AvailableVoices.FirstOrDefault()?.VoiceId;
            SetStatus($"Loaded {AvailableVoices.Count()} voices for {_selectedModel}.", Severity.Success);
        }
        catch (Exception ex) {
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task SynthesizeAsync()
    {
        if (string.IsNullOrWhiteSpace(_text)) {
            SetStatus("Enter text to synthesize.", Severity.Warning);
            return;
        }

        var selectedVoice = AvailableVoices.FirstOrDefault(i => i.VoiceId == _selectedVoiceId) ?? AvailableVoices.FirstOrDefault();
        if (selectedVoice == null) {
            SetStatus("No voice is available for the selected model.", Severity.Warning);
            return;
        }

        _selectedVoiceId = selectedVoice.VoiceId;
        _busy = true;
        try {
            var builder = TypecastTtsRequestBuilder.Create(_selectedVoiceId!, _text)
            .WithModel(_selectedModel)
            .WithOutput(
                new OutputSettings {
                    AudioFormat = _audioFormat,
                    Volume = _volume,
                    AudioPitch = _pitch,
                    AudioTempo = _tempo
                });

            if (_seed.HasValue)
                builder.WithSeed(_seed.Value);

            if (!string.IsNullOrWhiteSpace(_previousText) || !string.IsNullOrWhiteSpace(_nextText))
                builder.WithSmartPrompt(_previousText, _nextText);

            var request = builder.Build();
            var result = await TtsService.SynthesizeAsync(request);
            if (!result.IsSuccess || result.AudioData == null) {
                SetStatus(LyoResultErrorFormatter.FormatErrors(result.Errors), Severity.Error);
                return;
            }

            _audioBytes = result.AudioData;
            _audioSource = $"data:{GetMimeType()};base64,{Convert.ToBase64String(result.AudioData)}";
            SetStatus(result.Message ?? "Speech synthesized.", Severity.Success);
        }
        catch (Exception ex) {
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task DownloadAsync()
    {
        if (_audioBytes == null)
            return;

        await Js.DownloadFile(_audioBytes, $"typecast-{DateTime.UtcNow:yyyyMMddHHmmss}.{_audioFormat}", GetMimeType());
    }

    private async Task TestConnectionAsync()
    {
        _busy = true;
        try {
            var ok = await TtsService.TestConnectionAsync();
            SetStatus(ok ? "Typecast connection succeeded." : "Typecast connection failed.", ok ? Severity.Success : Severity.Warning);
        }
        catch (Exception ex) {
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private string GetMimeType() => (_audioFormat == "wav" ? FileTypeInfo.Wav : FileTypeInfo.Mp3).MimeType;
}
