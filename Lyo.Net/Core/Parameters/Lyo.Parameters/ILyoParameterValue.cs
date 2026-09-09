namespace Lyo.Parameters;

/// <summary>A parameter value supplied to a run, generation, schedule, or trigger. Carries the declared type so callers can round-trip without guessing.</summary>
public interface ILyoParameterValue : ILyoKeyedValue
{
    /// <summary>Human-readable note about the parameter, copied from the definition when one is present.</summary>
    string? Description { get; }

    /// <summary>CLR <see cref="Type.FullName" /> of the value, resolved through <see cref="Records.LyoTypeInfo" />.</summary>
    string Type { get; }

    /// <summary>Ciphertext for secret parameters. When set, <see cref="ILyoKeyedValue.Value" /> is null or a mask.</summary>
    byte[]? EncryptedValue { get; }
}
