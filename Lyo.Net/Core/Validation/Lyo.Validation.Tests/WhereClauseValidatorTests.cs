using System.Text.Json;
using Lyo.Cache;
using Lyo.Common;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;
using Lyo.Query.Services.ValueConversion;
using Lyo.Query.Services.WhereClause;
using Lyo.Result;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Validation.Tests;

public class WhereClauseValidatorTests
{
    [Fact]
    public void Validate_EqualsInRegexNotIn_ReturnsExpectedErrors()
    {
        var schema = new ValidationSchema {
            Key = "signup.v2",
            TargetTypeName = nameof(CreateUserRequest),
            Constraints = WhereClauseBuilder.And(b => b
                .Add(new ConditionClause("Email", ComparisonOperatorEnum.Regex, "^[^@]+@[^@]+$"))
                .Add(new ConditionClause("Role", ComparisonOperatorEnum.In, new[] { "User", "Admin" }))
                .Add(new ConditionClause("Status", ComparisonOperatorEnum.NotIn, new[] { "Banned" })))
        };
        var validator = new WhereClauseValidator<CreateUserRequest>(schema, CreateEvaluator());
        var result = validator.Validate(new() { Email = "not-an-email", Role = "Guest", Status = "Banned" });
        Assert.False(result.IsSuccess);
        Assert.Equal(3, result.Errors!.Count);
        Assert.Contains(result.Errors, e => Equals(e.Metadata?[ValidationMetadataKeys.PropertyName], "Email") && e.Code == ValidationErrorCodes.InvalidFormat);
        Assert.Contains(result.Errors, e => Equals(e.Metadata?[ValidationMetadataKeys.PropertyName], "Role") && e.Code == ValidationErrorCodes.MissingItem);
        Assert.Contains(result.Errors, e => Equals(e.Metadata?[ValidationMetadataKeys.PropertyName], "Status") && e.Code == ValidationErrorCodes.DisallowedItem);
    }

    [Fact]
    public void Validate_MatchingInstance_ReturnsSuccess()
    {
        var schema = SignupSchema();
        var validator = new WhereClauseValidator<CreateUserRequest>(schema, CreateEvaluator());
        var request = new CreateUserRequest { Email = "matt@example.com", Role = "Admin", Status = "Active" };
        var result = validator.Validate(request);
        Assert.True(result.IsSuccess);
        Assert.Same(request, result.Data);
    }

    [Fact]
    public void Validate_MessageOverrides_ReplaceCodeAndMessage()
    {
        var schema = SignupSchema();
        schema.Messages = new Dictionary<string, ValidationMessage>(StringComparer.OrdinalIgnoreCase) {
            ["Email"] = new() { ErrorCode = "BAD_EMAIL", ErrorMessage = "Use a real email" }
        };
        var validator = new WhereClauseValidator<CreateUserRequest>(schema, CreateEvaluator());
        var result = validator.Validate(new() { Email = "nope", Role = "User", Status = "Active" });
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors!, e => e.Code == "BAD_EMAIL" && e.Message == "Use a real email");
    }

    [Fact]
    public void Compile_TargetTypeMismatch_Throws()
    {
        var schema = SignupSchema();
        schema.TargetTypeName = "OtherRequest";
        var compiler = new ValidationSchemaCompiler(new InMemoryValidationSchemaStore(), CreateEvaluator());
        var ex = Assert.Throws<InvalidOperationException>(() => compiler.Compile<CreateUserRequest>(schema));
        Assert.Contains("OtherRequest", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetAsync_LoadsFromStore_AndValidates()
    {
        var store = new InMemoryValidationSchemaStore();
        await store.SaveAsync(SignupSchema(), TestContext.Current.CancellationToken);
        var compiler = new ValidationSchemaCompiler(store, CreateEvaluator());
        var validator = await compiler.GetAsync<CreateUserRequest>("signup.v2", TestContext.Current.CancellationToken);
        var result = validator.Validate(new() { Email = "matt@example.com", Role = "User", Status = "Active" });
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void IncludeSchema_MixesWithFluentRules()
    {
        var evaluator = CreateEvaluator();
        var validator = ValidatorBuilder<CreateUserRequest>.Create()
            .RuleFor(x => x.Name)
            .NotWhiteSpace()
            .IncludeSchema(SignupSchema(), evaluator)
            .Build();
        var result = validator.Validate(new() { Name = " ", Email = "matt@example.com", Role = "User", Status = "Active" });
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors!, e => Equals(e.Metadata?[ValidationMetadataKeys.PropertyName], "Name"));
    }

    [Fact]
    public async Task IncludeStore_LoadsSchema()
    {
        var store = new InMemoryValidationSchemaStore();
        await store.SaveAsync(SignupSchema(), TestContext.Current.CancellationToken);
        var validator = ValidatorBuilder<CreateUserRequest>.Create().IncludeStore(store, "signup.v2", CreateEvaluator()).Build();
        var result = validator.Validate(new() { Email = "x", Role = "User", Status = "Active" });
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors!, e => Equals(e.Metadata?[ValidationMetadataKeys.PropertyName], "Email"));
    }

    [Fact]
    public void Schema_JsonRoundTrip_PreservesOperators()
    {
        var schema = SignupSchema();
        schema.Messages = new Dictionary<string, ValidationMessage> { ["Role"] = new() { ErrorCode = "BAD_ROLE" } };
        var json = JsonSerializer.Serialize(schema, LyoJsonSerializerOptions.Create());
        var loaded = JsonSerializer.Deserialize<ValidationSchema>(json, LyoJsonSerializerOptions.Create());
        Assert.NotNull(loaded);
        Assert.Equal("signup.v2", loaded.Key);
        Assert.IsType<GroupClause>(loaded.Constraints);
        var group = (GroupClause)loaded.Constraints;
        Assert.Equal(3, group.Children.Count);
        Assert.Contains(group.Children.OfType<ConditionClause>(), c => c.Comparison == ComparisonOperatorEnum.In);
        Assert.Contains(group.Children.OfType<ConditionClause>(), c => c.Comparison == ComparisonOperatorEnum.NotIn);
        Assert.Contains(group.Children.OfType<ConditionClause>(), c => c.Comparison == ComparisonOperatorEnum.Regex);
        Assert.Equal("BAD_ROLE", loaded.Messages!["Role"].ErrorCode);
    }

    [Fact]
    public async Task InMemoryStore_ListGetDelete()
    {
        var store = new InMemoryValidationSchemaStore();
        await store.SaveAsync(SignupSchema(), TestContext.Current.CancellationToken);
        await store.SaveAsync(
            new ValidationSchema { Key = "other", TargetTypeName = "Other", Constraints = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.Equals, "A") },
            TestContext.Current.CancellationToken);
        var listed = await store.ListAsync(nameof(CreateUserRequest), TestContext.Current.CancellationToken);
        Assert.Single(listed);
        Assert.NotNull(await store.GetAsync("signup.v2", TestContext.Current.CancellationToken));
        Assert.True(await store.DeleteAsync("signup.v2", TestContext.Current.CancellationToken));
        Assert.Null(await store.GetAsync("signup.v2", TestContext.Current.CancellationToken));
    }

    [Fact]
    public void FluentInNotIn_ReturnsExpectedErrors()
    {
        var validator = ValidatorBuilder<CreateUserRequest>.Create().RuleFor(x => x.Role).In("User", "Admin").RuleFor(x => x.Status).NotIn("Banned").Build();
        var result = validator.Validate(new() { Role = "Guest", Status = "Banned" });
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors!, e => e.Code == ValidationErrorCodes.MissingItem && Equals(e.Metadata?[ValidationMetadataKeys.PropertyName], "Role"));
        Assert.Contains(result.Errors!, e => e.Code == ValidationErrorCodes.DisallowedItem && Equals(e.Metadata?[ValidationMetadataKeys.PropertyName], "Status"));
    }

    private static ValidationSchema SignupSchema()
        => new() {
            Key = "signup.v2",
            TargetTypeName = nameof(CreateUserRequest),
            Constraints = WhereClauseBuilder.And(b => b
                .Add(new ConditionClause("Email", ComparisonOperatorEnum.Regex, "^[^@]+@[^@]+$"))
                .Add(new ConditionClause("Role", ComparisonOperatorEnum.In, new[] { "User", "Admin" }))
                .Add(new ConditionClause("Status", ComparisonOperatorEnum.NotIn, new[] { "Banned" })))
        };

    private static WhereClauseServiceEvaluator CreateEvaluator()
    {
        var logger = new NullLogger<LocalCacheService>();
        var cacheOptions = new CacheOptions { Enabled = false };
        var cache = new LocalCacheService(new MemoryCache(new MemoryCacheOptions()), logger, cacheOptions);
        return new(new BaseWhereClauseService(cache, cacheOptions, new ValueConversionService()));
    }

    private sealed class CreateUserRequest
    {
        public string? Name { get; set; }

        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
    }
}
