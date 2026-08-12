using System.Diagnostics;
using System.Reflection;
using Lyo.Common.Enums;

namespace Lyo.Common.Records;

/// <summary>Well-known TCP/UDP port metadata with lookup by number, name, alias, or <see cref="PortCategory" />.</summary>
/// <remarks>
/// Curated common ports used by Lyo packages and typical infrastructure — not a full IANA registry.
/// Implicitly converts to <see cref="int" /> for options defaults (e.g. <c>Port = PortInfo.Ssh</c>).
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record PortInfo(int Port, string Name, string Description, PortCategory Category, string[] Aliases)
{
    /// <summary>Unknown or unregistered port.</summary>
    public static readonly PortInfo Unknown = new(0, "Unknown", "Unknown or unregistered port", PortCategory.Unknown, ["unknown", "unspecified"]);

    /// <summary>FTP control (21/tcp).</summary>
    public static readonly PortInfo Ftp = new(21, "FTP", "File Transfer Protocol control", PortCategory.FileTransfer, ["ftp"]);

    /// <summary>SSH / SFTP (22/tcp).</summary>
    public static readonly PortInfo Ssh = new(22, "SSH", "Secure Shell; also used by SFTP", PortCategory.RemoteAccess, ["ssh", "sftp"]);

    /// <summary>SMTP (25/tcp).</summary>
    public static readonly PortInfo Smtp = new(25, "SMTP", "Simple Mail Transfer Protocol", PortCategory.Mail, ["smtp", "mail"]);

    /// <summary>DNS (53/tcp+udp).</summary>
    public static readonly PortInfo Dns = new(53, "DNS", "Domain Name System", PortCategory.NameService, ["dns"]);

    /// <summary>HTTP (80/tcp).</summary>
    public static readonly PortInfo Http = new(80, "HTTP", "Hypertext Transfer Protocol", PortCategory.Web, ["http", "www"]);

    /// <summary>POP3 (110/tcp).</summary>
    public static readonly PortInfo Pop3 = new(110, "POP3", "Post Office Protocol v3", PortCategory.Mail, ["pop3", "pop"]);

    /// <summary>NTP (123/udp).</summary>
    public static readonly PortInfo Ntp = new(123, "NTP", "Network Time Protocol", PortCategory.Time, ["ntp"]);

    /// <summary>IMAP (143/tcp).</summary>
    public static readonly PortInfo Imap = new(143, "IMAP", "Internet Message Access Protocol", PortCategory.Mail, ["imap"]);

    /// <summary>LDAP (389/tcp).</summary>
    public static readonly PortInfo Ldap = new(389, "LDAP", "Lightweight Directory Access Protocol", PortCategory.Directory, ["ldap"]);

    /// <summary>HTTPS (443/tcp).</summary>
    public static readonly PortInfo Https = new(443, "HTTPS", "HTTP over TLS", PortCategory.Web, ["https", "ssl"]);

    /// <summary>SMTPS (465/tcp).</summary>
    public static readonly PortInfo Smtps = new(465, "SMTPS", "SMTP over TLS (implicit)", PortCategory.Mail, ["smtps"]);

    /// <summary>SMTP submission (587/tcp).</summary>
    public static readonly PortInfo SmtpSubmission = new(
        587, "SMTP Submission", "Message submission (STARTTLS)", PortCategory.Mail, ["submission", "smtp-submission"]);

    /// <summary>LDAPS (636/tcp).</summary>
    public static readonly PortInfo Ldaps = new(636, "LDAPS", "LDAP over TLS", PortCategory.Directory, ["ldaps"]);

    /// <summary>IMAPS (993/tcp).</summary>
    public static readonly PortInfo Imaps = new(993, "IMAPS", "IMAP over TLS", PortCategory.Mail, ["imaps"]);

    /// <summary>POP3S (995/tcp).</summary>
    public static readonly PortInfo Pop3s = new(995, "POP3S", "POP3 over TLS", PortCategory.Mail, ["pop3s"]);

    /// <summary>Microsoft SQL Server (1433/tcp).</summary>
    public static readonly PortInfo SqlServer = new(1433, "SQL Server", "Microsoft SQL Server", PortCategory.Database, ["mssql", "sqlserver", "sql-server"]);

    /// <summary>MySQL / MariaDB (3306/tcp).</summary>
    public static readonly PortInfo MySql = new(3306, "MySQL", "MySQL / MariaDB", PortCategory.Database, ["mysql", "mariadb"]);

    /// <summary>ClamAV clamd (3310/tcp).</summary>
    public static readonly PortInfo ClamAv = new(3310, "ClamAV", "ClamAV clamd INSTREAM / TCP", PortCategory.Security, ["clamav", "clamd"]);

    /// <summary>RDP (3389/tcp).</summary>
    public static readonly PortInfo Rdp = new(3389, "RDP", "Remote Desktop Protocol", PortCategory.RemoteAccess, ["rdp", "ms-wbt-server"]);

    /// <summary>PostgreSQL (5432/tcp).</summary>
    public static readonly PortInfo Postgres = new(5432, "PostgreSQL", "PostgreSQL database", PortCategory.Database, ["postgres", "postgresql", "pgsql"]);

    /// <summary>AMQP (5672/tcp).</summary>
    public static readonly PortInfo Amqp = new(5672, "AMQP", "Advanced Message Queuing Protocol (RabbitMQ)", PortCategory.Messaging, ["amqp", "rabbitmq"]);

    /// <summary>Redis (6379/tcp).</summary>
    public static readonly PortInfo Redis = new(6379, "Redis", "Redis key-value store", PortCategory.Cache, ["redis"]);

    /// <summary>HTTP alternate (8080/tcp).</summary>
    public static readonly PortInfo HttpAlt = new(8080, "HTTP Alt", "Alternate HTTP (often proxies / app servers)", PortCategory.Web, ["http-alt", "http-alt-8080"]);

    /// <summary>HTTPS alternate (8443/tcp).</summary>
    public static readonly PortInfo HttpsAlt = new(8443, "HTTPS Alt", "Alternate HTTPS", PortCategory.Web, ["https-alt", "https-alt-8443"]);

    /// <summary>Elasticsearch (9200/tcp).</summary>
    public static readonly PortInfo Elasticsearch = new(9200, "Elasticsearch", "Elasticsearch HTTP API", PortCategory.Search, ["elasticsearch", "elastic"]);

    /// <summary>Memcached (11211/tcp+udp).</summary>
    public static readonly PortInfo Memcached = new(11211, "Memcached", "Memcached cache", PortCategory.Cache, ["memcached", "memcache"]);

    /// <summary>RabbitMQ management UI / API (15672/tcp).</summary>
    public static readonly PortInfo RabbitMqManagement = new(
        15672, "RabbitMQ Management", "RabbitMQ management plugin HTTP API", PortCategory.Messaging, ["rabbitmq-management", "amqp-management"]);

    /// <summary>MongoDB (27017/tcp).</summary>
    public static readonly PortInfo MongoDb = new(27017, "MongoDB", "MongoDB database", PortCategory.Database, ["mongodb", "mongo"]);

    private static readonly Dictionary<int, PortInfo> ByPort = new();
    private static readonly Dictionary<string, PortInfo> ByAlias = new(StringComparer.OrdinalIgnoreCase);
    private static readonly List<PortInfo> AllPorts = [];

    /// <summary>All registered ports except <see cref="Unknown" />.</summary>
    public static IReadOnlyList<PortInfo> All => AllPorts;

    static PortInfo()
    {
        var fields = typeof(PortInfo).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(PortInfo))
            .Select(f => (PortInfo)f.GetValue(null)!)
            .ToList();

        foreach (var info in fields) {
            if (info == Unknown)
                continue;

            AllPorts.Add(info);
            ByPort[info.Port] = info;
            RegisterAlias(info.Name, info);
            foreach (var alias in info.Aliases) {
                if (!string.IsNullOrWhiteSpace(alias))
                    RegisterAlias(alias, info);
            }
        }
    }

    /// <summary>Finds a port by number, or <see cref="Unknown" /> when not registered.</summary>
    public static PortInfo FromPort(int port) => ByPort.TryGetValue(port, out var info) ? info : Unknown;

    /// <summary>Tries to find a registered port by number.</summary>
    public static bool TryFromPort(int port, out PortInfo info)
    {
        if (ByPort.TryGetValue(port, out var found)) {
            info = found;
            return true;
        }

        info = Unknown;
        return false;
    }

    /// <summary>Finds a port by display name or alias (e.g. <c>ssh</c>, <c>postgres</c>, <c>SMTP Submission</c>).</summary>
    public static PortInfo FromName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Unknown;

        return ByAlias.TryGetValue(Normalize(name), out var info) ? info : Unknown;
    }

    /// <summary>Tries to find a port by display name or alias.</summary>
    public static bool TryFromName(string? name, out PortInfo info)
    {
        if (string.IsNullOrWhiteSpace(name)) {
            info = Unknown;
            return false;
        }

        if (ByAlias.TryGetValue(Normalize(name), out var found)) {
            info = found;
            return true;
        }

        info = Unknown;
        return false;
    }

    /// <summary>Gets registered ports in the given <paramref name="category" />.</summary>
    public static IEnumerable<PortInfo> ByCategory(PortCategory category) => AllPorts.Where(p => p.Category == category);

    /// <summary>Implicit conversion to the numeric port for options defaults and APIs that take <see cref="int" />.</summary>
    public static implicit operator int(PortInfo info) => info.Port;

    /// <inheritdoc />
    public override string ToString() => Port == 0 ? Name : $"{Name} ({Port})";

    private static void RegisterAlias(string alias, PortInfo info)
    {
        var key = Normalize(alias);
        if (!ByAlias.ContainsKey(key))
            ByAlias[key] = info;
    }

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();
}
