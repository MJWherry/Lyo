using Lyo.Common.Core.Identifiers;

namespace Lyo.FileManager.Tests;

public class FileManagerKeyStoreTests
{
    [Fact]
    public void Load_PassphraseWithoutKeyStoreId_DoesNotCreateACatalogEntry()
    {
        var path = Path.Combine(Path.GetTempPath(), "lyo-fm-keys-" + LyoGuid.CreateV7().ToString("N") + ".json");
        try {
            File.WriteAllText(
                path,
                """
                {
                  "servers": [
                    { "name": "photos", "passphrase": "secret", "keys": [{ "id": "k", "version": "1", "secret": "s" }] },
                    {
                      "name": "cloud",
                      "keyStoreKind": 1,
                      "awsRegion": "eu-west-1",
                      "awsSecretPrefix": "lyo/kek",
                      "awsProfile": "dev",
                      "passphrase": "ignored",
                      "keys": [{ "id": "nope", "secret": "x" }]
                    }
                  ]
                }
                """);
            var loaded = FileManagerSettingsStore.LoadDocument(path);
            Assert.Empty(loaded.KeyStores);
            var photos = Assert.Single(loaded.Servers, server => server.Name == "photos");
            Assert.Null(photos.KeyStoreId);
            var cloud = Assert.Single(loaded.Servers, server => server.Name == "cloud");
            Assert.Null(cloud.KeyStoreId);
        }
        finally {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void Save_KeyStores_RoundTrips()
    {
        var path = Path.Combine(Path.GetTempPath(), "lyo-fm-keys-" + LyoGuid.CreateV7().ToString("N") + ".json");
        var localId = LyoGuid.CreateV7();
        var awsId = LyoGuid.CreateV7();
        var stores = new List<FileManagerKeyStore> {
            new() { Id = localId, Name = "local-keys", Kind = FileManagerKeyStoreKind.Local, Passphrase = "a" },
            new() { Id = awsId, Name = "aws-keys", Kind = FileManagerKeyStoreKind.Aws, AwsRegion = "us-east-1" }
        };
        try {
            FileManagerSettingsStore.Save(path, [], FileManagerLayout.CreateDefault(), keyStores: stores);
            var loaded = FileManagerSettingsStore.LoadDocument(path);
            Assert.Equal(2, loaded.KeyStores.Count);
            Assert.Equal(localId, loaded.KeyStores[0].Id);
            Assert.Equal("local-keys", loaded.KeyStores[0].Name);
            Assert.Equal("a", loaded.KeyStores[0].Passphrase);
            Assert.Equal(awsId, loaded.KeyStores[1].Id);
            Assert.Equal("aws-keys", loaded.KeyStores[1].Name);
            Assert.Equal(FileManagerKeyStoreKind.Aws, loaded.KeyStores[1].Kind);
            Assert.Equal("us-east-1", loaded.KeyStores[1].AwsRegion);
        }
        finally {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task Open_SelectedKeyStore_ListsThatKeyId()
    {
        var root = Path.Combine(Path.GetTempPath(), "lyo-fm-" + LyoGuid.CreateV7().ToString("N"));
        Directory.CreateDirectory(root);
        try {
            var server = new FileManagerServer {
                Kind = FileManagerServerKind.Local,
                Name = "local",
                RootDirectory = root,
                MetadataKind = FileManagerMetadataKind.Sqlite,
                Passphrase = "other-secret"
            };
            var selected = new FileManagerKeyStore {
                Name = "picked-store",
                Kind = FileManagerKeyStoreKind.Local,
                Keys = [new() { Id = "picked", Version = "1", Secret = "picked-secret" }]
            };
            using var host = FileStorageLocalHost.Open(server, selected);
            var ids = await host.ListKeyIdsAsync(TestCt);
            Assert.Equal(["picked"], ids);
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
    public void ReadLocal_FileKeyStore_KeepsRawKeyAndSalt()
    {
        var store = Assert.Single(FileManagerKeyStores.ReadLocal(
            """
            {
              "keys": [
                {
                  "keyId": "comic-images",
                  "version": "v1",
                  "current": true,
                  "key": "AQIDBA==",
                  "metadata": { "additionalData": { "Pbkdf2Salt": "BAUGBw==" } }
                }
              ]
            }
            """,
            "comic"));
        Assert.Equal("comic", store.Name);
        Assert.Equal(FileManagerKeyStoreKind.Local, store.Kind);
        var key = Assert.Single(store.Keys);
        Assert.Equal("comic-images", key.Id);
        Assert.Equal("v1", key.Version);
        Assert.Equal("AQIDBA==", key.RawKey);
        Assert.True(key.Current);
        Assert.Equal("BAUGBw==", key.Metadata!.AdditionalData!["Pbkdf2Salt"]);
        var storeObject = Assert.Throws<InvalidOperationException>(() => FileManagerKeyStores.ReadLocal("""{ "name": "desk", "passphrase": "secret" }""", "fallback"));
        Assert.Equal("The file is not a local key store.", storeObject.Message);
        var appSettings = Assert.Throws<InvalidOperationException>(() =>
            FileManagerKeyStores.ReadLocal("""{ "ComicKeyStore": { "FilePath": "~/Comic/keystore.json" } }""", "fallback"));
        Assert.Equal("The file is not a local key store.", appSettings.Message);
    }

    [Fact]
    public async Task Open_RawKey_InstallsThoseBytes()
    {
        var root = Path.Combine(Path.GetTempPath(), "lyo-fm-" + LyoGuid.CreateV7().ToString("N"));
        Directory.CreateDirectory(root);
        try {
            var server = new FileManagerServer {
                Kind = FileManagerServerKind.Local,
                Name = "local",
                RootDirectory = root,
                MetadataKind = FileManagerMetadataKind.Sqlite,
                Passphrase = "other-secret"
            };
            var selected = Assert.Single(FileManagerKeyStores.ReadLocal(
                """
                {
                  "keys": [
                    {
                      "keyId": "comic-images",
                      "version": "v1",
                      "current": true,
                      "key": "AQIDBA==",
                      "metadata": { "additionalData": { "Pbkdf2Salt": "BAUGBw==" } }
                    }
                  ]
                }
                """,
                "comic"));
            using var host = FileStorageLocalHost.Open(server, selected);
            var keys = host.KeyStore;
            Assert.NotNull(keys);
            Assert.Equal(Convert.FromBase64String("AQIDBA=="), keys.GetKey("comic-images", "v1"));
            Assert.Equal(Convert.FromBase64String("BAUGBw=="), keys.GetSaltForVersion("comic-images", "v1"));
        }
        finally {
            var database = FileManagerPaths.DefaultSqliteFile(root);
            if (File.Exists(database))
                File.Delete(database);
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }
}
