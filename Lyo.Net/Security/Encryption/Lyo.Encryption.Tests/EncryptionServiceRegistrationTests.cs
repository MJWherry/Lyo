using Lyo.Encryption.AesCcm;
using Lyo.Encryption.Extensions;
using Lyo.Encryption.TwoKey;
using Lyo.Encryption.XChaCha20Poly1305;
using Lyo.KeyStore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Encryption.Tests;

public class EncryptionServiceRegistrationTests
{
    [Fact]
    public void MultipleAddonConcretes_CanCoexist()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IKeyStore>(_ => new LocalKeyStore());
        services.AddAesCcmEncryption();
        services.AddXChaCha20Poly1305Encryption();
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<AesCcmEncryptionService>());
        Assert.NotNull(provider.GetRequiredService<XChaCha20Poly1305EncryptionService>());
    }

    [Fact]
    public void UnkeyedInterface_IsNullUntilDefaultMapper()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IKeyStore>(_ => new LocalKeyStore());
        services.AddAesCcmEncryption();
        var provider = services.BuildServiceProvider();
        Assert.Null(provider.GetService<IEncryptionService>());
    }

    [Fact]
    public void AddDefaultEncryptionService_ResolvesSameInstanceAsConcrete()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IKeyStore>(_ => new LocalKeyStore());
        services.AddAesCcmEncryption();
        services.AddDefaultEncryptionService<AesCcmEncryptionService>();
        var provider = services.BuildServiceProvider();
        var concrete = provider.GetRequiredService<AesCcmEncryptionService>();
        var iface = provider.GetRequiredService<IEncryptionService>();
        Assert.Same(concrete, iface);
    }

    [Fact]
    public void AddAesCcmEncryptionServiceKeyed_RegistersKeyedInterface()
    {
        const string keyName = "primary";
        const string keyStoreName = "ks";
        var services = new ServiceCollection();
        services.AddKeyedLocalKeyStore(keyStoreName, _ => { });
        services.AddAesCcmEncryptionServiceKeyed(keyName, keyStoreName);
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetKeyedService<IEncryptionService>(keyName));
        Assert.NotNull(provider.GetKeyedService<ITwoKeyEncryptionService>(keyName));
    }
}