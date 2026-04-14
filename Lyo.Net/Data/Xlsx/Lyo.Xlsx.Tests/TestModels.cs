

// ReSharper disable once CheckNamespace
namespace Lyo.Xlsx.Tests.TestModels;

internal class DateNumberModel
{
    public DateTime Date { get; init; }

    public decimal DecimalValue { get; init; }

    public double DoubleValue { get; init; }

    public bool Flag { get; init; }
}

internal class TestModel
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public int Age { get; set; }
}