using System.Net.Http.Json;
using System.Text;
using Lyo.Barcode.Models;
using Lyo.Common.Metadata.Records;
using Lyo.Web.Components;
using Lyo.Web.Primitives;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Utilities;

namespace Lyo.Barcode.Web.Components;

public partial class BarcodeWorkbench
{
    private bool _busy;
    private int _barHeight = 80;
    private string _data = "HELLO-128";
    private MudColor _darkColor = new("#000000");
    private BarcodeFormat _format = BarcodeFormat.Bmp;
    private int _heightPx;
    private byte[]? _imageBytes;
    private string? _imageSource;
    private MudColor _lightColor = new("#FFFFFF");
    private bool _showBorder;
    private int _borderWidthPx = 4;
    private MudColor _borderColor = new("#000000");
    private int _moduleWidth = 2;
    private int _quietZone = 10;
    private bool _showHumanReadable;
    private string _humanReadableOverride = string.Empty;
    private int _humanReadableFontPx = 14;
    private int _humanReadableMarginTopPx = 6;
    private int _humanReadableMarginBottomPx = 4;
    private MudColor _humanReadableColor = new("#000000");
    private string _svgMarkup = string.Empty;
    private int _widthPx;

    private static string ToOpaqueRgbHex(MudColor c)
    {
        var s = c.ToString(MudColorOutputFormats.Hex);
        return s is { Length: 9 } && s[0] == '#' ? s[..7] : s;
    }

    private async Task GenerateAsync()
    {
        if (string.IsNullOrWhiteSpace(_data)) {
            SetStatus("Enter text to encode.", Severity.Warning);
            return;
        }

        _busy = true;
        try {
            var options = new BarcodeOptions {
                Format = _format,
                ModuleWidthPixels = _moduleWidth,
                BarHeightPixels = _barHeight,
                QuietZoneModules = _quietZone,
                DarkColor = _darkColor.ToString(MudColorOutputFormats.Hex),
                LightColor = _lightColor.ToString(MudColorOutputFormats.Hex),
                ShowBorder = _showBorder,
                BorderWidthPixels = _borderWidthPx,
                BorderColorHex = ToOpaqueRgbHex(_borderColor),
                ShowHumanReadableTextBelow = _showHumanReadable,
                HumanReadableText = string.IsNullOrWhiteSpace(_humanReadableOverride) ? null : _humanReadableOverride,
                HumanReadableFontSizePixels = _humanReadableFontPx,
                HumanReadableMarginTopPixels = _humanReadableMarginTopPx,
                HumanReadableMarginBottomPixels = _humanReadableMarginBottomPx,
                HumanReadableColorHex = !_showHumanReadable ? null : string.Equals(_humanReadableColor.ToString(MudColorOutputFormats.Hex), _darkColor.ToString(MudColorOutputFormats.Hex), StringComparison.OrdinalIgnoreCase) ? null : _humanReadableColor.ToString(MudColorOutputFormats.Hex)
            };

            var result = await BarcodeService.GenerateAsync(_data, BarcodeSymbology.Code128, options);
            var bcResult = result as BarcodeResult;
            if (!result.IsSuccess || bcResult?.ImageBytes == null) {
                SetStatus(LyoResultErrorFormatter.FormatErrors(result.Errors), Severity.Error);
                return;
            }

            _imageBytes = bcResult.ImageBytes;
            _widthPx = bcResult.ImageWidthPixels ?? 0;
            _heightPx = bcResult.ImageHeightPixels ?? 0;
            if (_format == BarcodeFormat.Svg) {
                _svgMarkup = Encoding.UTF8.GetString(bcResult.ImageBytes);
                _imageSource = null;
            }
            else {
                _svgMarkup = string.Empty;
                _imageSource = $"data:{GetMimeType()};base64,{Convert.ToBase64String(bcResult.ImageBytes)}";
            }

            SetStatus(bcResult.Message ?? "Barcode generated.", Severity.Success);
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

        await Js.DownloadFile(_imageBytes, $"barcode.{GetExtension()}", GetMimeType());
    }

    private string GetExtension()
        => _format switch {
            BarcodeFormat.Bmp => "bmp",
            BarcodeFormat.Svg => "svg",
            var _ => "bin"
        };

    private string GetMimeType()
        => _format switch {
            BarcodeFormat.Bmp => FileTypeInfo.Bmp.MimeType,
            BarcodeFormat.Svg => FileTypeInfo.Svg.MimeType,
            var _ => FileTypeInfo.Unknown.MimeType
        };
}
