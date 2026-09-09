using System.Collections.Concurrent;
using Lyo.Common.Core.Enums;
using Lyo.Exceptions;

namespace Lyo.Common.Core.Records;

/// <summary>
/// Extensible identifier for an audio or video container (file type). Built-ins cover common formats
/// (<see cref="Mp4" />, <see cref="WebM" />, <see cref="Wav" />, <see cref="Flac" />, …).
/// Use <see cref="Custom" /> for a format that is not listed. Lookups are thread-safe.
/// </summary>
public sealed record MediaContainer
{
    private static readonly ConcurrentDictionary<string, MediaContainer> ByExt = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, MediaContainer> ByFormat = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, MediaContainer> ByName = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>MPEG-4 (<c>mp4</c> / <c>.mp4</c>).</summary>
    public static readonly MediaContainer Mp4 = new("Mp4", "mp4", ".mp4");

    /// <summary>WebM (<c>webm</c> / <c>.webm</c>).</summary>
    public static readonly MediaContainer WebM = new("WebM", "webm", ".webm");

    /// <summary>QuickTime (<c>mov</c> / <c>.mov</c>).</summary>
    public static readonly MediaContainer Mov = new("Mov", "mov", ".mov");

    /// <summary>MP3 (<c>mp3</c> / <c>.mp3</c>).</summary>
    public static readonly MediaContainer Mp3 = new("Mp3", "mp3", ".mp3");

    /// <summary>WAV (<c>wav</c> / <c>.wav</c>).</summary>
    public static readonly MediaContainer Wav = new("Wav", "wav", ".wav");

    /// <summary>Raw signed 16-bit little-endian PCM (<c>s16le</c>).</summary>
    public static readonly MediaContainer S16le = new("S16le", "s16le", ".s16le", [".pcm"]);

    /// <summary>AAC ADTS (<c>adts</c> / <c>.aac</c>).</summary>
    public static readonly MediaContainer Adts = new("Adts", "adts", ".aac");

    /// <summary>Ogg (<c>ogg</c> / <c>.ogg</c>).</summary>
    public static readonly MediaContainer Ogg = new("Ogg", "ogg", ".ogg");

    /// <summary>FLAC (<c>flac</c> / <c>.flac</c>).</summary>
    public static readonly MediaContainer Flac = new("Flac", "flac", ".flac");

    /// <summary>Every built-in container. Touching this list registers the static instances for lookup.</summary>
    public static IReadOnlyList<MediaContainer> BuiltIns { get; } = [Mp4, WebM, Mov, Mp3, Wav, S16le, Adts, Ogg, Flac];

    /// <summary>Stable display name (e.g. <c>Mp4</c>).</summary>
    public string Name { get; }

    /// <summary>Canonical format token (e.g. <c>mp4</c>, <c>wav</c>, <c>s16le</c>).</summary>
    public string Format { get; }

    /// <summary>Default file extension including the leading dot.</summary>
    public string Extension { get; }

    /// <summary>True when this is MPEG-4 (<see cref="Mp4" /> or format token <c>mp4</c>).</summary>
    public bool IsMp4 => string.Equals(Format, "mp4", StringComparison.OrdinalIgnoreCase);

    private MediaContainer(string name, string format, string extension, IReadOnlyList<string>? extraExtensions = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(format);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(extension);
        Name = name;
        Format = NormalizeFormat(format);
        Extension = NormalizeExtension(extension);
        ByName.TryAdd(Name, this);
        ByFormat.TryAdd(Format, this);
        ByExt.TryAdd(Extension, this);
        if (extraExtensions == null)
            return;

        foreach (var extra in extraExtensions) {
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(extra);
            ByExt.TryAdd(NormalizeExtension(extra), this);
        }
    }

    /// <summary>Looks up a built-in or previously created custom container by <see cref="Format" /> token.</summary>
    public static MediaContainer? TryFromFormat(string? format)
    {
        _ = BuiltIns;
        return !string.IsNullOrEmpty(format) && ByFormat.TryGetValue(NormalizeFormat(format), out var found) ? found : null;
    }

    /// <summary>Looks up a container by display <see cref="Name" />.</summary>
    public static MediaContainer? TryFromName(string? name)
    {
        _ = BuiltIns;
        return !string.IsNullOrEmpty(name) && ByName.TryGetValue(name, out var found) ? found : null;
    }

    /// <summary>Looks up a container by file extension (with or without a leading dot).</summary>
    public static MediaContainer? TryFromExtension(string? extension)
    {
        _ = BuiltIns;
        return !string.IsNullOrEmpty(extension) && ByExt.TryGetValue(NormalizeExtension(extension), out var found) ? found : null;
    }

    /// <summary>Returns a container for an arbitrary format token. Reuses a registered instance when the token is already known.</summary>
    public static MediaContainer Custom(string format)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(format);
        var key = NormalizeFormat(format);
        var existing = TryFromFormat(key);
        if (existing != null)
            return existing;

        var created = new MediaContainer(key, key, "." + key);
        return ByFormat.GetOrAdd(key, created);
    }

    /// <summary>Maps TTS/STT <see cref="AudioFormat" /> onto a container. Returns null for <see cref="AudioFormat.Unknown" />.</summary>
    public static MediaContainer? TryFromAudioFormat(AudioFormat format)
        => format switch {
            AudioFormat.Wav => Wav,
            AudioFormat.Mp3 => Mp3,
            AudioFormat.Ogg => Ogg,
            AudioFormat.Flac => Flac,
            AudioFormat.Aac => Adts,
            AudioFormat.M4a => Mp4,
            AudioFormat.Opus => Ogg,
            AudioFormat.Pcm => S16le,
            AudioFormat.Webm => WebM,
            var _ => null
        };

    /// <summary>Maps this container onto <see cref="AudioFormat" /> when there is a match; otherwise <see cref="AudioFormat.Unknown" />.</summary>
    public AudioFormat ToAudioFormat()
    {
        if (ReferenceEquals(this, Wav) || string.Equals(Format, "wav", StringComparison.OrdinalIgnoreCase))
            return AudioFormat.Wav;
        if (ReferenceEquals(this, Mp3) || string.Equals(Format, "mp3", StringComparison.OrdinalIgnoreCase))
            return AudioFormat.Mp3;
        if (ReferenceEquals(this, Ogg) || string.Equals(Format, "ogg", StringComparison.OrdinalIgnoreCase))
            return AudioFormat.Ogg;
        if (ReferenceEquals(this, Flac) || string.Equals(Format, "flac", StringComparison.OrdinalIgnoreCase))
            return AudioFormat.Flac;
        if (ReferenceEquals(this, Adts) || string.Equals(Format, "adts", StringComparison.OrdinalIgnoreCase))
            return AudioFormat.Aac;
        if (IsMp4)
            return AudioFormat.M4a;
        if (ReferenceEquals(this, S16le) || string.Equals(Format, "s16le", StringComparison.OrdinalIgnoreCase))
            return AudioFormat.Pcm;
        if (ReferenceEquals(this, WebM) || string.Equals(Format, "webm", StringComparison.OrdinalIgnoreCase))
            return AudioFormat.Webm;

        return AudioFormat.Unknown;
    }

    /// <inheritdoc />
    public override string ToString() => Name;

    private static string NormalizeFormat(string format) => format.Trim().TrimStart('.').ToLowerInvariant();

    private static string NormalizeExtension(string extension)
    {
        var trimmed = extension.Trim();
        if (trimmed.Length == 0)
            return trimmed;

        return trimmed[0] == '.' ? trimmed.ToLowerInvariant() : "." + trimmed.ToLowerInvariant();
    }
}
