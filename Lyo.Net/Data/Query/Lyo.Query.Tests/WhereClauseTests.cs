using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;
using Lyo.Query.Models.Exceptions;

namespace Lyo.Query.Tests;

public class WhereClauseTests : WhereClauseServiceTests
{
    [Theory]
    [MemberData(nameof(TestComparatorsData.StringComparatorData), MemberType = typeof(TestComparatorsData))]
    public void String_WhereClause_Comparators_Param(string property, ComparisonOperatorEnum comparator, object value, int expected)
    {
        var svc = CreateService();
        var people = new List<Person> { new PersonBuilder().WithName("Alice").Build(), new PersonBuilder().WithName("alice").Build(), new PersonBuilder().WithName("Bob").Build() }
            .AsQueryable();

        var node = WhereClauseBuilder.Condition(property, comparator, value);
        var res = svc.ApplyWhereClause(people, node).ToList();
        Assert.Equal(expected, res.Count);
    }

    [Theory]
    [MemberData(nameof(TestComparatorsData.IntComparatorData), MemberType = typeof(TestComparatorsData))]
    public void Int_WhereClause_IncludingNullable_Param(string property, ComparisonOperatorEnum comparator, object value, int expected)
    {
        var svc = CreateService();
        var people = new List<Person> {
            new PersonBuilder().WithAge(10).WithAgeNullable(10).WithName("A").Build(),
            new PersonBuilder().WithAge(20).WithAgeNullable(null).WithName("B").Build(),
            new PersonBuilder().WithAge(30).WithAgeNullable(30).WithName("C").Build()
        }.AsQueryable();

        var node = WhereClauseBuilder.Condition(property, comparator, value);
        var res = svc.ApplyWhereClause(people, node).ToList();
        Assert.Equal(expected, res.Count);
    }

    [Theory]
    [MemberData(nameof(TestComparatorsData.GuidComparatorData), MemberType = typeof(TestComparatorsData))]
    public void Guid_WhereClause_IncludingNullable_Param(string property, ComparisonOperatorEnum comparator, bool needsValue)
    {
        var svc = CreateService();
        var g1 = Guid.NewGuid();
        var g2 = Guid.NewGuid();
        var people = new List<Person> {
            new PersonBuilder().WithId(g1).WithIdNullable(g1).WithName("A").Build(), new PersonBuilder().WithId(g2).WithIdNullable(null).WithName("B").Build()
        }.AsQueryable();

        var value = needsValue ? (object)g1 : new object[] { g2 };
        var node = WhereClauseBuilder.Condition(property, comparator, value);
        var res = svc.ApplyWhereClause(people, node).ToList();
        Assert.Single(res);
    }

    [Fact]
    public void Guid_WhereClause_Contains_PartialString_UsesCanonicalStringForm()
    {
        var svc = CreateService();
        var g = Guid.Parse("f1111111-1111-1111-1111-111111111111");
        var people = new List<Person> { new PersonBuilder().WithId(g).WithName("A").Build() }.AsQueryable();
        var node = WhereClauseBuilder.Condition("Id", ComparisonOperatorEnum.Contains, "1111");
        var res = svc.ApplyWhereClause(people, node).ToList();
        Assert.Single(res);
    }

    [Fact]
    public void Guid_WhereClause_StartsWith_StringPrefix_Matches()
    {
        var svc = CreateService();
        var g = Guid.Parse("abcdef12-3456-7890-abcd-ef1234567890");
        var people = new List<Person> { new PersonBuilder().WithId(g).WithName("A").Build() }.AsQueryable();
        var node = WhereClauseBuilder.Condition("Id", ComparisonOperatorEnum.StartsWith, "abcdef12");
        var res = svc.ApplyWhereClause(people, node).ToList();
        Assert.Single(res);
    }

    [Theory]
    [MemberData(nameof(TestComparatorsData.DateOnlyComparatorData), MemberType = typeof(TestComparatorsData))]
    public void DateOnly_WhereClause_IncludingNullable_Param(string property, ComparisonOperatorEnum comparator, object? value, int expected)
    {
        var svc = CreateService();
        var d1 = DateOnly.FromDateTime(new(2025, 1, 1));
        var d2 = DateOnly.FromDateTime(new(2025, 1, 2));
        var people = new List<Person> {
            new PersonBuilder().WithD(d1).WithDNullable(d1).WithName("A").Build(), new PersonBuilder().WithD(d2).WithDNullable(null).WithName("B").Build()
        }.AsQueryable();

        var val = value ?? (comparator == ComparisonOperatorEnum.LessThan ? (object)d2 : d1);
        var node = WhereClauseBuilder.Condition(property, comparator, val);
        var res = svc.ApplyWhereClause(people, node).ToList();
        Assert.Equal(expected, res.Count);
    }

    [Theory]
    [MemberData(nameof(TestComparatorsData.TimeOnlyComparatorData), MemberType = typeof(TestComparatorsData))]
    public void TimeOnly_WhereClause_IncludingNullable_Param(string property, ComparisonOperatorEnum comparator, object? value, int expected)
    {
        var svc = CreateService();
        var t1 = TimeOnly.FromTimeSpan(TimeSpan.FromHours(9));
        var t2 = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10));
        var people = new List<Person> {
            new PersonBuilder().WithT(t1).WithTNullable(t1).WithName("A").Build(), new PersonBuilder().WithT(t2).WithTNullable(null).WithName("B").Build()
        }.AsQueryable();

        var val = value ?? t1;
        var node = WhereClauseBuilder.Condition(property, comparator, val);
        var res = svc.ApplyWhereClause(people, node).ToList();
        Assert.Equal(expected, res.Count);
    }

    [Theory]
    [MemberData(nameof(TestComparatorsData.DateTimeComparatorData), MemberType = typeof(TestComparatorsData))]
    public void DateTime_WhereClause_IncludingNullable_Param(string property, ComparisonOperatorEnum comparator, object value, int expected)
    {
        var svc = CreateService();
        var dt1 = new DateTime(2025, 1, 1, 9, 0, 0);
        var dt2 = new DateTime(2025, 1, 2, 10, 0, 0);
        var people = new List<Person> { new PersonBuilder().WithTs(dt1).WithName("A").Build(), new PersonBuilder().WithTs(dt2).WithName("B").Build() }.AsQueryable();
        var node = WhereClauseBuilder.Condition(property, comparator, value);
        var res = svc.ApplyWhereClause(people, node).ToList();
        Assert.Equal(expected, res.Count);
    }

    [Fact]
    public void WhereClause_Complex_AndOr_Works()
    {
        var svc = CreateService();
        var people = new List<Person> {
            new PersonBuilder().WithName("Ann").WithAge(30).Build(),
            new PersonBuilder().WithName("Ann").WithAge(20).Build(),
            new PersonBuilder().WithName("Bob").WithAge(30).Build()
        }.AsQueryable();

        var left = WhereClauseBuilder.And(b => {
            b.Equals("Name", "Ann");
            b.GreaterThanOrEqual("Age", 25);
        });

        var right = WhereClauseBuilder.And(b => {
            b.Equals("Name", "Bob");
            b.Equals("Age", 30);
        });

        var root = WhereClauseBuilder.Or(b => {
            b.Add(left);
            b.Add(right);
        });

        var res = svc.ApplyWhereClause(people, root).ToList();
        Assert.Equal(2, res.Count);
    }

    [Fact]
    public void WhereClause_DeeplyNested_Logic_Works()
    {
        var svc = CreateService();
        var people = new List<Person> {
            new PersonBuilder().WithName("A").WithAge(10).Build(),
            new PersonBuilder().WithName("A").WithAge(20).Build(),
            new PersonBuilder().WithName("B").WithAge(30).Build(),
            new PersonBuilder().WithName("C").WithAge(40).Build()
        }.AsQueryable();

        var inner = WhereClauseBuilder.And(b => {
            b.Equals("Name", "A");
            b.AddGroupOr(g => {
                g.GreaterThanOrEqual("Age", 15);
                g.LessThanOrEqual("Age", 12);
            });
        });

        var bGroup = WhereClauseBuilder.And(b => {
            b.Equals("Name", "B");
            b.Equals("Age", 30);
        });

        var root = WhereClauseBuilder.Or(b => {
            b.Add(inner);
            b.Add(bGroup);
            b.Equals("Name", "C");
        });

        var res = svc.ApplyWhereClause(people, root).ToList();
        Assert.Equal(4, res.Count);
    }

    [Fact]
    public void WhereClause_In_WithArrayInts_Works()
    {
        var svc = CreateService();
        var people = new List<Person> {
            new PersonBuilder().WithName("Alice").WithAge(30).WithTags("tag1", "other").Build(),
            new PersonBuilder().WithName("alice").WithAge(20).WithTags("tag2").Build(),
            new PersonBuilder().WithName("Bob").WithAge(25).WithTags("none").Build()
        }.AsQueryable();

        var node = new ConditionClause("Age", ComparisonOperatorEnum.In, new object[] { 25 });
        var res = svc.ApplyWhereClause(people, node).ToList();
        Assert.Single(res);
        Assert.Equal(25, res[0].Age);
    }

    [Theory]
    [MemberData(nameof(TestComparatorsData.BoolComparatorData), MemberType = typeof(TestComparatorsData))]
    public void Bool_Comparators_WhereClause(string property, ComparisonOperatorEnum comparator, object? value, int expected)
    {
        var svc = CreateService();
        var people = new List<Person> {
            new PersonBuilder().WithName("A").WithIsActive(true).WithIsActiveNullable(true).Build(),
            new PersonBuilder().WithName("B").WithIsActive(false).WithIsActiveNullable(null).Build(),
            new PersonBuilder().WithName("C").WithIsActive(false).WithIsActiveNullable(false).Build()
        }.AsQueryable();

        var node = WhereClauseBuilder.Condition(property, comparator, value);
        var res = svc.ApplyWhereClause(people, node).ToList();
        Assert.Equal(expected, res.Count);
    }

    [Fact]
    public void WhereClause_NotEqualsNull_OnNonNullableField_ThrowsInvalidQuery()
    {
        var svc = CreateService();
        var people = new List<Person> { new PersonBuilder().WithName("A").WithAge(10).Build() }.AsQueryable();
        var node = WhereClauseBuilder.Condition("Age", ComparisonOperatorEnum.NotEquals, null);
        Assert.Throws<InvalidQueryException>(() => svc.ApplyWhereClause(people, node).ToList());
    }

    [Fact]
    public void WhereClause_OrContainsOnSameField_WithRegexChars_PreservesContainsSemantics()
    {
        var svc = CreateService();
        var people = new List<Person> {
            new PersonBuilder().WithName("abc.def").Build(), new PersonBuilder().WithName("foo+bar").Build(), new PersonBuilder().WithName("nomatch").Build()
        }.AsQueryable();

        var node = WhereClauseBuilder.Or(b => {
            b.Contains("Name", "abc.def");
            b.Contains("Name", "foo+bar");
        });

        var res = svc.ApplyWhereClause(people, node).ToList();
        Assert.Equal(2, res.Count);
        Assert.Contains(res, p => p.Name == "abc.def");
        Assert.Contains(res, p => p.Name == "foo+bar");
    }

    [Fact]
    public void WhereClause_OrContainsOnCollectionField_PreservesSemantics()
    {
        var svc = CreateService();
        var people = new List<Person> {
            new PersonBuilder().WithName("A").WithTags("viol-3361", "x").Build(),
            new PersonBuilder().WithName("B").WithTags("75 § 3305", "x").Build(),
            new PersonBuilder().WithName("C").WithTags("other").Build()
        }.AsQueryable();

        var node = WhereClauseBuilder.Or(b => {
            b.Contains("Tags", "3361");
            b.Contains("Tags", "75 § 3305");
        });

        var res = svc.ApplyWhereClause(people, node).ToList();
        Assert.Equal(2, res.Count);
        Assert.Contains(res, p => p.Name == "A");
        Assert.Contains(res, p => p.Name == "B");
    }

    [Fact]
    public void WhereClause_ConditionWithSubClause_AppliesBoth()
    {
        var svc = CreateService();
        var people = new List<Person> {
            new PersonBuilder().WithName("A").WithAge(10).Build(), new PersonBuilder().WithName("B").WithAge(20).Build(), new PersonBuilder().WithName("C").WithAge(30).Build()
        }.AsQueryable();

        var root = WhereClauseBuilder.ConditionWithSubClause("Age", ComparisonOperatorEnum.GreaterThan, 5, WhereClauseBuilder.And(b => b.Equals("Name", "B")));
        var res = svc.ApplyWhereClause(people, root).ToList();
        Assert.Single(res);
        Assert.Equal("B", res[0].Name);
        Assert.Equal(20, res[0].Age);
    }

    [Fact]
    public void WhereClause_AddConditionWithSubClause_AppliesBoth()
    {
        var svc = CreateService();
        var people = new List<Person> {
            new PersonBuilder().WithName("A").WithAge(10).Build(), new PersonBuilder().WithName("B").WithAge(20).Build(), new PersonBuilder().WithName("C").WithAge(30).Build()
        }.AsQueryable();

        var root = WhereClauseBuilder.And().AddConditionWithSubClause("Age", ComparisonOperatorEnum.GreaterThan, 5, sub => sub.Equals("Name", "B")).Build();
        var res = svc.ApplyWhereClause(people, root).ToList();
        Assert.Single(res);
        Assert.Equal("B", res[0].Name);
        Assert.Equal(20, res[0].Age);
    }

    [Fact]
    public void WhereClause_ConditionWithSubClause_RootOnlyExcludesSubQuery()
    {
        var svc = CreateService();
        var people = new List<Person> {
            new PersonBuilder().WithName("A").WithAge(10).Build(), new PersonBuilder().WithName("B").WithAge(20).Build(), new PersonBuilder().WithName("C").WithAge(30).Build()
        }.AsQueryable();

        var root = WhereClauseBuilder.ConditionWithSubClause("Age", ComparisonOperatorEnum.GreaterThan, 15, WhereClauseBuilder.And(b => b.Equals("Name", "A")));
        var resRootOnly = svc.ApplyWhereClause(people, root, false).ToList();
        Assert.Equal(2, resRootOnly.Count);
        Assert.Contains(resRootOnly, p => p.Name == "B");
        Assert.Contains(resRootOnly, p => p.Name == "C");
        var resFull = svc.ApplyWhereClause(people, root).ToList();
        Assert.Empty(resFull);
    }

    [Fact]
    public void GetCollectionIncludePathsForWhereClause_ReturnsCollectionPathsFromSubQuery()
    {
        var svc = CreateService();
        var subQuery = WhereClauseBuilder.And(b => {
            b.Contains("Tags", "3361");
            b.Contains("Tags", "75 § 3305");
        });

        var paths = svc.GetCollectionIncludePathsForWhereClause<Person>(subQuery).ToList();
        Assert.Single(paths);
        Assert.Equal("Tags", paths[0]);
    }

    [Fact]
    public void GetCollectionIncludePathsForWhereClause_ReturnsEmpty_WhenNoCollectionPaths()
    {
        var svc = CreateService();
        var node = WhereClauseBuilder.And(b => {
            b.Equals("Name", "A");
            b.GreaterThan("Age", 5);
        });

        var paths = svc.GetCollectionIncludePathsForWhereClause<Person>(node).ToList();
        Assert.Empty(paths);
    }

    [Fact]
    public void GetCollectionIncludePathsForWhereClause_ReturnsEmpty_WhenNodeIsNull()
    {
        var svc = CreateService();
        var paths = svc.GetCollectionIncludePathsForWhereClause<Person>(null).ToList();
        Assert.Empty(paths);
    }

    [Fact]
    public void GetCollectionIncludePathsForWhereClause_CollectsFromNestedSubQueries()
    {
        var svc = CreateService();
        var innerSubQuery = WhereClauseBuilder.And(b => b.Contains("Tags", "nested"));
        var outerSubQuery = WhereClauseBuilder.ConditionWithSubClause(
            "Age", ComparisonOperatorEnum.GreaterThan, 0, WhereClauseBuilder.Or(b => {
                b.Equals("Name", "X");
                b.Add(innerSubQuery);
            }));

        var paths = svc.GetCollectionIncludePathsForWhereClause<Person>(outerSubQuery).ToList();
        Assert.Single(paths);
        Assert.Equal("Tags", paths[0]);
    }

    [Fact]
    public void GetCollectionIncludePathsForWhereClause_ReturnsDeduplicatedPaths()
    {
        var svc = CreateService();
        var subQuery = WhereClauseBuilder.Or(b => {
            b.Contains("Tags", "a");
            b.Contains("Tags", "b");
            b.Contains("Tags", "c");
        });

        var paths = svc.GetCollectionIncludePathsForWhereClause<Person>(subQuery).ToList();
        Assert.Single(paths);
        Assert.Equal("Tags", paths[0]);
    }

    [Fact]
    public void WhereClause_ConditionWithSubClause_WithCollectionField_AppliesBoth()
    {
        var svc = CreateService();
        var people = new List<Person> {
            new PersonBuilder().WithName("A").WithAge(10).WithTags("x").Build(),
            new PersonBuilder().WithName("B").WithAge(20).WithTags("match", "y").Build(),
            new PersonBuilder().WithName("C").WithAge(30).WithTags("z").Build()
        }.AsQueryable();

        var root = WhereClauseBuilder.ConditionWithSubClause("Age", ComparisonOperatorEnum.GreaterThan, 5, WhereClauseBuilder.And(b => b.Contains("Tags", "match")));
        var res = svc.ApplyWhereClause(people, root).ToList();
        Assert.Single(res);
        Assert.Equal("B", res[0].Name);
        Assert.Equal(20, res[0].Age);
        Assert.Contains("match", res[0].Tags);
    }

    [Fact]
    public void WhereClause_ConditionWithSubClause_EquivalentToFlatAnd()
    {
        var svc = CreateService();
        var people = new List<Person> {
            new PersonBuilder().WithName("A").WithAge(10).Build(), new PersonBuilder().WithName("B").WithAge(20).Build(), new PersonBuilder().WithName("C").WithAge(30).Build()
        }.AsQueryable();

        var withSubQuery = WhereClauseBuilder.ConditionWithSubClause("Age", ComparisonOperatorEnum.GreaterThan, 15, WhereClauseBuilder.And(b => b.Equals("Name", "B")));
        var flat = WhereClauseBuilder.And(b => {
            b.GreaterThan("Age", 15);
            b.Equals("Name", "B");
        });

        var resSubQuery = svc.ApplyWhereClause(people, withSubQuery).ToList();
        var resFlat = svc.ApplyWhereClause(people, flat).ToList();
        Assert.Equal(resFlat.Count, resSubQuery.Count);
        Assert.Equal(resFlat.Select(p => p.Name).ToList(), resSubQuery.Select(p => p.Name).ToList());
    }

    [Fact]
    public void WhereClause_LogicalWithSubQuery_AppliesBoth()
    {
        var svc = CreateService();
        var people = new List<Person> {
            new PersonBuilder().WithName("A").WithAge(10).Build(), new PersonBuilder().WithName("B").WithAge(20).Build(), new PersonBuilder().WithName("C").WithAge(30).Build()
        }.AsQueryable();

        var root = (GroupClause)WhereClauseBuilder.And(b => b.GreaterThan("Age", 5));
        root.SubClause = WhereClauseBuilder.And(b => b.Equals("Name", "B"));
        var res = svc.ApplyWhereClause(people, root).ToList();
        Assert.Single(res);
        Assert.Equal("B", res[0].Name);
    }

    [Fact]
    public void WhereClause_LogicalWithSubQuery_RootOnlyExcludesSubQuery()
    {
        var svc = CreateService();
        var people = new List<Person> {
            new PersonBuilder().WithName("A").WithAge(10).Build(), new PersonBuilder().WithName("B").WithAge(20).Build(), new PersonBuilder().WithName("C").WithAge(30).Build()
        }.AsQueryable();

        var root = (GroupClause)WhereClauseBuilder.And(b => b.GreaterThan("Age", 15));
        root.SubClause = WhereClauseBuilder.And(b => b.Equals("Name", "A"));
        var resRootOnly = svc.ApplyWhereClause(people, root, false).ToList();
        Assert.Equal(2, resRootOnly.Count);
        var resFull = svc.ApplyWhereClause(people, root).ToList();
        Assert.Empty(resFull);
    }

    [Fact]
    public void GroupClause_Equals_SameStructure_IsEqual()
    {
        var a = new GroupClause(GroupOperatorEnum.And, null,
            new ConditionClause("Name", ComparisonOperatorEnum.Equals, "x"),
            new ConditionClause("Age", ComparisonOperatorEnum.GreaterThan, 5));
        var b = new GroupClause(GroupOperatorEnum.And, null,
            new ConditionClause("Name", ComparisonOperatorEnum.Equals, "x"),
            new ConditionClause("Age", ComparisonOperatorEnum.GreaterThan, 5));
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void GroupClause_Equals_DifferentOperator_IsNotEqual()
    {
        var cond = new ConditionClause("Name", ComparisonOperatorEnum.Equals, "x");
        var a = new GroupClause(GroupOperatorEnum.And, null, cond);
        var b = new GroupClause(GroupOperatorEnum.Or, null, cond);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void GroupClause_Equals_NestedGroup_IsEqual()
    {
        var inner = new GroupClause(GroupOperatorEnum.Or, null,
            new ConditionClause("A", ComparisonOperatorEnum.Equals, 1),
            new ConditionClause("B", ComparisonOperatorEnum.Equals, 2));
        var a = new GroupClause(GroupOperatorEnum.And, null, inner);
        var b = new GroupClause(GroupOperatorEnum.And, null,
            new GroupClause(GroupOperatorEnum.Or, null,
                new ConditionClause("A", ComparisonOperatorEnum.Equals, 1),
                new ConditionClause("B", ComparisonOperatorEnum.Equals, 2)));
        Assert.Equal(a, b);
    }
}