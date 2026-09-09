using System.Text;
using Lyo.Config;
using Lyo.Config.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Config.Postgres.Tests;

[Trait("Category", "Integration")]
public sealed class ConfigPostgresStoreTests
{
    private readonly ConfigPostgresFixture _fixture;

    public ConfigPostgresStoreTests(ConfigPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task SaveDefinition_CreatesRevision1()
    {
        var ct = TestContext.Current.CancellationToken;
        var store = Store();
        var definition = NewDefinition("rev1-key", typeof(string));
        definition.Description = "first";
        await store.SaveDefinitionAsync(definition, ct);
        Assert.NotEqual(Guid.Empty, definition.Id);
        var revs = await store.GetDefinitionRevisionsAsync(definition.Id, ct);
        var rev = Assert.Single(revs);
        Assert.Equal(1, rev.Revision);
        Assert.Equal(definition.Key, rev.Key);
        Assert.Equal("first", rev.Description);
    }

    [Fact]
    public async Task SecondSave_CreatesRevision2()
    {
        var ct = TestContext.Current.CancellationToken;
        var store = Store();
        var definition = NewDefinition("rev2-key", typeof(string));
        definition.Description = "one";
        await store.SaveDefinitionAsync(definition, ct);
        definition.Description = "two";
        await store.SaveDefinitionAsync(definition, ct);
        var revs = await store.GetDefinitionRevisionsAsync(definition.Id, ct);
        Assert.Equal([2, 1], revs.Select(r => r.Revision));
        Assert.Equal("two", revs[0].Description);
        Assert.Equal("one", revs[1].Description);
    }

    [Fact]
    public async Task Revert_AppendsNewRevision()
    {
        var ct = TestContext.Current.CancellationToken;
        var store = Store();
        var definition = NewDefinition("revert-key", typeof(string));
        definition.Description = "original";
        await store.SaveDefinitionAsync(definition, ct);
        definition.Description = "changed";
        await store.SaveDefinitionAsync(definition, ct);
        await store.RevertDefinitionToRevisionAsync(definition.Id, 1, ct);
        var loaded = await store.GetDefinitionByIdAsync(definition.Id, ct);
        Assert.Equal("original", loaded!.Description);
        var revs = await store.GetDefinitionRevisionsAsync(definition.Id, ct);
        Assert.Equal(3, revs.Count);
        Assert.Equal(3, revs[0].Revision);
        Assert.Equal("original", revs[0].Description);
    }

    [Fact]
    public async Task Encrypted_RoundTripStoresCiphertext_AndDecryptsOnRead()
    {
        var ct = TestContext.Current.CancellationToken;
        var store = Store();
        const string plaintext = "s3cret-config-value";
        var definition = NewDefinition("enc-key", typeof(string));
        definition.IsEncrypted = true;
        definition.DefaultValue = ConfigValue.From(plaintext);
        await store.SaveDefinitionAsync(definition, ct);
        await using var db = await CreateDbAsync(ct);
        var row = await db.ConfigDefinitions.AsNoTracking().SingleAsync(d => d.Id == definition.Id, ct);
        Assert.True(row.IsEncrypted);
        Assert.Null(row.DefaultValueJson);
        Assert.NotNull(row.EncryptedDefaultValue);
        Assert.NotEmpty(row.EncryptedDefaultValue!);
        Assert.NotEqual(plaintext, Encoding.UTF8.GetString(row.EncryptedDefaultValue!));
        var loaded = await store.GetDefinitionByIdAsync(definition.Id, ct);
        Assert.Equal(plaintext, loaded!.DefaultValue!.GetValue<string>());
        var revs = await store.GetDefinitionRevisionsAsync(definition.Id, ct);
        Assert.Equal(plaintext, Assert.Single(revs).DefaultValue!.GetValue<string>());
    }

    [Fact]
    public async Task TogglingEncrypted_RewritesExistingBindingRevisions()
    {
        var ct = TestContext.Current.CancellationToken;
        var store = Store();
        var definition = NewDefinition("toggle-enc", typeof(string));
        definition.DefaultValue = ConfigValue.From("default-plain");
        await store.SaveDefinitionAsync(definition, ct);
        var binding = new ConfigBindingRecord {
            DefinitionId = definition.Id,
            Key = definition.Key,
            SubjectEntityType = definition.SubjectEntityType,
            SubjectEntityId = "gateway:local",
            Value = ConfigValue.From("bound-plain")
        };
        await store.SaveBindingAsync(binding, null, ct);

        definition.IsEncrypted = true;
        await store.SaveDefinitionAsync(definition, ct);

        await using (var db = await CreateDbAsync(ct)) {
            var defRow = await db.ConfigDefinitions.AsNoTracking().SingleAsync(d => d.Id == definition.Id, ct);
            Assert.True(defRow.IsEncrypted);
            Assert.Null(defRow.DefaultValueJson);
            Assert.NotNull(defRow.EncryptedDefaultValue);
            Assert.NotEmpty(defRow.EncryptedDefaultValue!);
            var rev = await db.ConfigBindingRevisions.AsNoTracking().SingleAsync(r => r.BindingId == binding.Id, ct);
            Assert.NotNull(rev.EncryptedValue);
            Assert.NotEmpty(rev.EncryptedValue!);
            Assert.NotEqual("bound-plain", Encoding.UTF8.GetString(rev.EncryptedValue!));
        }

        var loaded = await store.GetBindingByIdAsync(binding.Id, null, ct);
        Assert.Equal("bound-plain", loaded!.Value.GetValue<string>());

        definition.IsEncrypted = false;
        await store.SaveDefinitionAsync(definition, ct);

        await using (var db = await CreateDbAsync(ct)) {
            var defRow = await db.ConfigDefinitions.AsNoTracking().SingleAsync(d => d.Id == definition.Id, ct);
            Assert.False(defRow.IsEncrypted);
            Assert.Null(defRow.EncryptedDefaultValue);
            var rev = await db.ConfigBindingRevisions.AsNoTracking().SingleAsync(r => r.BindingId == binding.Id, ct);
            Assert.Null(rev.EncryptedValue);
            Assert.Contains("bound-plain", rev.ValueJson, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void MaskValue_ReturnsPlaceholderWhenEncrypted()
    {
        var encryption = _fixture.ServiceProvider.GetRequiredService<IConfigValueEncryptionService>();
        Assert.True(encryption.IsEncryptionEnabled);
        Assert.Equal("***", encryption.MaskValue("secret", [1, 2, 3]));
        Assert.Equal("plain", encryption.MaskValue("plain", null));
    }

    [Fact]
    public async Task SaveDefinition_RejectsTypeMismatch()
    {
        var ct = TestContext.Current.CancellationToken;
        var store = Store();
        var definition = NewDefinition("bad-type", typeof(int));
        definition.DefaultValue = new() { TypeName = typeof(int).FullName!, Json = "\"not-an-int\"" };
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveDefinitionAsync(definition, ct));
    }

    private IConfigStore Store() => _fixture.ServiceProvider.GetRequiredService<IConfigStore>();

    private Task<ConfigDbContext> CreateDbAsync(CancellationToken ct)
        => _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<ConfigDbContext>>().CreateDbContextAsync(ct);

    private static ConfigDefinitionRecord NewDefinition(string key, Type valueType)
        => new() {
            SubjectEntityType = AppConfigEntity.AppEntityType,
            Key = $"{key}-{Guid.NewGuid():N}",
            ForValueType = ConfigValue.GetTypeName(valueType)
        };
}
