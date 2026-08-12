namespace Lyo.Common.Enums;

/// <summary>Service category for grouping well-known <see cref="Records.PortInfo" /> entries.</summary>
public enum PortCategory
{
    /// <summary>Unknown or unregistered port.</summary>
    Unknown = 0,

    /// <summary>HTTP / HTTPS and common alternate web ports.</summary>
    Web,

    /// <summary>SMTP, IMAP, POP3 and related mail submission / retrieval.</summary>
    Mail,

    /// <summary>FTP and similar file-transfer protocols.</summary>
    FileTransfer,

    /// <summary>SSH, RDP, and other remote-access protocols.</summary>
    RemoteAccess,

    /// <summary>LDAP / directory services.</summary>
    Directory,

    /// <summary>DNS and related name services.</summary>
    NameService,

    /// <summary>NTP and time synchronization.</summary>
    Time,

    /// <summary>Relational and document databases.</summary>
    Database,

    /// <summary>Message brokers and queue protocols (e.g. AMQP).</summary>
    Messaging,

    /// <summary>Caches such as Redis and Memcached.</summary>
    Cache,

    /// <summary>Search engines (e.g. Elasticsearch).</summary>
    Search,

    /// <summary>Security / scanning services (e.g. ClamAV).</summary>
    Security
}
