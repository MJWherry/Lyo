
namespace Lyo.Common.Core.Enums;

/// <summary>Which UUID variant to mint.</summary>
public enum GuidVersion
{
    /// <summary>Version 3: deterministic MD5 of a namespace GUID plus name (RFC 9562). Call <see cref="LyoGuid.CreateV3" /> directly.</summary>
    V3 = 3,

    /// <summary>Version 4: random bits (RFC 9562).</summary>
    V4 = 4,

    /// <summary>Version 5: deterministic SHA-1 of a namespace GUID plus name (RFC 9562). Call <see cref="LyoGuid.CreateV5" /> directly.</summary>
    V5 = 5,

    /// <summary>Version 6: Gregorian 100-ns UUID timestamp, reordered so values sort lexicographically (RFC 9562).</summary>
    V6 = 6,

    /// <summary>Version 7: Unix-millisecond prefix, time-ordered (RFC 9562). Preferred for database primary keys.</summary>
    V7 = 7,

    /// <summary>COMB for PostgreSQL: 6-byte millisecond timestamp in the leading bytes so inserts stay sequential on Postgres leading-byte sort.</summary>
    CombPostgres = 100,

    /// <summary>COMB for SQL Server: 6-byte millisecond timestamp in the trailing bytes so inserts stay sequential on SQL Server trailing-byte sort.</summary>
    CombSqlServer = 101
}