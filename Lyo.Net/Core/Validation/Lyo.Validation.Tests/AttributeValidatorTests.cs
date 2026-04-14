using Lyo.Common;
using Lyo.Validation.Attributes;
using DataAnnotations = System.ComponentModel.DataAnnotations;

namespace Lyo.Validation.Tests;

public class AttributeValidatorTests
{
    [Fact]
    public void Validate_WithValidAttributedModel_ReturnsSuccess()
    {
        var validator = new AttributeValidator<CreateUserRequest>();
        var request = new CreateUserRequest {
            Name = "Matt",
            Email = "matt@example.com",
            Age = 33,
            Website = "https://lyo.dev",
            Tags = ["one"]
        };

        var result = validator.Validate(request);
        Assert.True(result.IsSuccess);
        Assert.Same(request, result.Data);
    }

    [Fact]
    public void Validate_WithInvalidAttributedModel_ReturnsPropertyErrors()
    {
        var validator = new AttributeValidator<CreateUserRequest>();
        var result = validator.Validate(
            new() {
                Name = " ",
                Email = "bad",
                Age = 5,
                Website = "nope",
                Tags = []
            });

        Assert.False(result.IsSuccess);
        Assert.Equal(6, result.Errors!.Count);
        Assert.Contains(result.Errors!, x => Equals(x.Metadata?[ValidationMetadataKeys.PropertyName], "Name"));
        Assert.Contains(result.Errors!, x => Equals(x.Metadata?[ValidationMetadataKeys.PropertyName], "Email"));
        Assert.Contains(result.Errors!, x => Equals(x.Metadata?[ValidationMetadataKeys.PropertyName], "Age"));
        Assert.Contains(result.Errors!, x => Equals(x.Metadata?[ValidationMetadataKeys.PropertyName], "Website"));
        Assert.Contains(result.Errors!, x => Equals(x.Metadata?[ValidationMetadataKeys.PropertyName], "Tags"));
    }

    [Fact]
    public void ValidateWithAttributes_Extension_UsesAttributeValidator()
    {
        var result = new CreateUserRequest {
            Name = "A",
            Email = "matt@example.com",
            Age = 30,
            Website = "https://lyo.dev",
            Tags = ["tag"]
        }.ValidateWithAttributes();

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors!, x => Equals(x.Metadata?[ValidationMetadataKeys.PropertyName], "Name"));
    }

    [Fact]
    public void Validate_WithDataAnnotationsModel_ReturnsMappedErrors()
    {
        var validator = new AttributeValidator<DataAnnotationsRequest>();
        var result = validator.Validate(new() { Name = "", Email = "bad", Age = 15 });
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors!, x => x.Code == ValidationErrorCodes.RequiredValue && Equals(x.Metadata?[ValidationMetadataKeys.PropertyName], "Name"));
        Assert.Contains(result.Errors!, x => x.Code == ValidationErrorCodes.InvalidEmail && Equals(x.Metadata?[ValidationMetadataKeys.PropertyName], "Email"));
        Assert.Contains(result.Errors!, x => x.Code == ValidationErrorCodes.OutOfRange && Equals(x.Metadata?[ValidationMetadataKeys.PropertyName], "Age"));
    }

    [Fact]
    public void Validate_WithIValidatableObject_AddsModelAndPropertyErrors()
    {
        var validator = new AttributeValidator<DataAnnotationsRequest>();
        var result = validator.Validate(new() { Name = "Valid", Email = "matt@example.com", Age = 42 });
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors!, x => x.Code == ValidationErrorCodes.ValidationFailed && Equals(x.Metadata?[ValidationMetadataKeys.PropertyName], "Age"));
        Assert.Contains(result.Errors!, x => x.Code == ValidationErrorCodes.ValidationFailed && x.Metadata == null);
    }

    [Fact]
    public void IncludeAttributes_AddsAttributeRulesToBuilder()
    {
        var validator = ValidatorBuilder<DataAnnotationsRequest>.Create().IncludeAttributes().Build();
        var result = validator.Validate(new() { Name = "", Email = "bad", Age = 15 });
        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.Errors!);
    }

    private sealed class CreateUserRequest
    {
        [NotWhiteSpace]
        [Length(2, 20)]
        public string? Name { get; set; }

        [Email]
        public string? Email { get; set; }

        [Range(18, 120)]
        public int Age { get; set; }

        [Uri]
        public string? Website { get; set; }

        [NotEmpty]
        public List<string> Tags { get; set; } = [];
    }

    private sealed class DataAnnotationsRequest : DataAnnotations.IValidatableObject
    {
        [DataAnnotations.Required]
        public string? Name { get; set; }

        [DataAnnotations.EmailAddress]
        public string? Email { get; set; }

        [DataAnnotations.Range(18, 120)]
        public int Age { get; set; }

        public IEnumerable<DataAnnotations.ValidationResult> Validate(DataAnnotations.ValidationContext validationContext)
        {
            if (Age == 42)
                yield return new("Age cannot be 42", [nameof(Age)]);

            if (Age == 42)
                yield return new("The answer is not allowed here");
        }
    }
}