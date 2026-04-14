using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.FileMetadataStore.Postgres.Tests;

public class FileMetadataStorePostgresExtensionsTests
{
    [Fact]
    public void AddFileMetadataStoreDbContext_WithNullServices_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => Extensions.AddFileMetadataStoreDbContext(null!, "Host=localhost"));
        Assert.Equal("services", ex.ParamName);
    }

    [Fact]
    public void AddFileMetadataStoreDbContext_WithNullConnectionString_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        var ex = Assert.Throws<ArgumentNullException>(() => services.AddFileMetadataStoreDbContext((string)null!));
        Assert.Equal("connectionString", ex.ParamName);
    }

    [Fact]
    public void AddFileMetadataStoreDbContext_WithEmptyConnectionString_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var ex = Assert.Throws<ArgumentException>(() => services.AddFileMetadataStoreDbContext(""));
        Assert.Equal("connectionString", ex.ParamName);
    }

    [Fact]
    public void AddFileMetadataStoreDbContext_WithWhitespaceConnectionString_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var ex = Assert.Throws<ArgumentException>(() => services.AddFileMetadataStoreDbContext("   "));
        Assert.Equal("connectionString", ex.ParamName);
    }

    [Fact]
    public void AddFileMetadataStoreDbContext_WithNullConfigure_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        var ex = Assert.Throws<ArgumentNullException>(() => services.AddFileMetadataStoreDbContext((Action<DbContextOptionsBuilder>)null!));
        Assert.Equal("configure", ex.ParamName);
    }

    [Fact]
    public void AddFileMetadataStoreDbContextFactory_WithNullServices_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => Extensions.AddFileMetadataStoreDbContextFactory(null!, _ => { }));
        Assert.Equal("services", ex.ParamName);
    }

    [Fact]
    public void AddFileMetadataStoreDbContextFactory_WithNullConfigure_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        var ex = Assert.Throws<ArgumentNullException>(() => services.AddFileMetadataStoreDbContextFactory((Action<PostgresFileMetadataStoreOptions>)null!));
        Assert.Equal("configure", ex.ParamName);
    }

    [Fact]
    public void AddFileMetadataStoreDbContextFactory_WithNullConfiguration_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        var ex = Assert.Throws<ArgumentNullException>(() => services.AddFileMetadataStoreDbContextFactoryFromConfiguration(null!));
        Assert.Equal("configuration", ex.ParamName);
    }

    [Fact]
    public void AddFileMetadataStoreDbContextFactory_WithEmptyConfigSectionName_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        var ex = Assert.Throws<ArgumentException>(() => services.AddFileMetadataStoreDbContextFactoryFromConfiguration(config, ""));
        Assert.Equal("configSectionName", ex.ParamName);
    }

    [Fact]
    public void AddFileMetadataStoreDbContextFactory_WithNullOptions_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        var ex = Assert.Throws<ArgumentNullException>(() => services.AddFileMetadataStoreDbContextFactory((PostgresFileMetadataStoreOptions)null!));
        Assert.Equal("options", ex.ParamName);
    }

    [Fact]
    public void AddFileMetadataStoreDbContextFactory_WithEmptyConnectionStringInOptions_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var options = new PostgresFileMetadataStoreOptions { ConnectionString = "" };
        var ex = Assert.Throws<ArgumentException>(() => services.AddFileMetadataStoreDbContextFactory(options));
        Assert.Equal("ConnectionString", ex.ParamName);
    }

    [Fact]
    public void AddPostgresFileMetadataStore_WithNullServices_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => Extensions.AddPostgresFileMetadataStore(null!));
        Assert.Equal("services", ex.ParamName);
    }

    [Fact]
    public void AddPostgresFileMetadataStore_WithConnectionString_WithNullServices_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => Extensions.AddPostgresFileMetadataStore(null!, "Host=localhost"));
        Assert.Equal("services", ex.ParamName);
    }

    [Fact]
    public void AddPostgresFileMetadataStore_WithConnectionString_WithNullConnectionString_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        var ex = Assert.Throws<ArgumentNullException>(() => services.AddPostgresFileMetadataStore((string)null!));
        Assert.Equal("connectionString", ex.ParamName);
    }

    [Fact]
    public void AddPostgresFileMetadataStore_WithConnectionString_WithEmptyConnectionString_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var ex = Assert.Throws<ArgumentException>(() => services.AddPostgresFileMetadataStore(""));
        Assert.Equal("connectionString", ex.ParamName);
    }

    [Fact]
    public void AddPostgresFileMetadataStoreKeyed_WithNullServices_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => Extensions.AddPostgresFileMetadataStoreKeyed(null!, "key", "Host=localhost"));
        Assert.Equal("services", ex.ParamName);
    }

    [Fact]
    public void AddPostgresFileMetadataStoreKeyed_WithEmptyKeyName_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var ex = Assert.Throws<ArgumentException>(() => services.AddPostgresFileMetadataStoreKeyed("", "Host=localhost"));
        Assert.Equal("keyName", ex.ParamName);
    }

    [Fact]
    public void AddPostgresFileMetadataStoreKeyed_WithNullKeyName_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        var ex = Assert.Throws<ArgumentNullException>(() => services.AddPostgresFileMetadataStoreKeyed(null!, "Host=localhost"));
        Assert.Equal("keyName", ex.ParamName);
    }

    [Fact]
    public void AddPostgresFileMetadataStoreKeyed_Builder_WithNullServices_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => Extensions.AddPostgresFileMetadataStoreKeyed(null!, "key"));
        Assert.Equal("services", ex.ParamName);
    }

    [Fact]
    public void AddPostgresFileMetadataStoreKeyed_Builder_WithEmptyKeyName_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var ex = Assert.Throws<ArgumentException>(() => services.AddPostgresFileMetadataStoreKeyed(""));
        Assert.Equal("keyName", ex.ParamName);
    }
}