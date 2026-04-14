using Lyo.Common;

namespace Lyo.Validation.Tests;

public class ValidatorBuilderTests
{
    [Fact]
    public void Build_WithValidModel_ReturnsSuccess()
    {
        var validator = ValidatorBuilder<CreatePersonRequest>.Create()
            .RuleFor(x => x.Name)
            .NotWhiteSpace()
            .Length(2, 50)
            .RuleFor(x => x.Email)
            .Email()
            .RuleFor(x => x.Age)
            .InclusiveBetween(18, 120)
            .Build();

        var request = new CreatePersonRequest { Name = "Matt", Email = "matt@example.com", Age = 33 };
        var result = validator.Validate(request);
        Assert.True(result.IsSuccess);
        Assert.Same(request, result.Data);
    }

    [Fact]
    public void Build_WithInvalidModel_ReturnsPropertyErrors()
    {
        var validator = ValidatorBuilder<CreatePersonRequest>.Create()
            .RuleFor(x => x.Name)
            .NotWhiteSpace()
            .RuleFor(x => x.Email)
            .Email()
            .RuleFor(x => x.Age)
            .InclusiveBetween(18, 120)
            .Build();

        var result = validator.Validate(new() { Name = " ", Email = "bad-email", Age = 12 });
        Assert.False(result.IsSuccess);
        Assert.Equal(3, result.Errors!.Count);
        Assert.Contains(result.Errors!, x => Equals(x.Metadata?[ValidationMetadataKeys.PropertyName], "Name"));
        Assert.Contains(result.Errors!, x => Equals(x.Metadata?[ValidationMetadataKeys.PropertyName], "Email"));
        Assert.Contains(result.Errors!, x => Equals(x.Metadata?[ValidationMetadataKeys.PropertyName], "Age"));
    }

    [Fact]
    public void Build_WithModelLevelRule_ReturnsFailure()
    {
        var validator = ValidatorBuilder<CreatePersonRequest>.Create()
            .RuleFor(x => x.Password)
            .NotWhiteSpace()
            .RuleFor(x => x.ConfirmPassword)
            .NotWhiteSpace()
            .Must(x => string.Equals(x.Password, x.ConfirmPassword, StringComparison.Ordinal), "PASSWORD_MISMATCH", "Passwords must match")
            .Build();

        var result = validator.Validate(new() { Password = "abc123", ConfirmPassword = "xyz789" });
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors!, x => x.Code == "PASSWORD_MISMATCH");
    }

    [Fact]
    public void Build_WithNestedValidator_ReusesExistingRules()
    {
        var addressValidator = ValidatorBuilder<Address>.Create().RuleFor(x => x.City).NotWhiteSpace().RuleFor(x => x.PostalCode).NotWhiteSpace().Build();
        var validator = ValidatorBuilder<CreatePersonRequest>.Create().RuleFor(x => x.Address).NotNull().SetValidator(addressValidator).Build();
        var result = validator.Validate(new() { Address = new() { City = "", PostalCode = "" } });
        Assert.False(result.IsSuccess);
        Assert.Equal(2, result.Errors!.Count);
        Assert.Contains(result.Errors!, error => Equals(error.Metadata?[ValidationMetadataKeys.PropertyName], "Address.City"));
        Assert.Contains(result.Errors!, error => Equals(error.Metadata?[ValidationMetadataKeys.PropertyName], "Address.PostalCode"));
    }

    [Fact]
    public void Build_WithEnumerableContainsRules_ReturnsExpectedErrors()
    {
        var validator = ValidatorBuilder<CreatePersonRequest>.Create().RuleFor(x => x.Tags).Contains("admin").RuleFor(x => x.BlockedTags).NotContains("banned").Build();
        var result = validator.Validate(new() { Tags = ["user"], BlockedTags = ["banned"] });
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors!, error => error.Code == ValidationErrorCodes.MissingItem && Equals(error.Metadata?[ValidationMetadataKeys.PropertyName], "Tags"));
        Assert.Contains(result.Errors!, error => error.Code == ValidationErrorCodes.DisallowedItem && Equals(error.Metadata?[ValidationMetadataKeys.PropertyName], "BlockedTags"));
    }

    [Fact]
    public void Build_WithEnumerableContainsRules_UsesComparer()
    {
        var validator = ValidatorBuilder<CreatePersonRequest>.Create()
            .RuleFor(x => x.Tags)
            .Contains("ADMIN", StringComparer.OrdinalIgnoreCase)
            .RuleFor(x => x.BlockedTags)
            .NotContains("BANNED", StringComparer.OrdinalIgnoreCase)
            .Build();

        var result = validator.Validate(new() { Tags = ["admin"], BlockedTags = ["guest"] });
        Assert.True(result.IsSuccess);
    }

    private sealed class CreatePersonRequest
    {
        public string? Name { get; set; }

        public string? Email { get; set; }

        public int Age { get; set; }

        public string? Password { get; set; }

        public string? ConfirmPassword { get; set; }

        public Address Address { get; set; } = new();

        public List<string> Tags { get; set; } = [];

        public string[] BlockedTags { get; set; } = [];
    }

    private sealed class Address
    {
        public string? City { get; set; }

        public string? PostalCode { get; set; }
    }
}