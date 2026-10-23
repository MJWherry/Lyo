using Lyo.Exceptions;

namespace Lyo.Law;

/// <summary>Rules for a state jurisdiction key. Federal text does not belong in this schema.</summary>
public static class LawJurisdictionKey
{
    /// <summary>The key reserved for a future federal store.</summary>
    public const string Federal = "US";

    /// <summary>Rejects <see cref="Federal" /> so United States Code cannot be written into the state schema.</summary>
    /// <param name="key">Jurisdiction key, such as <c>US-PA</c>.</param>
    /// <exception cref="ArgumentException">The key is missing or is <see cref="Federal" />.</exception>
    public static void ThrowIfFederal(string key)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        ArgumentHelpers.ThrowIf(key.Trim().Equals(Federal, StringComparison.OrdinalIgnoreCase), "Federal law is not stored in the state statute schema.", nameof(key));
    }
}