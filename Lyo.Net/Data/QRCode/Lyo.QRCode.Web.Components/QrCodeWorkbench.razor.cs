using System.Collections.Generic;
using System.Globalization;
using Lyo.Common.Enums;
using Lyo.Common.Records;
using Lyo.Images;
using Lyo.Images.Models;
using Lyo.QRCode;
using Lyo.QRCode.Models;
using Lyo.QRCode.Payloads;
using Lyo.Web.Components;
using Lyo.Web.Components.FileUpload;
using Lyo.Web.Components.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Utilities;

namespace Lyo.QRCode.Web.Components;

/// <summary>
/// Blazor workbench for QR generation: three columns (output and styling, typed payloads, result). PNG decorative frames are composited after rasterization using <see cref="QrFrameLayoutOptions"/>; MudBlazor picker colors are converted to opaque <c>#RRGGBB</c> for ImageSharp.
/// </summary>
public partial class QrCodeWorkbench : IAsyncDisposable
{
    /// <summary>
    /// When set (non-empty), the Format control lists only these <see cref="QRCodeFormat"/> values (e.g. PNG and SVG for hosts that register only the built-in encoder). When null or empty, every enum value is listed.
    /// </summary>
    [Parameter]
    public IReadOnlyList<QRCodeFormat>? AllowedFormats { get; set; }

    private static readonly QRCodeFormat[] s_allQrFormats = Enum.GetValues<QRCodeFormat>();

    /// <summary>Formats shown in the workbench Format dropdown.</summary>
    protected IReadOnlyList<QRCodeFormat> SelectableFormats =>
        AllowedFormats is { Count: > 0 } ? AllowedFormats : s_allQrFormats;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        NormalizeFormatToSelectable();
        RefreshEncodedPreview();
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        var previous = _format;
        NormalizeFormatToSelectable();
        if (_format != previous)
            RefreshEncodedPreview();
    }

    private void NormalizeFormatToSelectable()
    {
        var list = SelectableFormats;
        for (var i = 0; i < list.Count; i++) {
            if (list[i] == _format)
                return;
        }

        _format = list[0];
    }

    /// <summary>Dashboard-friendly buckets for <see cref="QRCodeOptions.Size" /> (pixels per module). Total image side ≈ module count × this value.</summary>
    private enum QrWorkbenchModulePreset
    {
        Thumb,
        WebSmall,
        WebDefault,
        WebLarge,
        PrintHd,
        Custom
    }

    private static readonly string ModulePresetHelper =
        "Pick a preset instead of raw pixels-per-module: total width/height ≈ (QR modules per side, often ~33–55 for URLs) × module scale. Hi‑DPI screen use is usually Web small–Web default.";

    private bool _busy;
    private QrPayloadKind _payloadKind = QrPayloadKind.Url;
    private string _encodedPreview = string.Empty;

    private string _plainText = "Hello, QR";

    private string _urlText = "https://example.com";
    private bool _urlForceHttps;

    private string _wifiSsid = string.Empty;
    private string _wifiPassword = string.Empty;
    private QrWifiSecurityType _wifiSecurity = QrWifiSecurityType.Wpa;
    private bool _wifiHidden;

    private string _mailtoTo = string.Empty;
    private string _mailtoSubject = string.Empty;
    private string _mailtoBody = string.Empty;

    private string _telNumber = string.Empty;

    private string _smsNumber = string.Empty;
    private string _smsBody = string.Empty;
    private bool _smsUseSmsto;

    private string _geoLat = "37.8199";
    private string _geoLon = "-122.4783";
    private string _geoLabel = string.Empty;

    private string _vcFn = string.Empty;
    private string _vcTel = string.Empty;
    private string _vcEmail = string.Empty;
    private string _vcOrg = string.Empty;
    private string _vcUrl = string.Empty;

    private string _meName = string.Empty;
    private string _meTel = string.Empty;
    private string _meEmail = string.Empty;

    private string _waPhone = string.Empty;
    private string _tgUsername = string.Empty;
    private string _signalPhone = string.Empty;

    private MudColor _darkColor = new("#000000");
    private bool _drawIconBorder = true;
    private bool _drawQuietZones = true;
    private QRCodeErrorCorrectionLevel _errorCorrectionLevel = QRCodeErrorCorrectionLevel.Medium;
    private QRCodeFormat _format = QRCodeFormat.Png;
    private int _iconSizePercent = 15;
    private string _frameCaption = "Scan Me";
    private int _frameCaptionFontSize;
    private int _frameHeaderMinPx;
    private bool _frameAutoSizeHeader = true;
    private MudColor _frameCaptionColor = new("#FFFFFF");
    private MudColor _frameHeaderBg = new("#1e293b");
    private MudColor _framePanelBg = new("#FFFFFF");
    private int _frameStyleInt;
    private byte[]? _imageBytes;
    private string? _imageSource;
    private byte[]? _logoBytes;
    private string? _logoFileName;
    private MudColor _lightColor = new("#FFFFFF");
    private QrWorkbenchModulePreset _modulePreset = QrWorkbenchModulePreset.WebDefault;
    private int _size = 16;
    private int? _lastOutputWidth;
    private int? _lastOutputHeight;
    private string _statusMessage = string.Empty;
    private Severity _statusSeverity = Severity.Info;
    private int _statusVersion;

    /// <summary>Object URL for SVG full-tab preview (data: SVG navigation is often blocked).</summary>
    private string? _svgPreviewObjectUrl;

    private IJSObjectReference? _previewModule;

    private static readonly FileTypeFlags LogoValidFileTypes =
        FileTypeFlags.Png | FileTypeFlags.Jpeg | FileTypeFlags.Jpg | FileTypeFlags.Gif | FileTypeFlags.Webp | FileTypeFlags.Svg;

    private const long MaxLogoFileBytes = 2 * 1024 * 1024;

    private static readonly LyoFileUpload.UploadProgressViewType LogoUploadProgressView = LyoFileUpload.UploadProgressViewType.None;

    private bool LogoSupported => _format is QRCodeFormat.Png or QRCodeFormat.Svg;

    private QrFrameStyle SelectedFrameStyle => Enum.IsDefined(typeof(QrFrameStyle), _frameStyleInt) ? (QrFrameStyle)_frameStyleInt : QrFrameStyle.None;

    /// <summary>PNG frame modes that can render optional caption text (header or footer band).</summary>
    private bool FrameShowsCaptionField => _format == QRCodeFormat.Png && SelectedFrameStyle != QrFrameStyle.None;

    private LyoFileUpload? _logoFileUpload;
    private LocalBrowserFile? _logoClientFile;

    private Task OnFrameStyleChangedAsync(QrFrameStyle style)
    {
        _frameStyleInt = (int)style;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private static string PayloadKindLabel(QrPayloadKind k)
        => k switch {
            QrPayloadKind.PlainText => "Plain text",
            QrPayloadKind.Url => "URL (http/https)",
            QrPayloadKind.Wifi => "Wi‑Fi",
            QrPayloadKind.Mailto => "Email (mailto)",
            QrPayloadKind.Tel => "Phone (tel)",
            QrPayloadKind.Sms => "SMS",
            QrPayloadKind.Geo => "Location (geo)",
            QrPayloadKind.VCard3 => "Contact (vCard 3.0)",
            QrPayloadKind.MeCard => "Contact (meCard)",
            QrPayloadKind.WhatsApp => "WhatsApp",
            QrPayloadKind.Telegram => "Telegram",
            QrPayloadKind.Signal => "Signal",
            var _ => k.ToString()
        };

    private static string ModulePresetLabel(QrWorkbenchModulePreset p)
        => p switch {
            QrWorkbenchModulePreset.Thumb => "Thumb / chat sticker (~6 px/module, ~240–450 px QR typical)",
            QrWorkbenchModulePreset.WebSmall => "Web small (~10 px/module)",
            QrWorkbenchModulePreset.WebDefault => "Web default (~16 px/module)",
            QrWorkbenchModulePreset.WebLarge => "Web large (~24 px/module)",
            QrWorkbenchModulePreset.PrintHd => "Print / HD (~32 px/module)",
            QrWorkbenchModulePreset.Custom => "Custom pixels per module",
            var _ => p.ToString()
        };

    private static int PresetToModulePixels(QrWorkbenchModulePreset p)
        => p switch {
            QrWorkbenchModulePreset.Thumb => 6,
            QrWorkbenchModulePreset.WebSmall => 10,
            QrWorkbenchModulePreset.WebDefault => 16,
            QrWorkbenchModulePreset.WebLarge => 24,
            QrWorkbenchModulePreset.PrintHd => 32,
            QrWorkbenchModulePreset.Custom => 16,
            var _ => 16
        };

    /// <summary>Rough hint assuming ~37 modules/side (common short URL).</summary>
    private static string ApproxQrSideHint(int pxPerModule)
    {
        const int approxModules = 37;
        var lo = approxModules * pxPerModule;
        var hi = 55 * pxPerModule;
        return $"typical QR square ~{lo}–{hi} px before frame";
    }

    private Task OnModulePresetChangedAsync(QrWorkbenchModulePreset value)
    {
        _modulePreset = value;
        if (value != QrWorkbenchModulePreset.Custom)
            _size = PresetToModulePixels(value);

        StateHasChanged();
        return Task.CompletedTask;
    }

    private Task OnCustomModulePixelsChanged(int value)
    {
        _modulePreset = QrWorkbenchModulePreset.Custom;
        _size = Math.Clamp(value, 4, 128);
        StateHasChanged();
        return Task.CompletedTask;
    }

    private Task OnQrPayloadKindChangedAsync(QrPayloadKind value)
    {
        _payloadKind = value;
        RefreshEncodedPreview();
        return Task.CompletedTask;
    }

    private Task OnWifiSecurityChangedAsync(QrWifiSecurityType value)
    {
        _wifiSecurity = value;
        RefreshEncodedPreview();
        return Task.CompletedTask;
    }

    private Task OnUrlForceHttpsChanged(bool value)
    {
        _urlForceHttps = value;
        RefreshEncodedPreview();
        return Task.CompletedTask;
    }

    private Task OnWifiHiddenChanged(bool value)
    {
        _wifiHidden = value;
        RefreshEncodedPreview();
        return Task.CompletedTask;
    }

    private Task OnSmsUseSmstoChanged(bool value)
    {
        _smsUseSmsto = value;
        RefreshEncodedPreview();
        return Task.CompletedTask;
    }

    private void RefreshEncodedPreview()
    {
        if (TryBuildQrData(out var data, out _))
            _encodedPreview = data;
        else
            _encodedPreview = string.Empty;
    }

    private Task OnLogoClientFileReadyAsync(LocalBrowserFile file)
    {
        _logoBytes = file.Content;
        _logoFileName = file.FileName;
        _logoClientFile = file;
        return Task.CompletedTask;
    }

    private Task OnLogoClientFileRemovedAsync(LocalBrowserFile file)
    {
        _logoBytes = null;
        _logoFileName = null;
        if (ReferenceEquals(_logoClientFile, file))
            _logoClientFile = null;

        return Task.CompletedTask;
    }

    private async Task ClearLogoAsync()
    {
        if (_logoFileUpload != null && _logoClientFile != null)
            await _logoFileUpload.RemoveClientFileAsync(_logoClientFile);
        else {
            _logoBytes = null;
            _logoFileName = null;
            _logoClientFile = null;
        }
    }

    private Task OnFrameHeaderMinPxChangedAsync(int value)
    {
        _frameHeaderMinPx = Math.Clamp(value, 0, 3200);
        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>6-digit <c>#RRGGBB</c> for <see cref="QrFrameLayoutOptions"/> / ImageSharp (never truncate <see cref="MudColor"/> strings — length-9 forms are not always <c>#RRGGBBAA</c>).</summary>
    private static string ToOpaqueRgbHex(MudColor c)
    {
        c.Deconstruct(out var r, out var g, out var b);
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    private QrFrameLayoutOptions BuildFrameLayoutOptions()
    {
        var headerHex = ToOpaqueRgbHex(_frameHeaderBg);
        var opts = new QrFrameLayoutOptions {
            Style = SelectedFrameStyle,
            CaptionText = string.IsNullOrWhiteSpace(_frameCaption) ? null : _frameCaption.Trim(),
            CaptionFontSizePx = _frameCaptionFontSize,
            HeaderHeightPx = _frameHeaderMinPx > 0 ? _frameHeaderMinPx : 52,
            AutoSizeHeaderToCaption = SelectedFrameStyle == QrFrameStyle.BadgeWithHeader && _frameAutoSizeHeader,
            HeaderBackgroundHex = headerHex,
            HeaderCaptionTextHex = ToOpaqueRgbHex(_frameCaptionColor),
            PanelBackgroundHex = ToOpaqueRgbHex(_framePanelBg)
        };

        // Badge: outer card stroke should read as part of the header chrome (defaults to slate otherwise).
        if (SelectedFrameStyle == QrFrameStyle.BadgeWithHeader)
            opts.CardOutlineHex = headerHex;

        return opts;
    }

    private static string FrameStyleLabel(QrFrameStyle style)
        => style switch {
            QrFrameStyle.None => "None",
            QrFrameStyle.BadgeWithHeader => "Badge with header",
            QrFrameStyle.SimpleRoundedPanel => "Rounded panel",
            QrFrameStyle.BorderOnly => "Border only",
            var _ => style.ToString()
        };

    /// <summary>Builds the QR string from the current payload kind and fields.</summary>
    private bool TryBuildQrData(out string data, out string? error)
    {
        data = string.Empty;
        error = null;
        try {
            data = _payloadKind switch {
                QrPayloadKind.PlainText => new PlainTextQrPayload(_plainText).ToQrString(),
                QrPayloadKind.Url => new HttpUrlPayload(_urlText, _urlForceHttps).ToQrString(),
                QrPayloadKind.Wifi => new WifiQrPayload(_wifiSsid, _wifiPassword, _wifiSecurity, _wifiHidden).ToQrString(),
                QrPayloadKind.Mailto => new MailtoPayload(_mailtoTo, _mailtoSubject, _mailtoBody).ToQrString(),
                QrPayloadKind.Tel => new TelPayload(_telNumber).ToQrString(),
                QrPayloadKind.Sms => new SmsPayload(_smsNumber, string.IsNullOrWhiteSpace(_smsBody) ? null : _smsBody, _smsUseSmsto).ToQrString(),
                QrPayloadKind.Geo => BuildGeoPayload().ToQrString(),
                QrPayloadKind.VCard3 => new VCard3Payload(_vcFn, _vcTel, _vcEmail, _vcOrg, _vcUrl).ToQrString(),
                QrPayloadKind.MeCard => new MeCardPayload(_meName, _meTel, _meEmail).ToQrString(),
                QrPayloadKind.WhatsApp => new WhatsAppUrlPayload(_waPhone).ToQrString(),
                QrPayloadKind.Telegram => new TelegramUrlPayload(_tgUsername).ToQrString(),
                QrPayloadKind.Signal => new SignalUrlPayload(_signalPhone).ToQrString(),
                var x => throw new ArgumentOutOfRangeException(nameof(_payloadKind), x, null)
            };
            return true;
        }
        catch (Exception ex) {
            error = ex.Message;
            return false;
        }
    }

    private GeoPayload BuildGeoPayload()
    {
        if (!double.TryParse(_geoLat.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var lat))
            throw new FormatException("Latitude must be a number (e.g. 37.8199).");

        if (!double.TryParse(_geoLon.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
            throw new FormatException("Longitude must be a number (e.g. -122.4783).");

        return new GeoPayload(lat, lon, string.IsNullOrWhiteSpace(_geoLabel) ? null : _geoLabel);
    }

    private async Task GenerateAsync()
    {
        if (!TryBuildQrData(out var data, out var err)) {
            SetStatus(err ?? "Invalid payload.", Severity.Warning);
            _encodedPreview = string.Empty;
            return;
        }

        _encodedPreview = data;

        _busy = true;
        try {
            var options = new QRCodeOptions {
                Format = _format,
                Size = _size,
                ErrorCorrectionLevel = _errorCorrectionLevel,
                DarkColor = _darkColor.ToString(MudColorOutputFormats.Hex),
                LightColor = _lightColor.ToString(MudColorOutputFormats.Hex),
                DrawQuietZones = _drawQuietZones
            };

            if (LogoSupported && _logoBytes is { Length: > 0 }) {
                var iconPct = QRCodeIconOptions.ClampIconSizePercent(_iconSizePercent);
                options.Icon = new() { IconBytes = _logoBytes, IconSizePercent = iconPct, DrawIconBorder = _drawIconBorder };
            }

            var result = await QrCodeService.GenerateAsync(data, options);
            if (!result.IsSuccess) {
                SetStatus(LyoResultErrorFormatter.FormatErrors(result.Errors), Severity.Error);
                return;
            }

            if (result is not QRCodeResult qrResult || qrResult.ImageBytes is not { Length: > 0 }) {
                SetStatus("QR generation did not return image bytes (unexpected result type).", Severity.Error);
                return;
            }

            var imageBytes = qrResult.ImageBytes;
            _lastOutputWidth = null;
            _lastOutputHeight = null;
            if (_format == QRCodeFormat.Png && SelectedFrameStyle != QrFrameStyle.None) {
                var frameOpts = BuildFrameLayoutOptions();
                var framed = await QrFrameLayout.CompositeQrFramePngAsync(imageBytes, frameOpts, CancellationToken.None).ConfigureAwait(false);
                if ((!framed.IsSuccess || framed.Data == null) && ImageService != null)
                    framed = await ImageService.CompositeQrFramePngAsync(imageBytes, frameOpts, CancellationToken.None).ConfigureAwait(false);

                if (!framed.IsSuccess || framed.Data == null) {
                    SetStatus(LyoResultErrorFormatter.FormatErrors(framed.Errors), Severity.Error);
                    return;
                }

                imageBytes = framed.Data;
            }

            await RevokeSvgPreviewObjectUrlAsync();

            _imageBytes = imageBytes;
            if (_format == QRCodeFormat.Png) {
                try {
                    await using var dimMs = new MemoryStream(imageBytes);
                    var info = await SixLabors.ImageSharp.Image.IdentifyAsync(dimMs);
                    _lastOutputWidth = info.Width;
                    _lastOutputHeight = info.Height;
                }
                catch {
                    /* ignore decode failures for metadata */
                }
            }

            if (_format == QRCodeFormat.Svg) {
                _imageSource = null;
                await EnsurePreviewModuleAsync();
                if (_previewModule != null) {
                    var b64 = Convert.ToBase64String(imageBytes);
                    _svgPreviewObjectUrl = await _previewModule.InvokeAsync<string>("createBlobUrlFromBase64", b64, GetMimeType());
                }
                else
                    _svgPreviewObjectUrl = null;
            }
            else {
                _svgPreviewObjectUrl = null;
                _imageSource = $"data:{GetMimeType()};base64,{Convert.ToBase64String(imageBytes)}";
            }

            SetStatus(qrResult.Message ?? "QR code generated.", Severity.Success);
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
        if (_imageBytes == null)
            return;

        await Js.DownloadFile(_imageBytes, $"qr-code.{GetExtension()}", GetMimeType());
    }

    private string GetExtension()
        => _format switch {
            QRCodeFormat.Png => "png",
            QRCodeFormat.Svg => "svg",
            QRCodeFormat.Jpeg => "jpg",
            QRCodeFormat.Bitmap => "bmp",
            var _ => "bin"
        };

    private string GetMimeType()
        => _format switch {
            QRCodeFormat.Png => FileTypeInfo.Png.MimeType,
            QRCodeFormat.Svg => FileTypeInfo.Svg.MimeType,
            QRCodeFormat.Jpeg => FileTypeInfo.Jpeg.MimeType,
            QRCodeFormat.Bitmap => FileTypeInfo.Bmp.MimeType,
            var _ => FileTypeInfo.Unknown.MimeType
        };

    private void SetStatus(string message, Severity severity)
    {
        _statusMessage = message;
        _statusSeverity = severity;
        var version = ++_statusVersion;
        Snackbar.Add(message, severity);
        _ = ClearStatusLaterAsync(version);
    }

    private async Task ClearStatusLaterAsync(int version)
    {
        await Task.Delay(2500);
        if (version != _statusVersion)
            return;

        _statusMessage = string.Empty;
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>Data URL fallback for SVG thumbnail when blob URL is unavailable.</summary>
    private string? SvgBytesDataUrl =>
        _format == QRCodeFormat.Svg && _imageBytes is { Length: > 0 }
            ? $"data:{FileTypeInfo.Svg.MimeType};base64,{Convert.ToBase64String(_imageBytes)}"
            : null;

    /// <summary>Raster or SVG thumbnail source (SVG uses blob URL when possible so &lt;img&gt; scales like PNG).</summary>
    private string? ResultThumbnailSrc =>
        _imageBytes is not { Length: > 0 }
            ? null
            : _format == QRCodeFormat.Svg ? (_svgPreviewObjectUrl ?? SvgBytesDataUrl) : _imageSource;

    /// <summary>Href for full-size preview (new tab). SVG prefers blob: because navigations to data:image/svg+xml are often blocked.</summary>
    private string? ResultPreviewHref =>
        _imageBytes is not { Length: > 0 }
            ? null
            : _format == QRCodeFormat.Svg ? (_svgPreviewObjectUrl ?? SvgBytesDataUrl) : _imageSource;

    private async Task EnsurePreviewModuleAsync()
    {
        if (_previewModule != null)
            return;

        try {
            _previewModule = await JsRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                "./_content/Lyo.QRCode.Web.Components/scripts/lyoQrPreview.js");
        }
        catch {
            _previewModule = null;
        }
    }

    private async Task RevokeSvgPreviewObjectUrlAsync()
    {
        var url = _svgPreviewObjectUrl;
        _svgPreviewObjectUrl = null;
        if (string.IsNullOrEmpty(url))
            return;

        if (_previewModule == null)
            return;

        try {
            await _previewModule.InvokeVoidAsync("revokeBlobUrl", url);
        }
        catch {
            /* ignore */
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await RevokeSvgPreviewObjectUrlAsync();
        if (_previewModule is not null) {
            try {
                await _previewModule.DisposeAsync();
            }
            catch {
                /* ignore */
            }

            _previewModule = null;
        }
    }
}
