namespace Lyo.Common.Core.Enums;

/// <summary>How well-known <see cref="Records.PortInfo" /> rows are grouped by service.</summary>
public enum PortCategory
{
    /// <summary>Port is not known or not registered.</summary>
    Unknown = 0,

    /// <summary>HTTP, HTTPS, and common alternate web ports.</summary>
    Web,

    /// <summary>SMTP, IMAP, POP3, and related mail submit/retrieve ports.</summary>
    Mail,

    /// <summary>FTP and similar file-transfer ports.</summary>
    FileTransfer,

    /// <summary>SSH, RDP, and other remote-access ports.</summary>
    RemoteAccess,

    /// <summary>LDAP and directory-service ports.</summary>
    Directory,

    /// <summary>DNS and related name-service ports.</summary>
    NameService,

    /// <summary>NTP and time-sync ports.</summary>
    Time,

    /// <summary>Relational and document database ports.</summary>
    Database,

    /// <summary>Message-broker and queue ports (for example AMQP).</summary>
    Messaging,

    /// <summary>Cache ports such as Redis and Memcached.</summary>
    Cache,

    /// <summary>Search ports (for example Elasticsearch).</summary>
    Search,

    /// <summary>Security or scanning ports (for example ClamAV).</summary>
    Security
}