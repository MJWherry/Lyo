using System.Linq.Expressions;
using Lyo.Common.Core;

namespace Lyo.Common.Tests;

public class LyoReflectionTests
{
    private sealed class Address
    {
        public string? City { get; set; }
        public int Zip { get; set; }
    }

    private sealed class Person
    {
        public Guid? PlaceOfBirthAddressId { get; set; }
        public Address? Address { get; set; }
        public string? Name { get; set; }
        public List<string>? Tags { get; set; }
        public string this[int i] => Name ?? "";
        public static readonly Person Catalog = new() { Name = "catalog" };
        public static Person OtherCatalog = new() { Name = "other" };
    }

    [Fact]
    public void FindProperty_IgnoreCase_SkipsIndexer()
    {
        Assert.Equal("Name", typeof(Person).FindProperty("name")!.Name);
        Assert.Null(typeof(Person).FindProperty("Item"));
    }

    [Fact]
    public void GetPropertyValue_DottedPath_ReadsNested()
    {
        var person = new Person { Address = new() { City = "Boston" } };
        Assert.Equal("Boston", person.GetPropertyValue("Address.City"));
        Assert.Equal("Boston", person.GetPropertyValue("address.city"));
        Assert.Null(person.GetPropertyValue("Address.Missing"));
        Assert.Null(((object?)null).GetPropertyValue("Name"));
    }

    [Fact]
    public void TryGetPropertyValue_MissOrNull_ReturnsFalse()
    {
        var person = new Person { Name = "Ada" };
        Assert.True(person.TryGetPropertyValue("Name", out var name));
        Assert.Equal("Ada", name);
        Assert.False(person.TryGetPropertyValue("Missing", out _));
        Assert.False(person.TryGetPropertyValue("Address.City", out _));
    }

    [Fact]
    public void GetPropertyValue_ConvertWrappedGuid_FromLambdaPath()
    {
        Expression<Func<Person, object?>> expr = x => x.PlaceOfBirthAddressId;
        var id = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var person = new Person { PlaceOfBirthAddressId = id };
        Assert.Equal("PlaceOfBirthAddressId", expr.TryGetMemberPath());
        Assert.Equal(id, person.GetPropertyValue(expr.GetMemberPath()));
    }

    [Fact]
    public void GetMemberPath_Nested_ReturnsDotted()
    {
        Expression<Func<Person, string?>> expr = x => x.Address!.City;
        Assert.Equal("Address.City", expr.GetMemberPath());
        Assert.Equal("City", expr.GetMemberName());
    }

    [Fact]
    public void TrySetPropertyValue_ConvertsAndWrites()
    {
        var person = new Person { Address = new() };
        Assert.True(person.TrySetPropertyValue("Address.Zip", "42"));
        Assert.Equal(42, person.Address!.Zip);
        person.SetPropertyValue("Name", "Ada");
        Assert.Equal("Ada", person.Name);
        Assert.False(person.TrySetPropertyValue("Missing", 1));
    }

    [Fact]
    public void HasPath_ResolvePath_GetPathType()
    {
        Assert.True(typeof(Person).HasPath("Address.City"));
        Assert.False(typeof(Person).HasPath("Address.Missing"));
        var chain = typeof(Person).ResolvePath("Address.City");
        Assert.Equal(2, chain!.Count);
        Assert.Equal(typeof(string), typeof(Person).GetPathType("Address.City"));
        Assert.Equal(typeof(Guid?), typeof(Person).GetPathType("PlaceOfBirthAddressId"));
    }

    [Fact]
    public void IsScalar_IsSimple_Matrix()
    {
        Assert.True(typeof(int).IsScalar());
        Assert.True(typeof(int?).IsScalar());
        Assert.True(typeof(Guid).IsScalar());
        Assert.True(typeof(DateOnly).IsScalar());
        Assert.True(typeof(Uri).IsScalar());
        Assert.False(typeof(byte[]).IsScalar());
        Assert.True(typeof(byte[]).IsSimple());
        Assert.False(typeof(List<string>).IsScalar());
        Assert.False(typeof(Person).IsScalar());
        Assert.True(Utilities.IsScalarType(typeof(DateTimeOffset)));
    }

    [Fact]
    public void ScalarProperties_SkipsCollectionsAndIndexers()
    {
        var names = typeof(Person).ScalarProperties().Select(p => p.Name).ToArray();
        Assert.Contains("Name", names);
        Assert.Contains("PlaceOfBirthAddressId", names);
        Assert.DoesNotContain("Tags", names);
        Assert.DoesNotContain("Address", names);
        Assert.DoesNotContain("Item", names);
    }

    [Fact]
    public void PublicStaticFields_CatalogPattern()
    {
        var fields = typeof(Person).PublicStaticFields<Person>();
        Assert.Equal(2, fields.Count);
        Assert.Contains(fields, p => p.Name == "catalog");
        Assert.Contains(fields, p => p.Name == "other");
    }

    [Fact]
    public void DefaultValue_And_TryCreateInstance()
    {
        Assert.Equal(0, typeof(int).DefaultValue());
        Assert.Null(typeof(string).DefaultValue());
        Assert.True(typeof(Person).TryCreateInstance(out var created));
        Assert.IsType<Person>(created);
        Assert.False(typeof(IDisposable).TryCreateInstance(out _));
    }

    [Fact]
    public void GetAttribute_HasAttribute()
    {
        var prop = typeof(Person).FindProperty("Name")!;
        Assert.False(prop.HasAttribute<ObsoleteAttribute>());
        Assert.Null(prop.GetAttribute<ObsoleteAttribute>());
    }

    [Fact]
    public void GetFieldValue_And_TryInvoke()
    {
        var person = new Person { Name = "Ada" };
        Assert.True(person.TryInvoke(nameof(object.ToString), [], [], out var text));
        Assert.Equal(person.ToString(), text);
        Assert.Equal("catalog", ((Person)typeof(Person).FindField(nameof(Person.Catalog))!.GetValue(null)!).Name);
        Assert.True(typeof(Person).FindField(nameof(Person.Catalog))!.IsPublicStatic());
    }

    [Fact]
    public void UtilitiesWrappers_DelegateToLyoReflection()
    {
        Expression<Func<Person, object?>> boxed = x => x.PlaceOfBirthAddressId;
        Assert.Equal("PlaceOfBirthAddressId", Utilities.GetPropertyName(boxed));
        Assert.Equal("PlaceOfBirthAddressId", Utilities.GetPropertyPath(boxed));
        Expression<Func<Person, string?>> nested = x => x.Address!.City;
        Assert.Equal("Address.City", Utilities.GetPropertyPath(nested));
    }
}
