using Lyo.Cache;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;
using Lyo.Query.Services.ValueConversion;
using Lyo.Query.Services.WhereClause;
using Lyo.Validation.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Validation.Postgres.Tests;

public class PostgresValidationSchemaStoreTests
{
    private readonly ValidationPostgresFixture _fixture;

    public PostgresValidationSchemaStoreTests(ValidationPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public void AddPostgresValidationStore_WithNullServices_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => Extensions.AddPostgresValidationStore(null!, _ => { }));
        Assert.Equal("services", ex.ParamName);
    }

    [Fact]
    public async Task DbContext_MigrationsApplied_SchemaExists()
    {
        var factory = _fixture.ServiceProvider.GetRequiredService<IDbContextFactory<ValidationDbContext>>();
        await using var context = factory.CreateDbContext();
        var pending = await context.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken);
        Assert.Empty(pending);
    }

    [Fact]
    public async Task Store_SaveGetListDelete_RoundTripsWhereClause()
    {
        var store = _fixture.ServiceProvider.GetRequiredService<IValidationSchemaStore>();
        var key = $"signup-{Guid.NewGuid():N}";
        var schema = new ValidationSchema {
            Key = key,
            TargetTypeName = nameof(SignupTarget),
            Description = "Signup rules",
            Constraints = WhereClauseBuilder.And(b => b
                .Regex("Email", "^[^@]+@[^@]+$")
                .In("Role", "User", "Admin")
                .NotIn("Status", "Banned")),
            Messages = new Dictionary<string, ValidationMessage> { ["Email"] = new() { ErrorCode = "BAD_EMAIL", ErrorMessage = "Bad email" } }
        };

        await store.SaveAsync(schema, TestContext.Current.CancellationToken);
        var loaded = await store.GetAsync(key, TestContext.Current.CancellationToken);
        Assert.NotNull(loaded);
        Assert.Equal(key, loaded.Key);
        Assert.Equal(nameof(SignupTarget), loaded.TargetTypeName);
        Assert.IsType<GroupClause>(loaded.Constraints);
        Assert.Equal("BAD_EMAIL", loaded.Messages!["Email"].ErrorCode);

        var listed = await store.ListAsync(nameof(SignupTarget), TestContext.Current.CancellationToken);
        Assert.Contains(listed, s => s.Key == key);

        schema.Description = "Updated";
        await store.SaveAsync(schema, TestContext.Current.CancellationToken);
        var updated = await store.GetAsync(key, TestContext.Current.CancellationToken);
        Assert.Equal("Updated", updated!.Description);

        Assert.True(await store.DeleteAsync(key, TestContext.Current.CancellationToken));
        Assert.Null(await store.GetAsync(key, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Store_CompileAfterLoad_ValidatesInRegexNotIn()
    {
        var store = _fixture.ServiceProvider.GetRequiredService<IValidationSchemaStore>();
        var key = $"compile-{Guid.NewGuid():N}";
        await store.SaveAsync(
            new ValidationSchema {
                Key = key,
                TargetTypeName = nameof(SignupTarget),
                Constraints = WhereClauseBuilder.And(b => b
                    .Regex("Email", "^[^@]+@[^@]+$")
                    .In("Role", "User", "Admin")
                    .NotIn("Status", "Banned"))
            },
            TestContext.Current.CancellationToken);

        var loaded = await store.GetAsync(key, TestContext.Current.CancellationToken);
        Assert.NotNull(loaded);
        var compiler = new ValidationSchemaCompiler(store, CreateEvaluator());
        var validator = compiler.Compile<SignupTarget>(loaded);
        var ok = validator.Validate(new() { Email = "a@b.com", Role = "User", Status = "Active" });
        Assert.True(ok.IsSuccess);
        var bad = validator.Validate(new() { Email = "nope", Role = "Guest", Status = "Banned" });
        Assert.False(bad.IsSuccess);
        Assert.Equal(3, bad.Errors!.Count);
    }

    private static WhereClauseServiceEvaluator CreateEvaluator()
    {
        var logger = new NullLogger<LocalCacheService>();
        var cacheOptions = new CacheOptions { Enabled = false };
        var cache = new LocalCacheService(new MemoryCache(new MemoryCacheOptions()), logger, cacheOptions);
        return new(new BaseWhereClauseService(cache, cacheOptions, new ValueConversionService()));
    }

    private sealed class SignupTarget
    {
        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
    }
}
