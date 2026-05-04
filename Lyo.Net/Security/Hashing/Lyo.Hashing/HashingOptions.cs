using Lyo.Common.Enums;
using Lyo.Hashing.Files;

namespace Lyo.Hashing;

/// <summary>Frozen defaults for <see cref="HashingService" />.</summary>
public sealed class HashingOptions
{
    /// <summary>Process-wide sensible defaults (<see cref="HashingService.Shared" />).</summary>
    public static HashingOptions Default { get; } = new();

    /// <summary>Default letter casing when emitting hex from service helpers.</summary>
    public TextLetterCase DefaultHexLetterCase { get; set; } = TextLetterCase.Upper;

    /// <summary>Copy used for sparse file fingerprint when callers pass no override.</summary>
    public FileFingerprintOptions FingerprintDefaults { get; set; } = FileFingerprintOptions.Default;
}