namespace Lyo.Screenshot;

/// <summary>
/// Encoded shot. <see cref="PreviewPng" /> is the picture shown in the window and copied to the clipboard.
/// A still or a receipt uses the same bytes for both. A GIF keeps <see cref="Bytes" /> as the animation and <see cref="PreviewPng" /> as the first frame.
/// </summary>
public readonly record struct CapturedShot(byte[] Bytes, byte[] PreviewPng);
