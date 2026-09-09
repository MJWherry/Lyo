using System.Net.Http.Json;
using Lyo.Common.Core.Enums;
using Lyo.Common.Metadata.Records;
using Lyo.Web.Components;
using Lyo.Web.Primitives;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.Tts.AwsPolly.Web.Components;

public partial class AwsPollyWorkbench
{
    private const string AutoLanguage = "__default__";
    private readonly IReadOnlyList<LanguageCodeInfo> _languages = LanguageCodeInfo.All.Where(i => i != LanguageCodeInfo.Unknown).OrderBy(i => i.Description).ToArray();
    private readonly IReadOnlyList<AwsPollyVoiceId> _availableVoices = Enum.GetValues<AwsPollyVoiceId>().Where(i => i != AwsPollyVoiceId.Unknown).ToArray();
    private bool _busy;
    private AudioFormat _audioFormat = AudioFormat.Mp3;
    private byte[]? _audioBytes;
    private string? _audioSource;
    private AwsPollyVoiceId _selectedVoiceId = AwsPollyVoiceId.Amy;
    private string _selectedLanguage = AutoLanguage;
    private string _text = "Hello from Lyo.";

    protected override void OnInitialized()
    {
        _selectedVoiceId = TtsOptions.DefaultVoiceIdEnum;
        _audioFormat = TtsOptions.DefaultOutputFormat ?? AudioFormat.Mp3;
    }

    private async Task SynthesizeAsync()
    {
        if (string.IsNullOrWhiteSpace(_text)) {
            SetStatus("Enter text to synthesize.", Severity.Warning);
            return;
        }

        _busy = true;
        try {
            var request = new AwsPollyTtsRequest(_text, _selectedVoiceId, _selectedLanguage == AutoLanguage ? null : LanguageCodeInfo.FromBcp47(_selectedLanguage), _audioFormat);
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

        await Js.DownloadFile(_audioBytes, $"aws-polly-{DateTime.UtcNow:yyyyMMddHHmmss}.{GetExtension()}", GetMimeType());
    }

    private async Task TestConnectionAsync()
    {
        _busy = true;
        try {
            var ok = await TtsService.TestConnectionAsync();
            SetStatus(ok ? "AWS Polly connection succeeded." : "AWS Polly connection failed.", ok ? Severity.Success : Severity.Warning);
        }
        catch (Exception ex) {
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private string GetExtension()
        => _audioFormat switch {
            AudioFormat.Wav => "wav",
            AudioFormat.Ogg => "ogg",
            var _ => "mp3"
        };

    private string GetMimeType() => GetFileType().MimeType;

    private FileTypeInfo GetFileType()
        => _audioFormat switch {
            AudioFormat.Wav => FileTypeInfo.Wav,
            AudioFormat.Ogg => FileTypeInfo.Ogg,
            var _ => FileTypeInfo.Mp3
        };
}
