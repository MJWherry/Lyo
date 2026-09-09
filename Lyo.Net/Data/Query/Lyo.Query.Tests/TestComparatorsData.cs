using Lyo.Query.Models.Enums;

namespace Lyo.Query.Tests;

public static class TestComparatorsData
{
    public static TheoryData<string, ComparisonOperatorEnum, object, int> StringComparatorData { get; } = new() {
        { "Name", ComparisonOperatorEnum.Equals, "Alice", 1 },
        { "Name", ComparisonOperatorEnum.Contains, "li", 2 },
        { "Name", ComparisonOperatorEnum.StartsWith, "Al", 2 },
        { "Name", ComparisonOperatorEnum.EndsWith, "ce", 2 },
        { "Name", ComparisonOperatorEnum.Regex, "(?i)^ali", 2 },
        { "Name", ComparisonOperatorEnum.NotContains, "li", 1 },
        { "Name", ComparisonOperatorEnum.NotStartsWith, "Al", 1 },
        { "Name", ComparisonOperatorEnum.NotEndsWith, "ce", 1 },
        { "Name", ComparisonOperatorEnum.NotRegex, "(?i)^ali", 1 }
    };

    public static TheoryData<string, ComparisonOperatorEnum, object, int> IntComparatorData { get; } = new() {
        { "Age", ComparisonOperatorEnum.GreaterThan, 15, 2 },
        { "Age", ComparisonOperatorEnum.LessThanOrEqual, 20, 2 },
        { "Age", ComparisonOperatorEnum.In, new object[] { 20, 30 }, 2 },
        { "Age", ComparisonOperatorEnum.Equals, 20, 1 },
        { "Age", ComparisonOperatorEnum.NotEquals, 20, 2 },
        { "AgeNullable", ComparisonOperatorEnum.Equals, null!, 1 },
        { "Age", ComparisonOperatorEnum.GreaterThanOrEqual, 30, 1 },
        { "Age", ComparisonOperatorEnum.LessThan, 20, 1 },
        { "Age", ComparisonOperatorEnum.NotIn, new object[] { 20, 30 }, 1 }
    };

    public static TheoryData<string, ComparisonOperatorEnum, bool> GuidComparatorData { get; } = new() {
        { "Id", ComparisonOperatorEnum.Equals, true },
        { "Id", ComparisonOperatorEnum.In, false },
        { "Id", ComparisonOperatorEnum.NotEquals, true },
        { "Id", ComparisonOperatorEnum.NotIn, false },
        { "IdNullable", ComparisonOperatorEnum.Equals, true }
    };

    public static TheoryData<string, ComparisonOperatorEnum, object?, int> DateOnlyComparatorData { get; } = new() {
        { "D", ComparisonOperatorEnum.Equals, DateOnly.FromDateTime(new(2025, 1, 1)), 1 },
        { "D", ComparisonOperatorEnum.NotEquals, DateOnly.FromDateTime(new(2025, 1, 1)), 1 },
        { "DNullable", ComparisonOperatorEnum.Equals, null, 1 },
        { "D", ComparisonOperatorEnum.GreaterThanOrEqual, DateOnly.FromDateTime(new(2025, 1, 1)), 2 },
        { "D", ComparisonOperatorEnum.LessThan, DateOnly.FromDateTime(new(2025, 1, 2)), 1 },
        { "D", ComparisonOperatorEnum.LessThanOrEqual, DateOnly.FromDateTime(new(2025, 1, 1)), 1 },
        { "D", ComparisonOperatorEnum.In, new object[] { DateOnly.FromDateTime(new(2025, 1, 1)) }, 1 },
        { "D", ComparisonOperatorEnum.NotIn, new object[] { DateOnly.FromDateTime(new(2025, 1, 1)) }, 1 }
    };

    public static TheoryData<string, ComparisonOperatorEnum, object?, int> TimeOnlyComparatorData { get; } = new() {
        { "T", ComparisonOperatorEnum.Equals, TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)), 1 },
        { "T", ComparisonOperatorEnum.NotEquals, TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)), 1 },
        { "TNullable", ComparisonOperatorEnum.Equals, null, 1 },
        { "T", ComparisonOperatorEnum.GreaterThan, TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)), 1 },
        { "T", ComparisonOperatorEnum.LessThan, TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)), 1 },
        { "T", ComparisonOperatorEnum.LessThanOrEqual, TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)), 1 },
        { "T", ComparisonOperatorEnum.In, new object[] { TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)) }, 1 },
        { "T", ComparisonOperatorEnum.NotIn, new object[] { TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)) }, 1 }
    };

    public static TheoryData<string, ComparisonOperatorEnum, object, int> DateTimeComparatorData { get; } = new() {
        { "Ts", ComparisonOperatorEnum.Equals, new DateTime(2025, 1, 1, 9, 0, 0), 1 },
        { "Ts", ComparisonOperatorEnum.NotEquals, new DateTime(2025, 1, 1, 9, 0, 0), 1 },
        { "Ts", ComparisonOperatorEnum.GreaterThan, new DateTime(2025, 1, 1, 9, 0, 0), 1 },
        { "Ts", ComparisonOperatorEnum.GreaterThanOrEqual, new DateTime(2025, 1, 1, 9, 0, 0), 2 },
        { "Ts", ComparisonOperatorEnum.LessThan, new DateTime(2025, 1, 2, 9, 0, 0), 1 },
        { "Ts", ComparisonOperatorEnum.LessThanOrEqual, new DateTime(2025, 1, 1, 9, 0, 0), 1 },
        { "Ts", ComparisonOperatorEnum.In, new object[] { new DateTime(2025, 1, 1, 9, 0, 0) }, 1 },
        { "Ts", ComparisonOperatorEnum.NotIn, new object[] { new DateTime(2025, 1, 1, 9, 0, 0) }, 1 }
    };

    public static TheoryData<string, ComparisonOperatorEnum, object?, int> BoolComparatorData { get; } = new() {
        { "IsActive", ComparisonOperatorEnum.Equals, true, 1 },
        { "IsActive", ComparisonOperatorEnum.NotEquals, true, 2 },
        { "IsActive", ComparisonOperatorEnum.In, new object[] { true }, 1 },
        { "IsActive", ComparisonOperatorEnum.NotIn, new object[] { true }, 2 },
        { "IsActiveNullable", ComparisonOperatorEnum.Equals, null, 1 },
        { "IsActiveNullable", ComparisonOperatorEnum.Equals, true, 1 },
        { "IsActiveNullable", ComparisonOperatorEnum.NotEquals, true, 2 }
    };
}