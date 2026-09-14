using Lyo.Api.FileStorage.Models;
using Lyo.FileStorage.Web.Components.FileStorageManagement;
using Lyo.IO.FileSystem;

namespace Lyo.FileStorage.Web.Components.Tests;

public sealed class FileStoragePresenceUiTests
{
    [Fact]
    public void TryChip_FileBoth_IsReadable()
    {
        var entry = new FileSystemEntry(
            "/a", "a", false, 0, DateTimeOffset.MinValue, DateTimeOffset.MinValue,
            new Dictionary<string, string> { ["Presence"] = nameof(FileStoragePresence.Both) });
        Assert.True(FileStoragePresenceUi.TryChip(entry, out var label, out var hint, out _, out _));
        Assert.Equal("Both", label);
        Assert.Contains("catalog", hint, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("disk", hint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryChip_Directory_Hidden()
    {
        var entry = new FileSystemEntry(
            "/report", "report", true, 0, DateTimeOffset.MinValue, DateTimeOffset.MinValue,
            new Dictionary<string, string> {
                ["Presence"] = nameof(FileStoragePresence.Physical),
                ["OriginalFileName"] = "report"
            });
        Assert.False(FileStoragePresenceUi.TryChip(entry, out _, out _, out _, out _));
    }

    [Theory]
    [InlineData(FileStoragePresence.Store, true)]
    [InlineData(FileStoragePresence.Both, true)]
    [InlineData(FileStoragePresence.None, true)]
    [InlineData(FileStoragePresence.Physical, false)]
    public void InCatalog_PhysicalOnly_IsFalse(FileStoragePresence presence, bool expected)
    {
        var fileId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Assert.Equal(expected, FileStoragePresenceUi.InCatalog(presence, fileId));
    }
}
