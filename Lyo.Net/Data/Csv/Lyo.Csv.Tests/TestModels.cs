using CsvHelper.Configuration;

// ReSharper disable once CheckNamespace
namespace Lyo.Csv.Tests.TestModels;

internal sealed class Person
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public int Age { get; set; }
}

internal sealed class PersonName
{
    public string? Name { get; set; }
}

internal sealed class PersonNameMap : ClassMap<PersonName>
{
    public PersonNameMap() => Map(m => m.Name).Name("Full Name");
}