namespace Lyo.FileStorage.Blob.Tests;

/// <summary>Pure-data coverage for the BlobFileStorageOptions surface (CPK detection, SectionName invariants).</summary>
public sealed class BlobFileStorageOptionsTests
{
    [Fact]
    public void UsesCustomerProvidedKey_TrueWhenKeyPresent()
    {
        var opts = new BlobFileStorageOptions { ConnectionString = "x", ContainerName = "c", CustomerProvidedKeyBase64 = Convert.ToBase64String(new byte[32]) };
        Assert.True(opts.UsesCustomerProvidedKey);
    }

    [Fact]
    public void UsesCustomerProvidedKey_FalseWhenNullOrWhitespace()
    {
        var opts = new BlobFileStorageOptions { ConnectionString = "x", ContainerName = "c", CustomerProvidedKeyBase64 = "   " };
        Assert.False(opts.UsesCustomerProvidedKey);
    }

    [Fact]
    public void SectionName_IsStable()
    {
        // SectionName is part of the public binding contract for appsettings.json.
        Assert.Equal("BlobFileStorage", BlobFileStorageOptions.SectionName);
        Assert.Equal("AzureFileStorageOptions", BlobFileStorageOptions.LegacyAzureConfigurationSectionName);
    }

    [Fact]
    public void ResolveCustomerProvidedKey_NullOrWhitespace_ReturnsNull()
    {
        Assert.Null(new BlobFileStorageOptions { ConnectionString = "x", ContainerName = "c" }.ResolveCustomerProvidedKey());
        Assert.Null(new BlobFileStorageOptions { ConnectionString = "x", ContainerName = "c", CustomerProvidedKeyBase64 = "   " }.ResolveCustomerProvidedKey());
    }

    [Fact]
    public void ResolveCustomerProvidedKey_ValidBase64_ReturnsKey()
    {
        var keyBytes = new byte[32];
        for (var i = 0; i < keyBytes.Length; i++)
            keyBytes[i] = (byte)i;

        var opts = new BlobFileStorageOptions { ConnectionString = "x", ContainerName = "c", CustomerProvidedKeyBase64 = Convert.ToBase64String(keyBytes) };
        var cpk = opts.ResolveCustomerProvidedKey();
        Assert.NotNull(cpk);
        Assert.Equal("Aes256", cpk.Value.EncryptionAlgorithm.ToString());
    }

    [Fact]
    public void ResolveCustomerProvidedKey_InvalidBase64_Throws()
    {
        var opts = new BlobFileStorageOptions { ConnectionString = "x", ContainerName = "c", CustomerProvidedKeyBase64 = "!not base64!" };
        Assert.Throws<FormatException>(() => opts.ResolveCustomerProvidedKey());
    }

    [Fact]
    public void EnableMetrics_DefaultsToBaseTrue()
    {
        // Base class sets EnableMetrics = true. Confirm Blob doesn't override to false (p5-options-defaults invariant).
        var opts = new BlobFileStorageOptions { ConnectionString = "x", ContainerName = "c" };
        Assert.True(opts.EnableMetrics);
    }
}