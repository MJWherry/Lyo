using Lyo.Common.Core.Identifiers;
using Lyo.FileMetadataStore.Postgres;
using Lyo.FileMetadataStore.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.FileManager.Tests;

public class FileStorageLocalHostTests
{
    [Fact]
    public async Task OpenLocal_Sqlite_SaveThenList_ReadsThatRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "lyo-fm-" + LyoGuid.CreateV7().ToString("N"));
        var source = Path.Combine(Path.GetTempPath(), "lyo-fm-src-" + LyoGuid.CreateV7().ToString("N") + ".txt");
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(source, "hello", TestCt);
        try {
            var server = new FileManagerServer { Kind = FileManagerServerKind.Local, Name = "local", RootDirectory = root, MetadataKind = FileManagerMetadataKind.Sqlite };
            using var host = FileStorageLocalHost.Open(server);
            await host.UploadAsync(source, "docs", new(), TestCt);
            var listed = await host.ListAsync("docs", TestCt);
            Assert.Contains(listed.Entries, entry => entry.Name == Path.GetFileName(source) && entry.FileId != null);
        }
        finally {
            if (File.Exists(source))
                File.Delete(source);
            var database = FileManagerPaths.DefaultSqliteFile(root);
            if (File.Exists(database))
                File.Delete(database);
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task Open_WithoutPassphrase_ReportsLocalHostCapabilities()
    {
        var root = Path.Combine(Path.GetTempPath(), "lyo-fm-" + LyoGuid.CreateV7().ToString("N"));
        Directory.CreateDirectory(root);
        try {
            var server = new FileManagerServer { Kind = FileManagerServerKind.Local, Name = "local", RootDirectory = root, MetadataKind = FileManagerMetadataKind.Sqlite };
            using var host = FileStorageLocalHost.Open(server);
            var caps = await host.GetCapabilitiesAsync(TestCt);
            Assert.True(caps.PresignedRead);
            Assert.False(caps.DirectUpload);
            Assert.True(caps.Multipart);
            Assert.False(caps.PresignedMultipartParts);
            Assert.True(caps.Copy);
            Assert.True(caps.Move);
            Assert.True(caps.Physical);
            Assert.False(caps.Encryption);
            Assert.True(caps.Archive);
            Assert.True(caps.AccessLinks);
        }
        finally {
            var database = FileManagerPaths.DefaultSqliteFile(root);
            if (File.Exists(database))
                File.Delete(database);
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task Open_WithPassphrase_ReportsEncryption()
    {
        var root = Path.Combine(Path.GetTempPath(), "lyo-fm-" + LyoGuid.CreateV7().ToString("N"));
        Directory.CreateDirectory(root);
        try {
            var server = new FileManagerServer {
                Kind = FileManagerServerKind.Local,
                Name = "local",
                RootDirectory = root,
                MetadataKind = FileManagerMetadataKind.Sqlite,
                Passphrase = "local-passphrase"
            };
            using var host = FileStorageLocalHost.Open(server);
            var caps = await host.GetCapabilitiesAsync(TestCt);
            Assert.True(caps.Encryption);
            Assert.False(caps.PresignedMultipartParts);
        }
        finally {
            var database = FileManagerPaths.DefaultSqliteFile(root);
            if (File.Exists(database))
                File.Delete(database);
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task Upload_RootWithGZip_ListsTheFileAsGZip()
    {
        var root = Path.Combine(Path.GetTempPath(), "lyo-fm-" + LyoGuid.CreateV7().ToString("N"));
        var source = Path.Combine(Path.GetTempPath(), "lyo-fm-src-" + LyoGuid.CreateV7().ToString("N") + ".txt");
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(source, "hello root", TestCt);
        try {
            var server = new FileManagerServer { Kind = FileManagerServerKind.Local, Name = "local", RootDirectory = root, MetadataKind = FileManagerMetadataKind.Sqlite };
            using var host = FileStorageLocalHost.Open(server);
            await host.UploadAsync(source, null, new() { Compress = true, CompressionAlgorithm = "GZip" }, TestCt);
            var listed = await host.ListAsync(null, TestCt);
            var file = Assert.Single(listed.Entries, entry => entry.Name == Path.GetFileName(source));
            Assert.False(file.IsDirectory);
            Assert.True(file.IsCompressed);
            Assert.Equal("GZip", file.Compression);
            Assert.False(file.IsEncrypted);
        }
        finally {
            if (File.Exists(source))
                File.Delete(source);
            var database = FileManagerPaths.DefaultSqliteFile(root);
            if (File.Exists(database))
                File.Delete(database);
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task Upload_RootWithAesGcm_ListsTheFileAsEncrypted()
    {
        var root = Path.Combine(Path.GetTempPath(), "lyo-fm-" + LyoGuid.CreateV7().ToString("N"));
        var source = Path.Combine(Path.GetTempPath(), "lyo-fm-src-" + LyoGuid.CreateV7().ToString("N") + ".txt");
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(source, "secret root", TestCt);
        try {
            var server = new FileManagerServer {
                Kind = FileManagerServerKind.Local,
                Name = "local",
                RootDirectory = root,
                MetadataKind = FileManagerMetadataKind.Sqlite,
                Passphrase = "local-passphrase"
            };
            using var host = FileStorageLocalHost.Open(server);
            await host.UploadAsync(source, null, new() { Encrypt = true, EncryptionAlgorithm = "AesGcm", KeyId = FileManagerLimits.LocalKeyId }, TestCt);
            var listed = await host.ListAsync(null, TestCt);
            var file = Assert.Single(listed.Entries, entry => entry.Name == Path.GetFileName(source));
            Assert.True(file.IsEncrypted);
            Assert.Equal("AesGcm", file.Encryption);
            Assert.False(file.IsCompressed);
            var copy = Path.Combine(Path.GetTempPath(), "lyo-fm-out-" + LyoGuid.CreateV7().ToString("N") + ".txt");
            try {
                await host.DownloadAsync(file.FileId!.Value, copy, TestCt);
                Assert.Equal("secret root", await File.ReadAllTextAsync(copy, TestCt));
            }
            finally {
                if (File.Exists(copy))
                    File.Delete(copy);
            }
        }
        finally {
            if (File.Exists(source))
                File.Delete(source);
            var database = FileManagerPaths.DefaultSqliteFile(root);
            if (File.Exists(database))
                File.Delete(database);
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Fact]
    public void OpenLocal_PostgresConnectionString_SelectsThePostgresStore()
    {
        var services = FileStorageLocalHost.CreateServices(
            new() {
                Kind = FileManagerServerKind.Local,
                RootDirectory = "/tmp/lyo-files",
                MetadataKind = FileManagerMetadataKind.Postgres,
                ConnectionString = "Host=localhost;Database=lyo_files"
            });
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(PostgresFileMetadataStore));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(SqliteFileMetadataStore));
    }
}
