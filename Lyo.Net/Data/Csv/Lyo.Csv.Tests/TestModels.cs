using Lyo.Csv.Models;

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
    [CsvColumn("Full Name")]
    public string? Name { get; set; }
}