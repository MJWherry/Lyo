using Lyo.Configuration.Validation;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;
using Lyo.Query.Services.ValueConversion;
using Lyo.Query.Services.WhereClause;
using Lyo.Validation;
using Lyo.Validation.Models;
using Microsoft.Extensions.Configuration;
namespace Lyo.Configuration.Validation.Tests;

public sealed class ConfigurationValidatorTests
{
    [Fact]
    public async Task ValidateKeysAsync_SatisfyingClause_Passes()
    {
        var (validator, store) = Build(
            new() {
                ["Database:Host"] = "db.internal",
                ["Database:Port"] = "5432"
            });

        await SaveAsync(
            store, "host.v1",
            new GroupClause(
                GroupOperatorEnum.And, [
                    new ConditionClause("Database.Host", ComparisonOperatorEnum.NotEquals, ""),
                    new ConditionClause("Database.Port", ComparisonOperatorEnum.GreaterThan, 1024)
                ]));

        var result = await validator.ValidateKeysAsync("host.v1");
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ValidateKeysAsync_FailingField_ReportsField()
    {
        var (validator, store) = Build(new() { ["Database:Port"] = "80" });
        await SaveAsync(store, "host.v1", new ConditionClause("Database.Port", ComparisonOperatorEnum.GreaterThan, 1024));

        var result = await validator.ValidateKeysAsync("host.v1");

        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors!);
        Assert.Contains("Database.Port", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ValidateKeysAsync_NumericOperators_CompareNumerically()
    {
        // As strings "9" > "1024", but as numbers 9 < 1024. The probe type comes from the rule's literal, so the engine compares numbers.
        var (validator, store) = Build(new() { ["Database:Port"] = "9" });
        await SaveAsync(store, "host.v1", new ConditionClause("Database.Port", ComparisonOperatorEnum.GreaterThan, 1024));

        Assert.False((await validator.ValidateKeysAsync("host.v1")).IsSuccess);
    }

    [Fact]
    public async Task ValidateKeysAsync_MissingKey_FailsWithMessage()
    {
        var (validator, store) = Build(new() { ["Database:Host"] = "db.internal" });
        await SaveAsync(store, "host.v1", new ConditionClause("Database.Port", ComparisonOperatorEnum.GreaterThan, 0));

        var result = await validator.ValidateKeysAsync("host.v1");

        Assert.False(result.IsSuccess);
        Assert.Contains("not set", Assert.Single(result.Errors!).Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateKeysAsync_DottedRulePaths_ResolveAsKeyDelimiters()
    {
        var (validator, store) = Build(new() { ["Serilog:MinimumLevel:Default"] = "Information" });
        await SaveAsync(store, "log.v1", new ConditionClause("Serilog.MinimumLevel.Default", ComparisonOperatorEnum.Equals, "Information"));

        Assert.True((await validator.ValidateKeysAsync("log.v1")).IsSuccess);
    }

    [Fact]
    public async Task ValidateKeysAsync_ColonPaths_Resolve()
    {
        var (validator, store) = Build(new() { ["Serilog:MinimumLevel:Default"] = "Information" });
        await SaveAsync(store, "log.v1", new ConditionClause("Serilog:MinimumLevel:Default", ComparisonOperatorEnum.Equals, "Information"));

        Assert.True((await validator.ValidateKeysAsync("log.v1")).IsSuccess);
    }

    [Fact]
    public async Task ValidateKeysAsync_TrailingCountSegment_ReadsChildCount()
    {
        var (validator, store) = Build(
            new() {
                ["Hosts:0"] = "a.internal",
                ["Hosts:1"] = "b.internal"
            });

        await SaveAsync(store, "hosts.v1", new ConditionClause("Hosts.Count", ComparisonOperatorEnum.GreaterThanOrEqual, 2));

        Assert.True((await validator.ValidateKeysAsync("hosts.v1")).IsSuccess);
    }

    [Fact]
    public async Task ValidateKeysAsync_InOperator_UsesListSemantics()
    {
        var (validator, store) = Build(new() { ["Environment"] = "Staging" });
        await SaveAsync(store, "env.v1", new ConditionClause("Environment", ComparisonOperatorEnum.In, new[] { "Development", "Staging" }));

        Assert.True((await validator.ValidateKeysAsync("env.v1")).IsSuccess);
    }

    [Fact]
    public async Task ValidateKeysAsync_OrGroup_PassesWhenEitherBranch()
    {
        var (validator, store) = Build(new() { ["Storage:Local:Path"] = "/var/lyo" });
        await SaveAsync(
            store, "storage.v1",
            new GroupClause(
                GroupOperatorEnum.Or, [
                    new ConditionClause("Storage.S3.Bucket", ComparisonOperatorEnum.NotEquals, ""),
                    new ConditionClause("Storage.Local.Path", ComparisonOperatorEnum.NotEquals, "")
                ]));

        Assert.True((await validator.ValidateKeysAsync("storage.v1")).IsSuccess);
    }

    [Fact]
    public async Task ValidateKeysAsync_FailedOrGroup_ReportsFirstField()
    {
        var (validator, store) = Build(new() { ["Unrelated"] = "x" });
        await SaveAsync(
            store, "storage.v1",
            new GroupClause(
                GroupOperatorEnum.Or, [
                    new ConditionClause("Storage.S3.Bucket", ComparisonOperatorEnum.Equals, "lyo"),
                    new ConditionClause("Storage.Local.Path", ComparisonOperatorEnum.Equals, "/var/lyo")
                ]));

        var result = await validator.ValidateKeysAsync("storage.v1");

        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors!);
        Assert.Equal("Storage.S3.Bucket", error.Metadata!["propertyName"]);
    }

    [Fact]
    public async Task ValidateKeysAsync_SchemaMessageOverride_ReplacesGeneratedError()
    {
        var (validator, store) = Build(new() { ["Database:Port"] = "80" });
        await store.SaveAsync(
            new() {
                Key = "host.v1",
                Constraints = new ConditionClause("Database.Port", ComparisonOperatorEnum.GreaterThan, 1024),
                Messages = new Dictionary<string, ValidationMessage> { ["Database.Port"] = new() { ErrorCode = "PORT_LOW", ErrorMessage = "Port must be above 1024." } }
            });

        var result = await validator.ValidateKeysAsync("host.v1");

        var error = Assert.Single(result.Errors!);
        Assert.Equal("PORT_LOW", error.Code);
        Assert.Equal("Port must be above 1024.", error.Message);
    }

    [Fact]
    public async Task ValidateSectionAsync_NamedSection_BindsThenValidates()
    {
        var (validator, store) = Build(
            new() {
                ["Database:Host"] = "db.internal",
                ["Database:Port"] = "5432"
            });

        await SaveAsync(store, "database.v1", new ConditionClause("Port", ComparisonOperatorEnum.GreaterThan, 1024));

        var result = await validator.ValidateSectionAsync<DatabaseOptions>("database.v1", "Database");

        Assert.True(result.IsSuccess);
        Assert.Equal("db.internal", result.Data!.Host);
        Assert.Equal(5432, result.Data.Port);
    }

    [Fact]
    public async Task ValidateSectionAsync_BrokenBoundInstance_Fails()
    {
        var (validator, store) = Build(new() { ["Database:Port"] = "80" });
        await SaveAsync(store, "database.v1", new ConditionClause("Port", ComparisonOperatorEnum.GreaterThan, 1024));

        Assert.False((await validator.ValidateSectionAsync<DatabaseOptions>("database.v1", "Database")).IsSuccess);
    }

    [Fact]
    public async Task ValidateSectionAsync_MissingSectionName_IsConfigurationError()
    {
        var (validator, store) = Build(new() { ["Database:Port"] = "5432" });
        await SaveAsync(store, "database.v1", new ConditionClause("Port", ComparisonOperatorEnum.GreaterThan, 0));

        await Assert.ThrowsAsync<ConfigurationValidationException>(() => validator.ValidateSectionAsync<DatabaseOptions>("database.v1"));
    }

    [Fact]
    public async Task ValidateKeysAsync_MissingSchema_FailsByDefault()
    {
        var (validator, _) = Build(new() { ["Database:Port"] = "5432" });

        var result = await validator.ValidateKeysAsync("absent.v1");

        Assert.False(result.IsSuccess);
        Assert.Contains("absent.v1", Assert.Single(result.Errors!).Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ValidateKeysAsync_MissingSchema_CanBeTolerated()
    {
        var (validator, _) = Build(new() { ["Database:Port"] = "5432" }, new() { FailWhenSchemaMissing = false });

        Assert.True((await validator.ValidateKeysAsync("absent.v1")).IsSuccess);
    }

    [Fact]
    public void Validate_NonConfigurationTarget_PassesThrough()
    {
        var evaluator = new ConfigurationClauseEvaluator(InnerEvaluator());
        var clause = new ConditionClause(nameof(DatabaseOptions.Port), ComparisonOperatorEnum.GreaterThan, 1024);

        Assert.True(evaluator.Explain(new DatabaseOptions { Port = 5432 }, clause).Passed);
        Assert.False(evaluator.Explain(new DatabaseOptions { Port = 80 }, clause).Passed);
    }

    [Fact]
    public async Task ValidateKeysAsync_TreatDotAsKeyDelimiterOff_LeavesDottedKeys()
    {
        var (validator, store) = Build(new() { ["Legacy.Key"] = "on" }, new() { TreatDotAsKeyDelimiter = false });
        await SaveAsync(store, "legacy.v1", new ConditionClause("Legacy.Key", ComparisonOperatorEnum.Equals, "on"));

        Assert.True((await validator.ValidateKeysAsync("legacy.v1")).IsSuccess);
    }

    private static IValidationClauseEvaluator InnerEvaluator() => new WhereClauseServiceEvaluator(new WhereClauseEvaluator(new ValueConversionService()));

    private static (IConfigurationValidator Validator, IValidationSchemaStore Store) Build(
        Dictionary<string, string?> values,
        ConfigurationValidationOptions? options = null)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var resolved = options ?? new ConfigurationValidationOptions();
        var store = new InMemoryValidationSchemaStore();
        var evaluator = new ConfigurationClauseEvaluator(InnerEvaluator(), resolved);
        return (new ConfigurationValidator(configuration, store, evaluator, resolved), store);
    }

    private static Task SaveAsync(IValidationSchemaStore store, string key, WhereClause constraints)
        => store.SaveAsync(
            new() {
                Key = key,
                Constraints = constraints
            });

    private sealed class DatabaseOptions
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
    }
}
