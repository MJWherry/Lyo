using Lyo.Exceptions.Models;

namespace Lyo.Exceptions.Tests;

public class ValidationErrorsBuilderTests
{
    [Fact]
    public void NewBuilder_HasNoErrors()
    {
        var builder = new ValidationErrorsBuilder();
        Assert.False(builder.HasErrors);
        Assert.Equal(0, builder.Count);
        Assert.Empty(builder.Build());
    }

    [Fact]
    public void ThrowIfAny_NoErrors_DoesNotThrow() => new ValidationErrorsBuilder().ThrowIfAny();

    [Fact]
    public void Add_AccumulatesErrorsPerField()
    {
        var builder = new ValidationErrorsBuilder();
        builder.Add("Email", "Email is required.").Add("Email", "Email is invalid.").Add("Age", "Age must be positive.");
        Assert.True(builder.HasErrors);
        Assert.Equal(3, builder.Count);
        var errors = builder.Build();
        Assert.Equal(2, errors.Count);
        Assert.Equal(new[] { "Email is required.", "Email is invalid." }, errors["Email"]);
        Assert.Equal(new[] { "Age must be positive." }, errors["Age"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Add_WhitespaceFieldName_Throws(string fieldName) => Assert.Throws<ArgumentException>(() => new ValidationErrorsBuilder().Add(fieldName, "message"));

    [Fact]
    public void Add_NullFieldName_Throws() => Assert.Throws<ArgumentNullException>(() => new ValidationErrorsBuilder().Add(null!, "message"));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Add_WhitespaceMessage_Throws(string message) => Assert.Throws<ArgumentException>(() => new ValidationErrorsBuilder().Add("Email", message));

    [Fact]
    public void AddRange_AddsAllNonWhitespaceMessages()
    {
        var builder = new ValidationErrorsBuilder();
        builder.AddRange("Email", new[] { "Email is required.", "", "  ", "Email is invalid." });
        Assert.Equal(2, builder.Count);
        Assert.Equal(new[] { "Email is required.", "Email is invalid." }, builder.Build()["Email"]);
    }

    [Fact]
    public void AddRange_NullMessages_Throws() => Assert.Throws<ArgumentNullException>(() => new ValidationErrorsBuilder().AddRange("Email", null!));

    [Fact]
    public void AddIf_True_AddsError()
    {
        var builder = new ValidationErrorsBuilder().AddIf(true, "Email", "Email is required.");
        Assert.True(builder.HasErrors);
    }

    [Fact]
    public void AddIf_False_DoesNotAdd()
    {
        var builder = new ValidationErrorsBuilder().AddIf(false, "Email", "Email is required.");
        Assert.False(builder.HasErrors);
    }

    [Fact]
    public void ThrowIfAny_WithErrors_ThrowsValidationExceptionContainingErrors()
    {
        var builder = new ValidationErrorsBuilder().Add("Email", "Email is required.");
        var ex = Assert.Throws<ValidationException>(() => builder.ThrowIfAny());
        Assert.Single(ex.Errors);
        Assert.Equal("Email is required.", ex.Errors["Email"][0]);
    }

    [Fact]
    public void ThrowIfAny_WithCustomMessage_UsesMessage()
    {
        var builder = new ValidationErrorsBuilder().Add("Email", "Email is required.");
        var ex = Assert.Throws<ValidationException>(() => builder.ThrowIfAny("Signup request invalid."));
        Assert.StartsWith("Signup request invalid.", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_ReturnsSnapshotShapedForValidationException()
    {
        var builder = new ValidationErrorsBuilder().Add("Email", "Email is required.");
        var errors = builder.Build();
        var ex = new ValidationException(errors);
        Assert.Equal("Email is required.", ex.Errors["Email"][0]);
    }
}