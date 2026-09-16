namespace Lyo.Authorization;

/// <summary>List filter. The default hides revoked and expired rows.</summary>
public sealed class GrantListQuery
{
    /// <summary>Default page size.</summary>
    public const int DefaultTake = 100;

    /// <summary>Largest page the store will return.</summary>
    public const int MaxTake = 1000;

    /// <summary>Optional subject filter.</summary>
    public string? Subject { get; init; }

    /// <summary>When true, revoked rows are included.</summary>
    public bool IncludeRevoked { get; init; }

    /// <summary>When true, expired rows are included.</summary>
    public bool IncludeExpired { get; init; }

    /// <summary>Page size. Default 100. Values above <see cref="MaxTake" /> throw.</summary>
    public int Take { get; init; } = DefaultTake;
}
