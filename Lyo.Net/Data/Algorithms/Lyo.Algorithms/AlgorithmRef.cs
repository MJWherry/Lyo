using Lyo.Exceptions;

namespace Lyo.Algorithms;

/// <summary>Points at one catalog algorithm. Exactly one of latest, a revision number, or a label.</summary>
public sealed record AlgorithmRef
{
    private AlgorithmRef(string key, AlgorithmAddress address, int? revision, string? label)
    {
        Key = key;
        Address = address;
        RevisionNumber = revision;
        LabelName = label;
    }

    /// <summary>Catalog key.</summary>
    public string Key { get; }

    /// <summary>Which revision selector is set.</summary>
    public AlgorithmAddress Address { get; }

    /// <summary>Set when <see cref="Address" /> is <see cref="AlgorithmAddress.Revision" />.</summary>
    public int? RevisionNumber { get; }

    /// <summary>Set when <see cref="Address" /> is <see cref="AlgorithmAddress.Label" />.</summary>
    public string? LabelName { get; }

    /// <summary>Follow the current revision. Hot-updates when a new revision is saved.</summary>
    public static AlgorithmRef Latest(string key)
    {
        AlgorithmKeys.ThrowIfInvalidKey(key);
        return new(key, AlgorithmAddress.Latest, null, null);
    }

    /// <summary>Pin snapshot <paramref name="revision" />. Tournaments and published reports use this.</summary>
    public static AlgorithmRef Revision(string key, int revision)
    {
        AlgorithmKeys.ThrowIfInvalidKey(key);
        ArgumentHelpers.ThrowIfLessThan(revision, 1);
        return new(key, AlgorithmAddress.Revision, revision, null);
    }

    /// <summary>Resolve <paramref name="label" /> at execute time. Mutable labels hot-update; immutable labels do not.</summary>
    public static AlgorithmRef Label(string key, string label)
    {
        AlgorithmKeys.ThrowIfInvalidKey(key);
        AlgorithmKeys.ThrowIfInvalidLabel(label);
        return new(key, AlgorithmAddress.Label, null, label);
    }
}
