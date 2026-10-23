using Lyo.FileMetadataStore;
using Lyo.FileMetadataStore.Models;

namespace Lyo.FileStorage.Tests;

public class FileTagGrouperTests
{
    [Fact]
    public void Parse_ImportedYear_IsDateTimeYear()
    {
        var key = FileTagGroupKey.Parse("imported:year");
        Assert.Equal(FileTagTypes.Imported, key.TagType);
        Assert.Equal(FileTagValueKind.DateTime, key.Kind);
        Assert.Equal(FileTagGrain.Year, key.Grain);
    }

    [Fact]
    public void Parse_DecimalAndTimespanHours_ReadKindAndGrain()
    {
        var amount = FileTagGroupKey.Parse("amount:decimal");
        var sla = FileTagGroupKey.Parse("sla:timespan:hours");
        Assert.Equal(FileTagValueKind.Number, amount.Kind);
        Assert.Equal(FileTagGrain.Value, amount.Grain);
        Assert.Equal(FileTagValueKind.TimeSpan, sla.Kind);
        Assert.Equal(FileTagGrain.Hours, sla.Grain);
    }

    [Fact]
    public void ListGroup_Guid_UsesCanonicalValue()
    {
        var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var first = File();
        var second = File();
        var tags = new FileTag[] { new(Guid.NewGuid(), first.Id, "owner", id.ToString("D"), "", false), new(Guid.NewGuid(), second.Id, "owner", id.ToString("N"), "", false) };
        var page = FileTagGrouper.Group([FileTagGroupKey.Parse("owner:guid")], null, [first, second], tags, 10);
        var folder = Assert.Single(page.Entries);
        Assert.Equal(id.ToString("D"), folder.Name);
        Assert.True(folder.IsDirectory);
    }

    [Fact]
    public void ListGroup_Decimal_SortsNumerically()
    {
        var ten = File();
        var two = File();
        var tags = new FileTag[] { new(Guid.NewGuid(), ten.Id, "amount", "10", "", false), new(Guid.NewGuid(), two.Id, "amount", "2", "", false) };
        var page = FileTagGrouper.Group([FileTagGroupKey.Parse("amount:decimal")], null, [ten, two], tags, 10);
        Assert.Equal(["2", "10"], page.Entries.Select(static entry => entry.Name).ToArray());
    }

    [Fact]
    public void ListGroup_TimeSpanDaysThenHours_NestsRemainder()
    {
        var file = File();
        var tags = new FileTag[] { new(Guid.NewGuid(), file.Id, "sla", "1.03:00:00", "", false) };
        var keys = new[] { FileTagGroupKey.Parse("sla:timespan:days"), FileTagGroupKey.Parse("sla:timespan:hours") };
        var root = FileTagGrouper.Group(keys, null, [file], tags, 10);
        var day = Assert.Single(root.Entries);
        Assert.Equal("1", day.Name);
        var nested = FileTagGrouper.Group(keys, "1", [file], tags, 10);
        var hour = Assert.Single(nested.Entries);
        Assert.Equal("03", hour.Name);
        Assert.Equal("1/03", hour.VirtualPath);
    }

    private static FileStoreResult File()
        => new(Guid.NewGuid(), "report.pdf", 3, [1], "report.pdf", 3, [1], false, null, null, null, false, null, null, null, null, null, null, null, null, DateTime.UtcNow);
}