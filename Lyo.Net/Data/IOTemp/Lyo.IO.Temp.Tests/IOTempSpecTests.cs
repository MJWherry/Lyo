using Lyo.Common.Records;
using Lyo.IO.Temp.Models;

namespace Lyo.IO.Temp.Tests;

public sealed class IOTempSpecTests : IDisposable
{
    private readonly IOTempService _service = new(TestOptions());

    public void Dispose() => _service.Dispose();

    private static IOTempServiceOptions TestOptions() => new() { TempRoot = Path.Combine(Path.GetTempPath(), "lyo-io-spec-tests"), DirectoryName = Guid.NewGuid().ToString("N") };

    [Fact]
    public void Builder_creates_spec_with_files()
    {
        var spec = TempDirectorySpec.Builder().WithFiles(5, 1024).Build();
        Assert.Equal(5, spec.FileCount);
        Assert.Equal(1024, spec.FileSizeBytes);
        Assert.Null(spec.FileSizeSelector);
        Assert.Null(spec.Subdirectories);
    }

    [Fact]
    public void Builder_creates_spec_with_file_size_unit()
    {
        var spec = TempDirectorySpec.Builder().WithFiles(3, FileSizeUnitInfo.Kilobyte, 4).Build();
        Assert.Equal(3, spec.FileCount);
        Assert.Equal(4096, spec.FileSizeBytes);
    }

    [Fact]
    public void Builder_creates_spec_with_file_size_selector()
    {
        var spec = TempDirectorySpec.Builder().WithFiles(2, 512).WithFileSizeSelector(i => (i + 1) * 1024).Build();
        Assert.NotNull(spec.FileSizeSelector);
        Assert.Equal(1024, spec.FileSizeSelector(0));
        Assert.Equal(2048, spec.FileSizeSelector(1));
    }

    [Fact]
    public void Builder_creates_spec_with_subdirectories()
    {
        var spec = TempDirectorySpec.Builder().WithFiles(2, 512).WithSubdirectory(sub => sub.WithFiles(3, 256)).WithSubdirectory(TempDirectorySpec.Flat(1, 128)).Build();
        Assert.NotNull(spec.Subdirectories);
        Assert.Equal(2, spec.Subdirectories!.Count);
        Assert.Equal(3, spec.Subdirectories[0].FileCount);
        Assert.Equal(1, spec.Subdirectories[1].FileCount);
    }

    [Fact]
    public void Random_creates_files_within_count_and_size_range()
    {
        using var session = _service.CreateSession();
        var spec = TempDirectorySpec.Random(3, 5, 64, 256);
        session.Generator.SimulateDirectory(spec);
        Assert.InRange(session.Files.Count, 3, 5);
        Assert.All(session.Files, f => Assert.InRange(new FileInfo(f).Length, 64, 256));
    }

    [Fact]
    public void Random_with_FileSizeUnitInfo_creates_correct_range()
    {
        using var session = _service.CreateSession();
        var spec = TempDirectorySpec.Random(2, 4, FileSizeUnitInfo.Byte, 64, FileSizeUnitInfo.Byte, 128);
        session.Generator.SimulateDirectory(spec);
        Assert.InRange(session.Files.Count, 2, 4);
        Assert.All(session.Files, f => Assert.InRange(new FileInfo(f).Length, 64, 128));
    }

    [Fact]
    public void FileSizeSelector_controls_per_file_size()
    {
        using var session = _service.CreateSession();
        var sizes = new long[] { 100, 200, 300 };
        var spec = new TempDirectorySpec { FileCount = 3, FileSizeBytes = 0, FileSizeSelector = i => sizes[i] };
        session.Generator.SimulateDirectory(spec);
        var files = session.Files.OrderBy(f => new FileInfo(f).Length).ToList();
        Assert.Equal(3, files.Count);
        Assert.Equal(100, new FileInfo(files[0]).Length);
        Assert.Equal(200, new FileInfo(files[1]).Length);
        Assert.Equal(300, new FileInfo(files[2]).Length);
    }
}