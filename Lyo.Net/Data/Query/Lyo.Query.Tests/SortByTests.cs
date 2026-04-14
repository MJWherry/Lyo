using System.Linq.Expressions;
using Lyo.Common.Enums;
using Lyo.Query.Models.Attributes;
using Lyo.Query.Models.Common;

namespace Lyo.Query.Tests;

public class SortByTests : WhereClauseServiceTests
{
    [Fact]
    public void SortByProperty_Works_InMemory()
    {
        var svc = CreateService();
        var people = new List<Person> {
            new PersonBuilder().WithName("A").WithAge(40).Build(), new PersonBuilder().WithName("B").WithAge(20).Build(), new PersonBuilder().WithName("C").WithAge(30).Build()
        }.AsQueryable();

        var res = svc.SortByProperty(people, "Age", SortDirection.Asc).ToList();
        Assert.Equal([20, 30, 40], res.Select(p => p.Age));
    }

    [Fact]
    public void ApplyOrdering_MultipleSorts_Works()
    {
        var svc = CreateService();
        var people = new List<Person> {
            new PersonBuilder().WithName("B").WithAge(30).Build(), new PersonBuilder().WithName("A").WithAge(30).Build(), new PersonBuilder().WithName("C").WithAge(20).Build()
        }.AsQueryable();

        var sortProps = new[] { new SortBy("Age", SortDirection.Desc, 1), new SortBy("Name", SortDirection.Asc, 2) };
        var defaultOrder = (Expression<Func<Person, object?>>)(p => p.Name);
        var res = svc.ApplyOrdering(people, sortProps, defaultOrder, SortDirection.Asc).ToList();
        Assert.Equal(["A", "B", "C"], res.Select(p => p.Name));
    }

    [Fact]
    public void ApplyOrdering_DefaultOrder_Applied_WhenNoSorts()
    {
        var svc = CreateService();
        var people = new List<Person> { new PersonBuilder().WithName("B").WithAge(2).Build(), new PersonBuilder().WithName("A").WithAge(1).Build() }.AsQueryable();
        var res = svc.ApplyOrdering(people, [], p => p.Name, SortDirection.Asc).ToList();
        Assert.Equal(["A", "B"], res.Select(r => r.Name));
    }

    [Fact]
    public void SortByProperty_String_Desc_Works()
    {
        var svc = CreateService();
        var people = new List<Person> { new PersonBuilder().WithName("A").Build(), new PersonBuilder().WithName("C").Build(), new PersonBuilder().WithName("B").Build() }
            .AsQueryable();

        var res = svc.SortByProperty(people, "Name", SortDirection.Desc).ToList();
        Assert.Equal(["C", "B", "A"], res.Select(p => p.Name));
    }

    [Fact]
    public void SortByProperty_Guid_Works()
    {
        var svc = CreateService();
        var g1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var g2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var g3 = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var people = new List<Person> {
            new PersonBuilder().WithId(g2).WithName("B").Build(), new PersonBuilder().WithId(g3).WithName("C").Build(), new PersonBuilder().WithId(g1).WithName("A").Build()
        }.AsQueryable();

        var res = svc.SortByProperty(people, "Id", SortDirection.Asc).ToList();
        Assert.Equal([g1, g2, g3], res.Select(p => p.Id));
    }

    [Fact]
    public void SortByProperty_Bool_Works()
    {
        var svc = CreateService();
        var people = new List<Person> {
            new PersonBuilder().WithName("A").WithIsActive(false).Build(),
            new PersonBuilder().WithName("B").WithIsActive(true).Build(),
            new PersonBuilder().WithName("C").WithIsActive(false).Build()
        }.AsQueryable();

        var desc = svc.SortByProperty(people, "IsActive", SortDirection.Desc).ToList();
        Assert.Equal([true, false, false], desc.Select(p => p.IsActive));
        var asc = svc.SortByProperty(people, "IsActive", SortDirection.Asc).ToList();
        Assert.Equal([false, false, true], asc.Select(p => p.IsActive));
    }

    [Fact]
    public void SortByProperty_DateTime_DateOnly_TimeOnly_Work()
    {
        var svc = CreateService();
        var dt1 = new DateTime(2025, 1, 1, 9, 0, 0);
        var dt2 = new DateTime(2025, 1, 2, 9, 0, 0);
        var d1 = DateOnly.FromDateTime(new(2025, 1, 1));
        var d2 = DateOnly.FromDateTime(new(2025, 1, 2));
        var t1 = TimeOnly.FromTimeSpan(TimeSpan.FromHours(9));
        var t2 = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10));
        var people = new List<Person> {
            new PersonBuilder().WithTs(dt2).WithD(d2).WithT(t2).WithName("B").Build(), new PersonBuilder().WithTs(dt1).WithD(d1).WithT(t1).WithName("A").Build()
        }.AsQueryable();

        var rDt = svc.SortByProperty(people, "Ts", SortDirection.Asc).ToList();
        Assert.Equal([dt1, dt2], rDt.Select(p => p.Ts));
        var rD = svc.SortByProperty(people, "D", SortDirection.Asc).ToList();
        Assert.Equal([d1, d2], rD.Select(p => p.D));
        var rT = svc.SortByProperty(people, "T", SortDirection.Asc).ToList();
        Assert.Equal([t1, t2], rT.Select(p => p.T));
    }

    [Fact]
    public void ApplyOrdering_MixedTypes_Works()
    {
        var svc = CreateService();
        var g1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var g2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var people = new List<Person> {
            new PersonBuilder().WithName("B").WithAge(30).WithId(g2).Build(),
            new PersonBuilder().WithName("A").WithAge(30).WithId(g1).Build(),
            new PersonBuilder().WithName("C").WithAge(20).WithId(g1).Build()
        }.AsQueryable();

        var sortProps = new[] { new SortBy("Age", SortDirection.Desc, 1), new SortBy("Id", SortDirection.Asc, 2), new SortBy("Name", SortDirection.Asc, 3) };
        var res = svc.ApplyOrdering(people, sortProps, p => p.Name, SortDirection.Asc).ToList();
        Assert.Equal(["A", "B", "C"], res.Select(p => p.Name));
    }

    [Fact]
    public void Sorting_QueryPropertyNameAttribute_ResolvesPropertyPath()
    {
        var svc = CreateService();
        var entities = new List<SortableWithQueryProp> { new() { OrderBy = "z" }, new() { OrderBy = "a" }, new() { OrderBy = "m" } }.AsQueryable();

        // Saved sort uses "SortKey" (QueryPropertyName), resolves to OrderBy property
        var res = svc.SortByProperty(entities, "SortKey", SortDirection.Asc).ToList();
        Assert.Equal(["a", "m", "z"], res.Select(e => e.OrderBy));
    }

    [Fact]
    public void Sorting_QueryPropertyNameAttribute_WorksWithApplyOrdering()
    {
        var svc = CreateService();
        var entities = new List<SortableWithQueryProp> { new() { OrderBy = "b", Id = 2 }, new() { OrderBy = "a", Id = 2 }, new() { OrderBy = "a", Id = 1 } }.AsQueryable();
        var sortProps = new[] { new SortBy("SortKey", SortDirection.Asc, 1), new SortBy("Id", SortDirection.Asc, 2) };
        var res = svc.ApplyOrdering(entities, sortProps, e => e.OrderBy, SortDirection.Asc).ToList();
        Assert.Equal("a", res[0].OrderBy);
        Assert.Equal(1, res[0].Id);
        Assert.Equal("a", res[1].OrderBy);
        Assert.Equal(2, res[1].Id);
        Assert.Equal("b", res[2].OrderBy);
    }

    private sealed class SortableWithQueryProp
    {
        [QueryPropertyName("SortKey")]
        public string OrderBy { get; set; } = "";

        public int Id { get; set; }
    }
}