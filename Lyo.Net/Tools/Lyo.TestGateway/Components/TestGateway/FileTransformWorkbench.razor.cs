using System.Net.Http.Json;
using System.Text;
using Lyo.Api.Client;
using Lyo.Common.Metadata.Records;
using Lyo.Compression.Models;
using Lyo.Encryption;
using Lyo.Encryption.AesSiv;
using Lyo.Images;
using Lyo.Images.Models;
using Lyo.TestGateway.Components;
using Lyo.TestGateway.Services;
using Lyo.Web.Components;
using Lyo.Web.Primitives;
using Lyo.Web.Components.DataGrid;
using Lyo.Web.Components.FileUpload;
using Lyo.Web.Components.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using MudBlazor;
using static Microsoft.AspNetCore.Components.Web.RenderMode;

namespace Lyo.TestGateway.Components.TestGateway;

public partial class FileTransformWorkbench
{
    private bool _busy;
    private CompressionAlgorithm? _compressionAlgorithm;
    private EncryptionAlgorithm? _dataEncryptionAlgorithm;
    private EncryptionAlgorithm? _keyEncryptionAlgorithm;
    private AesGcmKeySizeBits _dataAesGcmKeySize = AesGcmKeySizeBits.Bits256;
    private AesGcmKeySizeBits _keyAesGcmKeySize = AesGcmKeySizeBits.Bits256;
    private AesSivKeySizeBits _dataAesSivKeySize = AesSivKeySizeBits.Bits256;
    private AesSivKeySizeBits _keyAesSivKeySize = AesSivKeySizeBits.Bits256;

    private static readonly EncryptionAlgorithm[] SymmetricWorkbenchAlgorithms = [EncryptionAlgorithm.AesGcm, EncryptionAlgorithm.ChaCha20Poly1305, EncryptionAlgorithm.AesCcm, EncryptionAlgorithm.AesSiv, EncryptionAlgorithm.XChaCha20Poly1305];

    private static string FormatEncryptionAlgorithmLabel(EncryptionAlgorithm a)
        => a switch {
            EncryptionAlgorithm.AesGcm => "AES-GCM",
            EncryptionAlgorithm.ChaCha20Poly1305 => "ChaCha20-Poly1305",
            EncryptionAlgorithm.AesCcm => "AES-CCM",
            EncryptionAlgorithm.AesSiv => "AES-SIV",
            EncryptionAlgorithm.XChaCha20Poly1305 => "XChaCha20-Poly1305",
            var _ => a.ToString()
        };

    private static bool UsesAesGcmOrCcmKeySize(EncryptionAlgorithm? algorithm) => algorithm is EncryptionAlgorithm.AesGcm or EncryptionAlgorithm.AesCcm;

    /// <summary>Whether the data-encryption row shows a key-size list next to the algorithm (two-key mode).</summary>
    private bool ShowsTwoKeyDataKeySize => UsesAesGcmOrCcmKeySize(_dataEncryptionAlgorithm) || _dataEncryptionAlgorithm == EncryptionAlgorithm.AesSiv;

    /// <summary>Whether the key-wrapping row shows a key-size list next to the algorithm (two-key mode).</summary>
    private bool ShowsTwoKeyKekKeySize => UsesAesGcmOrCcmKeySize(_keyEncryptionAlgorithm) || _keyEncryptionAlgorithm == EncryptionAlgorithm.AesSiv;

    /// <summary>Whether single-key mode shows a key-size list next to the algorithm.</summary>
    private bool ShowsNormalKeySize => UsesAesGcmOrCcmKeySize(_dataEncryptionAlgorithm) || _dataEncryptionAlgorithm == EncryptionAlgorithm.AesSiv;

    private bool _reverse;
    private bool _useRawText;
    private string _rawText = string.Empty;
    private TestGatewayTransformResult? _result;
    private string _secret = "change-me";
    private bool _showSecret;
    private TestGatewayUploadedFile? _uploadedFile;
    private bool _useTwoKeyEncryption;
    private string _uploadStatus = "No file selected.";

    private bool ActionButtonsDisabled => _busy || (!_useRawText && _uploadedFile == null) || (_useRawText && string.IsNullOrWhiteSpace(_rawText));

    private const int MaxClipboardCopyBytes = 512 * 1024;

    private bool CanCopyResultToClipboard => _result != null && _result.OutputBytes.Length > 0 && _result.OutputBytes.Length <= MaxClipboardCopyBytes;

    private string CopyResultTooltip => _result == null || _result.OutputBytes.Length == 0 ? string.Empty : _result.OutputBytes.Length > MaxClipboardCopyBytes ? $"Output exceeds {MaxClipboardCopyBytes / 1024} KB — use download." : "Copy as UTF-8 when lossless, otherwise Base64.";

    private void OnTwoKeyEncryptionToggleClicked(bool twoKey)
    {
        if (_useTwoKeyEncryption == twoKey)
            return;

        _useTwoKeyEncryption = twoKey;
        _result = null;
    }

    private void OnDirectionToggleClicked(bool reverse)
    {
        if (_reverse == reverse)
            return;

        _reverse = reverse;
        if (_useRawText) {
            _rawText = string.Empty;
            _result = null;
        }
    }

    private void OnInputModeToggleClicked(bool rawText)
    {
        if (_useRawText == rawText)
            return;

        _useRawText = rawText;
        _rawText = string.Empty;
        _result = null;
    }

    private void ClearRawText()
    {
        _rawText = string.Empty;
        _result = null;
    }

    private async Task LoadRawTextFromClipboardAsync()
    {
        try {
            var text = await Js.ReadClipboardTextAsync();
            _rawText = text ?? string.Empty;
            if (string.IsNullOrWhiteSpace(_rawText)) {
                SetStatus("Clipboard is empty.", Severity.Warning);
                return;
            }

            SetStatus($"Loaded {_rawText.Length} characters from clipboard into the text area.", Severity.Success);
        }
        catch (Exception ex) {
            SetStatus($"Could not read clipboard: {ex.Message}", Severity.Warning);
        }
    }

    private async Task CopyResultToClipboardAsync()
    {
        if (_result == null || _result.OutputBytes.Length == 0 || !CanCopyResultToClipboard)
            return;

        var payload = FormatClipboardPayload(_result.OutputBytes);
        await Js.SendToClipboard(payload);
        SetStatus("Copied to clipboard.", Severity.Success);
    }

    private static string FormatClipboardPayload(byte[] bytes)
    {
        if (bytes.Length == 0)
            return string.Empty;

        return IsLosslessUtf8(bytes) ? Encoding.UTF8.GetString(bytes) : Convert.ToBase64String(bytes);
    }

    private static bool IsLosslessUtf8(byte[] data) => Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(data)).AsSpan().SequenceEqual(data);

    private bool TryBuildInput(out TestGatewayUploadedFile? input)
    {
        input = null;
        if (!_useRawText) {
            if (_uploadedFile == null) {
                SetStatus("Upload a file first.", Severity.Warning);
                return false;
            }

            input = _uploadedFile;
            return true;
        }

        if (string.IsNullOrWhiteSpace(_rawText)) {
            SetStatus(_reverse ? "Enter Base64 in the text area or use Input from clipboard." : "Enter text in the raw text area.", Severity.Warning);
            return false;
        }

        if (!_reverse) {
            input = new("input.txt", FileTypeInfo.Txt.MimeType, Encoding.UTF8.GetBytes(_rawText));
            return true;
        }

        try {
            var normalized = _rawText.Trim().Replace("\r", "").Replace("\n", "").Replace(" ", "");
            input = new("input.bin", FileTypeInfo.Unknown.MimeType, Convert.FromBase64String(normalized));
            return true;
        }
        catch (FormatException) {
            SetStatus("Reverse mode expects Base64-encoded ciphertext or compressed bytes in the text area.", Severity.Warning);
            return false;
        }
    }

    private Task OnClientFileReadyAsync(LocalBrowserFile file)
    {
        _uploadedFile = new(file.FileName, FileTypeInfo.Unknown.MimeType, file.Content);
        _result = null;
        SetStatus($"Loaded {_uploadedFile.FileName}.", Severity.Success);
        return Task.CompletedTask;
    }

    private Task OnClientFileRemovedAsync(LocalBrowserFile file)
    {
        if (_uploadedFile?.FileName == file.FileName)
            _uploadedFile = null;

        _result = null;
        SetStatus($"{file.FileName} removed.", Severity.Info);
        return Task.CompletedTask;
    }

    private Task OnUploadStartedAsync(LyoFileUploadEventArgs args)
    {
        _uploadStatus = $"Uploading {args.FileName}...";
        return Task.CompletedTask;
    }

    private Task OnUploadProgressAsync(LyoFileUploadEventArgs args)
    {
        _uploadStatus = $"{args.FileName}: {args.Progress:F0}%";
        return Task.CompletedTask;
    }

    private Task OnUploadCompletedAsync(LyoFileUploadEventArgs args)
    {
        _uploadStatus = $"{args.FileName} ready.";
        return Task.CompletedTask;
    }

    private Task OnUploadCancelledAsync(LyoFileUploadEventArgs args)
    {
        _uploadStatus = $"{args.FileName} cancelled.";
        return Task.CompletedTask;
    }

    private Task OnUploadFailedAsync(LyoFileUploadEventArgs args)
    {
        _uploadStatus = $"{args.FileName} failed: {args.ErrorMessage}";
        return Task.CompletedTask;
    }

    private async Task ProbeFileAsync()
    {
        if (!TryBuildInput(out var input) || input == null)
            return;

        _busy = true;
        try {
            _result = await Task.Run(() => Transformer.ProbeFile(input));
            SetStatus("Probe completed.", Severity.Success);
        }
        catch (Exception ex) {
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task TransformAsync()
    {
        if (!TryBuildInput(out var input) || input == null)
            return;

        if (_useTwoKeyEncryption && HasEncryptionSelection && !(_dataEncryptionAlgorithm.HasValue && _keyEncryptionAlgorithm.HasValue)) {
            SetStatus("Select both data and key algorithms for two-key mode, or leave both as None.", Severity.Warning);
            return;
        }

        var actionLabel = GetActionButtonText();
        _busy = true;
        try {
            _result = await Transformer.TransformAsync(input, new(_reverse, ApplyCompression, ApplyEncryption, _useTwoKeyEncryption, _secret, _dataEncryptionAlgorithm ?? EncryptionAlgorithm.AesGcm, _keyEncryptionAlgorithm ?? EncryptionAlgorithm.AesGcm, _compressionAlgorithm ?? CompressionAlgorithm.Brotli, _dataAesGcmKeySize, _keyAesGcmKeySize, _dataAesSivKeySize, _keyAesSivKeySize));
            SetStatus($"{actionLabel} completed.", Severity.Success);
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
        if (_result == null)
            return;

        await Js.DownloadFile(_result.OutputBytes, _result.OutputFileName, _result.OutputContentType);
    }

    private string GetActionButtonText()
    {
        if (_busy)
            return "Working...";

        if (_reverse)
            return "Reverse";

        if (ApplyCompression && ApplyEncryption)
            return _useTwoKeyEncryption ? "Compress + Two-key Encrypt" : "Compress + Encrypt";

        if (ApplyCompression)
            return "Compress";

        if (ApplyEncryption)
            return _useTwoKeyEncryption ? "Two-key Encrypt" : "Encrypt";

        return "Process";
    }

    private Task ToggleSecretVisibility(MouseEventArgs _)
    {
        _showSecret = !_showSecret;
        return Task.CompletedTask;
    }

    private bool ApplyCompression => _compressionAlgorithm is not null;

    private bool ApplyEncryption => _useTwoKeyEncryption ? _dataEncryptionAlgorithm.HasValue && _keyEncryptionAlgorithm.HasValue : _dataEncryptionAlgorithm.HasValue;

    private bool HasEncryptionSelection => _useTwoKeyEncryption ? _dataEncryptionAlgorithm.HasValue || _keyEncryptionAlgorithm.HasValue : _dataEncryptionAlgorithm.HasValue;
}
