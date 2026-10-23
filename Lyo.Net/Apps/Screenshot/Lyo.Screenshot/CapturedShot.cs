using System.Diagnostics;

namespace Lyo.Screenshot;

/// <summary>
/// Encoded shot. <see cref="PreviewPng" /> is the picture shown in the window and copied to the clipboard. A still or a receipt uses the same bytes for both. A GIF keeps
/// <see cref="Bytes" /> as the animation and <see cref="PreviewPng" /> as the first frame.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct CapturedShot(byte[] Bytes, byte[] PreviewPng)
{
    /// <summary>Byte lengths of the shot and the preview. Image bytes are omitted.</summary>
    public override string ToString() => $"Bytes={Bytes?.Length ?? 0} PreviewPng={PreviewPng?.Length ?? 0}";
}