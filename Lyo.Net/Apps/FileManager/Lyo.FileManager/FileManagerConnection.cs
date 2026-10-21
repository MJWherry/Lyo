using Lyo.Api.FileStorage.Models;

namespace Lyo.FileManager;

/// <summary>One open server and key store. Its listing stays put while another pair is selected.</summary>
public sealed class FileManagerConnection
{
    private readonly string? _keyStoreName;

    /// <summary>Saved server this connection opened.</summary>
    public FileManagerServer Server { get; }

    /// <summary><see cref="FileManagerServer.Id" />.</summary>
    public Guid ServerId => Server.Id;

    /// <summary>Key store this connection opened with. Null is no key store. A remote server is always null.</summary>
    public Guid? KeyStoreId { get; }

    /// <summary>Tab label. A remote server is its name. An in-process server is the key store name, then the server name.</summary>
    public string Title => Server.Kind == FileManagerServerKind.Remote ? Server.Name : (_keyStoreName ?? "(None)") + " + " + Server.Name;

    internal IFileManagerApi Api { get; set; }

    internal string? Prefix { get; set; }

    internal List<FileManagerEntry> Pending { get; } = [];

    /// <summary>Children of <see cref="Prefix" />.</summary>
    public IReadOnlyList<FileManagerEntry> Entries { get; internal set; } = [];

    /// <summary>True when the last list was capped.</summary>
    public bool Truncated { get; internal set; }

    /// <summary>Last error on this connection. Null after a successful call.</summary>
    public string? Error { get; internal set; }

    /// <summary>Feature flags from the last successful connect.</summary>
    public FileStorageCapabilitiesResponse Capabilities { get; internal set; } = FileStorageCapabilitiesResponse.None;

    /// <summary>Rows from the last query on this connection.</summary>
    public IReadOnlyList<FileManagerQueryRow> QueryRows { get; internal set; } = [];

    internal FileManagerConnection(FileManagerServer server, IFileManagerApi api, Guid? keyStoreId, string? keyStoreName)
    {
        Server = server;
        Api = api;
        KeyStoreId = keyStoreId;
        _keyStoreName = string.IsNullOrWhiteSpace(keyStoreName) ? null : keyStoreName.Trim();
    }
}