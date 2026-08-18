using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;
using Lyo.Result;
using Lyo.Result.Enums;

namespace Lyo.Query.Tests;

public class ExplainMatchToErrorsTests : WhereClauseServiceTests
{
    [Fact]
    public void ToErrors_Passed_ReturnsEmpty()
    {
        var svc = CreateService();
        var person = new PersonBuilder().WithName("Alice").Build();
        var clause = WhereClauseBuilder.And(b => b.Equals("Name", "Alice"));
        var errors = svc.ExplainMatch(person, clause).ToErrors();
        Assert.Empty(errors);
    }

    [Fact]
    public void ToErrors_And_EmitsOneErrorPerFailingCondition()
    {
        var svc = CreateService();
        var person = new PersonBuilder().WithName("Bob").WithAge(10).Build();
        var clause = WhereClauseBuilder.And(b => b.Equals("Name", "Alice").Equals("Age", 99));
        var errors = svc.ExplainMatch(person, clause).ToErrors();
        Assert.Equal(2, errors.Count);
        Assert.Contains(errors, e => Equals(e.Metadata?["propertyName"], "Name"));
        Assert.Contains(errors, e => Equals(e.Metadata?["propertyName"], "Age"));
        Assert.All(errors, e => Assert.Equal(ErrorType.Validation, e.Type));
    }

    [Fact]
    public void ToErrors_Or_EmitsSingleError()
    {
        var svc = CreateService();
        var person = new PersonBuilder().WithName("Charlie").Build();
        var clause = WhereClauseBuilder.Or(b => b.Equals("Name", "Alpha").Equals("Name", "Bravo"));
        var errors = svc.ExplainMatch(person, clause).ToErrors();
        Assert.Single(errors);
        Assert.Equal(ValidationErrorCodes.ValidationFailed, errors[0].Code);
        Assert.False(string.IsNullOrWhiteSpace(errors[0].Message));
    }

    [Fact]
    public void ToErrors_In_UsesMissingItemCode()
    {
        var svc = CreateService();
        var person = new PersonBuilder().WithName("Zed").Build();
        var clause = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.In, new[] { "Alice", "Bob" });
        var errors = svc.ExplainMatch(person, clause).ToErrors();
        Assert.Single(errors);
        Assert.Equal(ValidationErrorCodes.MissingItem, errors[0].Code);
        Assert.Equal("Name", errors[0].Metadata!["propertyName"]);
    }

    [Fact]
    public void ToErrors_NotIn_UsesDisallowedItemCode()
    {
        var svc = CreateService();
        var person = new PersonBuilder().WithName("Alice").Build();
        var clause = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.NotIn, new[] { "Alice", "Bob" });
        var errors = svc.ExplainMatch(person, clause).ToErrors();
        Assert.Single(errors);
        Assert.Equal(ValidationErrorCodes.DisallowedItem, errors[0].Code);
    }

    [Fact]
    public void ToErrors_Regex_UsesInvalidFormatCode()
    {
        var svc = CreateService();
        var person = new PersonBuilder().WithName("alice").Build();
        var clause = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.Regex, "^[A-Z]");
        var errors = svc.ExplainMatch(person, clause).ToErrors();
        Assert.Single(errors);
        Assert.Equal(ValidationErrorCodes.InvalidFormat, errors[0].Code);
    }

    [Fact]
    public void ToErrors_MessageOverride_ByFieldPath()
    {
        var svc = CreateService();
        var person = new PersonBuilder().WithName("Bob").Build();
        var clause = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.Equals, "Alice");
        var errors = svc.ExplainMatch(person, clause)
            .ToErrors(new Dictionary<string, WhereClauseErrorOverride>(StringComparer.OrdinalIgnoreCase) {
                ["name"] = new() { ErrorCode = "BAD_NAME", ErrorMessage = "Name must be Alice" }
            });
        Assert.Single(errors);
        Assert.Equal("BAD_NAME", errors[0].Code);
        Assert.Equal("Name must be Alice", errors[0].Message);
    }

    [Fact]
    public void ToErrors_NullEntity_ReturnsFailureSummary()
    {
        var svc = CreateService();
        var clause = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.Equals, "X");
        var errors = svc.ExplainMatch<Person>(null!, clause).ToErrors();
        Assert.Single(errors);
        Assert.Equal("Entity is null.", errors[0].Message);
    }
}
