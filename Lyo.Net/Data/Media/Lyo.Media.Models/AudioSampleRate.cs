using System.Collections.Concurrent;
using System.Globalization;
using Lyo.Exceptions;

namespace Lyo.Media.Models;

/// <summary>Output or input sample rate in hertz. Lookups are thread-safe. Use <see cref="FromHertz" /> for a rate that is not listed.</summary>
public sealed record AudioSampleRate
{
    private static readonly ConcurrentDictionary<string, AudioSampleRate> ById = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, AudioSampleRate> ByName = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>8 kHz.</summary>
    public static readonly AudioSampleRate Hz8000 = Create("Hz8000", 8000);

    /// <summary>16 kHz.</summary>
    public static readonly AudioSampleRate Hz16000 = Create("Hz16000", 16000);

    /// <summary>22.05 kHz.</summary>
    public static readonly AudioSampleRate Hz22050 = Create("Hz22050", 22050);

    /// <summary>44.1 kHz.</summary>
    public static readonly AudioSampleRate Hz44100 = Create("Hz44100", 44100);

    /// <summary>48 kHz.</summary>
    public static readonly AudioSampleRate Hz48000 = Create("Hz48000", 48000);

    /// <summary>96 kHz.</summary>
    public static readonly AudioSampleRate Hz96000 = Create("Hz96000", 96000);

    /// <summary>Every built-in rate. Touching this list registers the static instances for lookup.</summary>
    public static IReadOnlyList<AudioSampleRate> BuiltIns { get; } = [Hz8000, Hz16000, Hz22050, Hz44100, Hz48000, Hz96000];

    /// <summary>Stable display name (e.g. <c>Hz48000</c>).</summary>
    public string Name { get; }

    /// <summary>Decimal hertz token (e.g. <c>48000</c>).</summary>
    public string Id { get; }

    /// <summary>Sample rate in hertz.</summary>
    public int Hertz { get; }

    private AudioSampleRate(string name, int hertz)
    {
        ArgumentHelpers.ThrowIfLessThan(hertz, 1);
        Name = name;
        Hertz = hertz;
        Id = hertz.ToString(CultureInfo.InvariantCulture);
        NamedCatalog.Register(ByName, ById, this, Name, Id);
    }

    /// <summary>Looks up a rate by hertz token.</summary>
    public static AudioSampleRate? TryFromId(string? id)
    {
        _ = BuiltIns;
        return NamedCatalog.TryFromId(ById, id);
    }

    /// <summary>Looks up a rate by display name.</summary>
    public static AudioSampleRate? TryFromName(string? name)
    {
        _ = BuiltIns;
        return NamedCatalog.TryFromName(ByName, name);
    }

    /// <summary>Returns a rate for <paramref name="hertz" />. Reuses a built-in when the value is already known.</summary>
    public static AudioSampleRate FromHertz(int hertz)
    {
        _ = BuiltIns;
        ArgumentHelpers.ThrowIfLessThan(hertz, 1);
        var key = hertz.ToString(CultureInfo.InvariantCulture);
        return NamedCatalog.GetOrCustom(ById, key, _ => new(key, hertz));
    }

    /// <inheritdoc />
    public override string ToString() => Name;

    private static AudioSampleRate Create(string name, int hertz) => new(name, hertz);
}