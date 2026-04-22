using Lyo.Common.Identifiers;

namespace Lyo.Common.Enums;

/// <summary>UUID variant to generate.</summary>
public enum GuidVersion
{
    /// <summary>Version 3: deterministic, MD5 hash of a namespace GUID + name (RFC 9562). Use <see cref="LyoGuid.CreateV3" /> directly.</summary>
    V3 = 3,

    /// <summary>Version 4: fully random (RFC 9562).</summary>
    V4 = 4,

    /// <summary>Version 5: deterministic, SHA-1 hash of a namespace GUID + name (RFC 9562). Use <see cref="LyoGuid.CreateV5" /> directly.</summary>
    V5 = 5,

    /// <summary>Version 6: time-ordered using the Gregorian 100-ns UUID timestamp, reordered for lexicographic sortability (RFC 9562).</summary>
    V6 = 6,

    /// <summary>Version 7: Unix-millisecond timestamp prefix, time-ordered (RFC 9562). Recommended for database primary keys.</summary>
    V7 = 7,

    /// <summary>COMB variant optimised for PostgreSQL: 6-byte millisecond timestamp in the leading bytes so inserts are sequential on Postgres's leading-byte sort order.</summary>
    CombPostgres = 100,

    /// <summary>COMB variant optimised for SQL Server: 6-byte millisecond timestamp in the trailing bytes so inserts are sequential on SQL Server's trailing-byte sort order.</summary>
    CombSqlServer = 101
}