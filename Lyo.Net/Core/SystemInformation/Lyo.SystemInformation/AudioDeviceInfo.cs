namespace Lyo.SystemInformation;

/// <summary>Facts for one audio endpoint. Fields the current platform cannot supply are <see langword="null" />.</summary>
/// <param name="Name">Device name (ALSA PCM name, or the Windows endpoint friendly name).</param>
/// <param name="Kind"><see cref="Playback" />, <see cref="Capture" />, or <see cref="Duplex" />.</param>
/// <param name="Description">Card long name on Linux, or the Windows device description. Null when the platform does not report one.</param>
/// <param name="FormFactor">
/// Windows endpoint form factor (for example <c>Speakers</c>, <c>Headphones</c>, <c>Headset</c>, <c>Microphone</c>, <c>Hdmi</c>). Null on Linux, where ALSA
/// proc does not classify speakers.
/// </param>
/// <param name="IsDefault">True when this is the console default for its direction. Null when the platform does not report a default.</param>
public sealed record AudioDeviceInfo(string Name, string Kind, string? Description, string? FormFactor, bool? IsDefault)
{
    /// <summary>Kind for a playback-only endpoint.</summary>
    public const string Playback = "Playback";

    /// <summary>Kind for a capture-only endpoint.</summary>
    public const string Capture = "Capture";

    /// <summary>Kind for an endpoint that both plays and captures.</summary>
    public const string Duplex = "Duplex";
}