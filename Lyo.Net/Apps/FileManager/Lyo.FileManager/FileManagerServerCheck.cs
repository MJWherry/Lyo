namespace Lyo.FileManager;

/// <summary>Checks a server before it is added. The dialog shows the message and stays open.</summary>
public static class FileManagerServerCheck
{
    /// <summary>Returns the first problem, or null when the server can be added.</summary>
    public static string? Validate(string? name, FileManagerServerKind kind, string? baseUrl, string? rootDirectory)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Name is required.";
        if (kind == FileManagerServerKind.Local && string.IsNullOrWhiteSpace(rootDirectory))
            return "File folder is required.";
        if (kind == FileManagerServerKind.Remote && string.IsNullOrWhiteSpace(baseUrl))
            return "Base URL is required.";
        return null;
    }

    /// <summary>Returns the first missing field for an S3, Azure Blob, FTP, or SFTP server, or null when those fields are present.</summary>
    public static string? ValidateStore(
        FileManagerServerKind kind,
        string? bucket,
        string? azureConnection,
        string? container,
        string? host,
        string? username,
        string? password,
        string? privateKey,
        string? fingerprints,
        string? catalog,
        bool postgres)
    {
        switch (kind) {
            case FileManagerServerKind.S3:
                if (string.IsNullOrWhiteSpace(bucket))
                    return "Bucket is required.";
                break;
            case FileManagerServerKind.AzureBlob:
                if (string.IsNullOrWhiteSpace(azureConnection))
                    return "Storage connection string is required.";
                if (string.IsNullOrWhiteSpace(container))
                    return "Container is required.";
                break;
            case FileManagerServerKind.Ftp:
                if (string.IsNullOrWhiteSpace(host))
                    return "Host is required.";
                if (string.IsNullOrWhiteSpace(username))
                    return "Username is required.";
                if (string.IsNullOrWhiteSpace(password))
                    return "Password is required.";
                break;
            case FileManagerServerKind.Sftp:
                if (string.IsNullOrWhiteSpace(host))
                    return "Host is required.";
                if (string.IsNullOrWhiteSpace(username))
                    return "Username is required.";
                if (string.IsNullOrWhiteSpace(password) && string.IsNullOrWhiteSpace(privateKey))
                    return "Password or a private key is required.";
                if (string.IsNullOrWhiteSpace(fingerprints))
                    return "Host key fingerprint is required.";
                break;
        }

        if (kind is FileManagerServerKind.S3 or FileManagerServerKind.AzureBlob or FileManagerServerKind.Ftp or FileManagerServerKind.Sftp
            && string.IsNullOrWhiteSpace(catalog))
            return postgres ? "Postgres connection string is required." : "Catalog database is required.";
        return null;
    }
}
