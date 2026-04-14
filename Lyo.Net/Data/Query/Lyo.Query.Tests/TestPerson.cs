namespace Lyo.Query.Tests;

// Test Person used for query filter/sort tests (no EF/navigation dependencies)
public class Person
{
    public string Name { get; set; } = string.Empty;

    public int Age { get; set; }

    public int? AgeNullable { get; set; }

    public List<string> Tags { get; set; } = new();

    public bool IsActive { get; set; }

    public bool? IsActiveNullable { get; set; }

    public Guid Id { get; set; }

    public Guid? IdNullable { get; set; }

    public DateTime Ts { get; set; }

    public DateOnly D { get; set; }

    public DateOnly? DNullable { get; set; }

    public TimeOnly T { get; set; }

    public TimeOnly? TNullable { get; set; }
}