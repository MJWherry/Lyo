using Lyo.Common.Core.Enums;
using Lyo.Hashing.Files;

namespace Lyo.Hashing;

/// <summary>Frozen defaults for <see cref="HashingService" />.</summary>
public sealed class HashingOptions
{
    /// <summary>Process-wide defaults used by <see cref="HashingService.Shared" />.</summary>
    public static HashingOptions Default { get; } = new();

    /// <summary>Default letter case when service helpers emit hex.</summary>
    public TextLetterCase DefaultHexLetterCase { get; set; } = TextLetterCase.Upper;

    /// <summary>Copy used for sparse file fingerprints when callers omit an override.</summary>
    public FileFingerprintOptions FingerprintDefaults { get; set; } = FileFingerprintOptions.Default;
}