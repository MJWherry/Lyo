using System.IO.Compression;
using Lyo.Common.Metadata.Records;
using Lyo.IO.Temp.Models;

namespace Lyo.IO.Temp.Tests;

public sealed class IOTempFileGeneratorTests
{
    private static IOTempServiceOptions GetTestOptions()
        => new() { TempRoot = Path.Combine(Path.GetTempPath(), "lyo-io-temp-tests"), DirectoryName = Guid.NewGuid().ToString("N") };

    [Fact]
    public void Session_Exposes_Generator()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            Assert.NotNull(session.Generator);
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Theory]
    [InlineData(1024)]
    [InlineData(0)]
    public void CreateRandomFile_RequestedSize_CreatesFile(int size)
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var path = session.Generator.CreateRandomFile(size);
            Assert.True(File.Exists(path));
            Assert.Equal(size, new FileInfo(path).Length);
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void CreateRandomFile_WithFileSizeUnitInfo_CreatesCorrectSize()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var path = session.Generator.CreateRandomFile(FileSizeUnitInfo.Kilobyte, 4);
            Assert.True(File.Exists(path));
            Assert.Equal(FileSizeUnitInfo.Kilobyte.ConvertToBytes(4), new FileInfo(path).Length);
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void CreateRandomFile_WithName_UsesGivenName()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var path = session.Generator.CreateRandomFile(512, "named.bin");
            Assert.True(File.Exists(path));
            Assert.EndsWith("named.bin", path);
            Assert.Equal(512, new FileInfo(path).Length);
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void CreateRandomFile_Registers_InSessionFiles()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var path = session.Generator.CreateRandomFile(256);
            Assert.Single(session.Files);
            Assert.Contains(path, session.Files);
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void CreateRandomFile_Content_IsNotAllZeros()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var path = session.Generator.CreateRandomFile(1024);
            var bytes = File.ReadAllBytes(path);
            Assert.Contains(bytes, b => b != 0);
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public async Task CreateRandomFileAsync_Creates_FileWithCorrectSize()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            await using var session = service.CreateSession();
            var path = await session.Generator.CreateRandomFileAsync(2048, null, TestContext.Current.CancellationToken);
            Assert.True(File.Exists(path));
            Assert.Equal(2048, new FileInfo(path).Length);
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public async Task CreateRandomFileAsync_WithFileSizeUnitInfo_CreatesCorrectSize()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            await using var session = service.CreateSession();
            var path = await session.Generator.CreateRandomFileAsync(FileSizeUnitInfo.Kilobyte, 8, null, TestContext.Current.CancellationToken);
            Assert.Equal(FileSizeUnitInfo.Kilobyte.ConvertToBytes(8), new FileInfo(path).Length);
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void CreateRandomFiles_Creates_CorrectCountAndSize()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var paths = session.Generator.CreateRandomFiles(5, 512);
            Assert.Equal(5, paths.Count);
            Assert.Equal(5, session.Files.Count);
            Assert.All(
                paths, p => {
                    Assert.True(File.Exists(p));
                    Assert.Equal(512, new FileInfo(p).Length);
                });
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void CreateRandomFiles_WithFileSizeUnitInfo_CreatesCorrectSizes()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var expectedSize = FileSizeUnitInfo.Kilobyte.ConvertToBytes(1);
            var paths = session.Generator.CreateRandomFiles(3, FileSizeUnitInfo.Kilobyte, 1);
            Assert.Equal(3, paths.Count);
            Assert.All(paths, p => Assert.Equal(expectedSize, new FileInfo(p).Length));
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateRandomFiles_WithInvalidCount_Throws(int count)
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            Assert.ThrowsAny<ArgumentOutOfRangeException>(() => session.Generator.CreateRandomFiles(count, 256));
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public async Task CreateRandomFilesAsync_Creates_CorrectCount()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            await using var session = service.CreateSession();
            var paths = await session.Generator.CreateRandomFilesAsync(4, 128, TestContext.Current.CancellationToken);
            Assert.Equal(4, paths.Count);
            Assert.All(paths, p => Assert.Equal(128, new FileInfo(p).Length));
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void SimulateDirectory_Creates_DirectoryWithFiles()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var dirPath = session.Generator.SimulateDirectory(3, 256);
            Assert.True(Directory.Exists(dirPath));
            var files = Directory.GetFiles(dirPath);
            Assert.Equal(3, files.Length);
            Assert.All(files, f => Assert.Equal(256, new FileInfo(f).Length));
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void SimulateDirectory_WithSpecCreatesDirectoryAnd_RegistersTracking()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var spec = TempDirectorySpec.Flat(4, 128);
            var dirPath = session.Generator.SimulateDirectory(spec);
            Assert.True(Directory.Exists(dirPath));
            Assert.Contains(dirPath, session.Directories);
            Assert.Equal(4, session.Files.Count);
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void SimulateDirectory_WithName_UsesGivenName()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var dirPath = session.Generator.SimulateDirectory(2, 64, "my-dir");
            Assert.EndsWith("my-dir", dirPath);
            Assert.True(Directory.Exists(dirPath));
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void SimulateDirectory_WithSubdirectories_CreatesFullTree()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var spec = new TempDirectorySpec { FileCount = 2, FileSizeBytes = 128, Subdirectories = [TempDirectorySpec.Flat(3, 64), TempDirectorySpec.Flat(1, 256)] };
            var rootDir = session.Generator.SimulateDirectory(spec);
            Assert.True(Directory.Exists(rootDir));

            // root has 2 files + 2 subdirs
            Assert.Equal(2, Directory.GetFiles(rootDir).Length); // 2 is intentional — Assert.Single would be wrong here
            var subdirs = Directory.GetDirectories(rootDir);
            Assert.Equal(2, subdirs.Length);

            // subdirs have their own files — sort by file count to avoid filesystem-ordering sensitivity
            var subFileCounts = subdirs.Select(d => Directory.GetFiles(d).Length).OrderByDescending(c => c).ToArray();
            Assert.Equal(3, subFileCounts[0]);
            Assert.Equal(1, subFileCounts[1]);

            // session tracking: 3 dirs (root + 2 subs), 2+3+1=6 files
            Assert.Equal(3, session.Directories.Count);
            Assert.Equal(6, session.Files.Count);
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void SimulateDirectory_WithFileSizeUnitInfoFlat_Factory()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var spec = TempDirectorySpec.Flat(2, FileSizeUnitInfo.Kilobyte, 2);
            var dirPath = session.Generator.SimulateDirectory(spec);
            var files = Directory.GetFiles(dirPath);
            Assert.Equal(2, files.Length);
            Assert.All(files, f => Assert.Equal(FileSizeUnitInfo.Kilobyte.ConvertToBytes(2), new FileInfo(f).Length));
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void SimulateDirectory_NullSpec_Throws()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            Assert.Throws<ArgumentNullException>(() => session.Generator.SimulateDirectory(null!));
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public async Task SimulateDirectoryAsync_Creates_DirectoryWithFiles()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            await using var session = service.CreateSession();
            var dirPath = await session.Generator.SimulateDirectoryAsync(3, 512, null, TestContext.Current.CancellationToken);
            Assert.True(Directory.Exists(dirPath));
            Assert.Equal(3, Directory.GetFiles(dirPath).Length);
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public async Task SimulateDirectoryAsync_WithSubdirectories_CreatesFullTree()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            await using var session = service.CreateSession();
            var spec = new TempDirectorySpec { FileCount = 1, FileSizeBytes = 64, Subdirectories = [TempDirectorySpec.Flat(2, 32)] };
            var rootDir = await session.Generator.SimulateDirectoryAsync(spec, null, TestContext.Current.CancellationToken);
            Assert.Single(Directory.GetFiles(rootDir));
            var sub = Directory.GetDirectories(rootDir).Single();
            Assert.Equal(2, Directory.GetFiles(sub).Length);
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void Disposed_SessionGenerator_ThrowsOnCreateRandomFile()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            var session = service.CreateSession();
            var generator = session.Generator;
            session.Dispose();
            Assert.Throws<ObjectDisposedException>(() => generator.CreateRandomFile(128));
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void Disposed_SessionGenerator_ThrowsOnSimulateDirectory()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            var session = service.CreateSession();
            var generator = session.Generator;
            session.Dispose();
            Assert.Throws<ObjectDisposedException>(() => generator.SimulateDirectory(1, 64));
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void CreateRandomFiles_WithNameSelector_UsesProvidedNames()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var paths = session.Generator.CreateRandomFiles(3, 256, i => $"file_{i}.dat");
            Assert.Equal(3, paths.Count);
            Assert.Contains(paths, p => Path.GetFileName(p) == "file_0.dat");
            Assert.Contains(paths, p => Path.GetFileName(p) == "file_1.dat");
            Assert.Contains(paths, p => Path.GetFileName(p) == "file_2.dat");
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public async Task CreateRandomFilesAsync_WithNameSelector_UsesProvidedNames()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            await using var session = service.CreateSession();
            var paths = await session.Generator.CreateRandomFilesAsync(2, 128, i => $"async_{i}.bin", TestContext.Current.CancellationToken);
            Assert.Equal(2, paths.Count);
            Assert.All(paths, p => Assert.True(File.Exists(p)));
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void CreateTextFile_Creates_FileWithCorrectLineCount()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var path = session.Generator.CreateTextFile(10, 40);
            Assert.True(File.Exists(path));
            Assert.Equal(10, File.ReadAllLines(path).Length);
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void CreateTextFile_EachLine_HasCorrectLength()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var path = session.Generator.CreateTextFile(5, 20);
            var lines = File.ReadAllLines(path);
            Assert.All(lines, l => Assert.Equal(20, l.Length));
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void CreateTextFile_Registers_InSessionFiles()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var path = session.Generator.CreateTextFile(3, 10);
            Assert.Single(session.Files);
            Assert.Contains(path, session.Files);
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void CreateTextFile_InvalidArgs_Throw()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            Assert.ThrowsAny<ArgumentOutOfRangeException>(() => session.Generator.CreateTextFile(0, 10));
            Assert.ThrowsAny<ArgumentOutOfRangeException>(() => session.Generator.CreateTextFile(5, 0));
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public async Task CreateTextFileAsync_Creates_File()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            await using var session = service.CreateSession();
            var path = await session.Generator.CreateTextFileAsync(4, 15, null, TestContext.Current.CancellationToken);
            Assert.True(File.Exists(path));
            Assert.Equal(4, File.ReadAllLines(path).Length);
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void CreateCsvFile_Has_HeaderPlusDataRows()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var path = session.Generator.CreateCsvFile(5, 3);
            Assert.True(File.Exists(path));
            var lines = File.ReadAllLines(path);
            Assert.Equal(6, lines.Length); // 1 header + 5 data
            Assert.Equal("col_0,col_1,col_2", lines[0]);
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void CreateCsvFile_EachRow_HasCorrectColumnCount()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var path = session.Generator.CreateCsvFile(3, 4);
            var lines = File.ReadAllLines(path);
            Assert.All(lines, l => Assert.Equal(4, l.Split(',').Length));
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void CreateCsvFile_Registers_InSessionFiles()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var path = session.Generator.CreateCsvFile(2, 2);
            Assert.Single(session.Files);
            Assert.Contains(path, session.Files);
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public async Task CreateCsvFileAsync_Creates_File()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            await using var session = service.CreateSession();
            var path = await session.Generator.CreateCsvFileAsync(2, 2, null, TestContext.Current.CancellationToken);
            Assert.True(File.Exists(path));
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void CreateJsonFile_Creates_NonEmptyFile()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var path = session.Generator.CreateJsonFile(2, 3);
            Assert.True(File.Exists(path));
            Assert.True(new FileInfo(path).Length > 0);
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void CreateJsonFile_Content_StartsWithBrace()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var path = session.Generator.CreateJsonFile(1, 2);
            var text = File.ReadAllText(path).TrimStart();
            Assert.StartsWith("{", text);
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void CreateJsonFile_ZeroDepth_ProducesFlatObject()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var path = session.Generator.CreateJsonFile(0, 4);
            var text = File.ReadAllText(path);
            Assert.Contains("key_0", text);
            Assert.Contains("key_3", text);
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public async Task CreateJsonFileAsync_Creates_File()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            await using var session = service.CreateSession();
            var path = await session.Generator.CreateJsonFileAsync(1, 2, null, TestContext.Current.CancellationToken);
            Assert.True(File.Exists(path));
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void CreateZipFile_Creates_ValidZipArchive()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var zipPath = session.Generator.CreateZipFile(TempDirectorySpec.Flat(3, 512));
            Assert.True(File.Exists(zipPath));
            Assert.EndsWith(".zip", zipPath);
            using var archive = ZipFile.OpenRead(zipPath);
            Assert.Equal(3, archive.Entries.Count);
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void CreateZipFile_WithSubdirectories_CreatesNestedEntries()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var spec = TempDirectorySpec.Builder().WithFiles(2, 256).WithSubdirectory(sub => sub.WithFiles(2, 128)).Build();
            var zipPath = session.Generator.CreateZipFile(spec);
            using var archive = ZipFile.OpenRead(zipPath);
            Assert.Equal(4, archive.Entries.Count);
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void CreateZipFile_Is_TrackedInSession()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var zipPath = session.Generator.CreateZipFile(TempDirectorySpec.Flat(1, 128));
            Assert.Contains(zipPath, session.Files);
            Assert.True(session.GetTotalBytesUsed() > 0);
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void CreateZipFile_WithCustomName_UsesProvidedName()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var zipPath = session.Generator.CreateZipFile(TempDirectorySpec.Flat(1, 64), "myarchive.zip");
            Assert.Equal("myarchive.zip", Path.GetFileName(zipPath));
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public async Task CreateZipFileAsync_Creates_ValidZipArchive()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            await using var session = service.CreateSession();
            var zipPath = await session.Generator.CreateZipFileAsync(TempDirectorySpec.Flat(2, 256), ct: TestContext.Current.CancellationToken);
            Assert.True(File.Exists(zipPath));
            using var archive = ZipFile.OpenRead(zipPath);
            Assert.Equal(2, archive.Entries.Count);
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void CreateXmlFile_Creates_NonEmptyFile()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var path = session.Generator.CreateXmlFile(2, 3);
            Assert.True(File.Exists(path));
            Assert.True(new FileInfo(path).Length > 0);
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void CreateXmlFile_Content_IsValidXmlWithRootElement()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var path = session.Generator.CreateXmlFile(1, 2);
            var content = File.ReadAllText(path);
            Assert.StartsWith("<root>", content);
            Assert.Contains("</root>", content);
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void CreateXmlFile_Is_TrackedInSession()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var path = session.Generator.CreateXmlFile(1, 2);
            Assert.Contains(path, session.Files);
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void CreateXmlFile_WithName_UsesProvidedName()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var path = session.Generator.CreateXmlFile(1, 2, "data.xml");
            Assert.Equal("data.xml", Path.GetFileName(path));
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public async Task CreateXmlFileAsync_Creates_ValidXmlFile()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var path = await session.Generator.CreateXmlFileAsync(1, 3, ct: TestContext.Current.CancellationToken);
            Assert.True(File.Exists(path));
            var content = File.ReadAllText(path);
            Assert.Contains("<root>", content);
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void ExtractZipFile_Extracts_AllEntriesIntoSession()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var zipPath = session.Generator.CreateZipFile(TempDirectorySpec.Flat(3, 128));
            var extractDir = session.Generator.ExtractZipFile(zipPath);
            Assert.True(Directory.Exists(extractDir));
            Assert.Equal(3, Directory.GetFiles(extractDir, "*", SearchOption.AllDirectories).Length);
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void ExtractZipFile_Tracks_ExtractedFiles()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var zipPath = session.Generator.CreateZipFile(TempDirectorySpec.Flat(2, 64));
            var countBefore = session.Files.Count;
            session.Generator.ExtractZipFile(zipPath);
            Assert.True(session.Files.Count > countBefore);
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void ExtractZipFile_WithCustomDirName_UsesProvidedName()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var zipPath = session.Generator.CreateZipFile(TempDirectorySpec.Flat(1, 32));
            var extractDir = session.Generator.ExtractZipFile(zipPath, "my-extract");
            Assert.Equal("my-extract", Path.GetFileName(extractDir));
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void ExtractZipFile_ThrowsWhenZip_DoesNotExist()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            Assert.Throws<FileNotFoundException>(() => session.Generator.ExtractZipFile("/nonexistent/file.zip"));
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public async Task ExtractZipFileAsync_Extracts_Entries()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var zipPath = session.Generator.CreateZipFile(TempDirectorySpec.Flat(2, 128));
            var extractDir = await session.Generator.ExtractZipFileAsync(zipPath, ct: TestContext.Current.CancellationToken);
            Assert.True(Directory.Exists(extractDir));
            Assert.Equal(2, Directory.GetFiles(extractDir, "*", SearchOption.AllDirectories).Length);
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void CreateDirectoryTree_Creates_NestedDirectoryStructure()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var root = session.Generator.CreateDirectoryTree(2, 2, 64);
            Assert.True(Directory.Exists(root));
            var allDirs = Directory.GetDirectories(root, "*", SearchOption.AllDirectories);
            Assert.True(allDirs.Length >= 2, $"Expected nested dirs but got {allDirs.Length}");
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void CreateDirectoryTree_DepthZero_CreatesFlatDirectory()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var root = session.Generator.CreateDirectoryTree(0, 3, 32);
            Assert.True(Directory.Exists(root));
            Assert.Equal(3, Directory.GetFiles(root).Length);
            Assert.Empty(Directory.GetDirectories(root));
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void CreateDirectoryTree_Is_TrackedInSession()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var root = session.Generator.CreateDirectoryTree(1, 1, 32);
            Assert.Contains(root, session.Directories);
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void CreateDirectoryTree_TotalFilesMatch_Spec()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            // depth=1, 2 files per dir, 2 dirs per level: root has 2 files + 2 subdirs each with 2 files = 6 files total
            var root = session.Generator.CreateDirectoryTree(1, 2, 16);
            var allFiles = Directory.GetFiles(root, "*", SearchOption.AllDirectories);
            Assert.Equal(6, allFiles.Length);
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public async Task CreateDirectoryTreeAsync_Creates_NestedStructure()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var root = await session.Generator.CreateDirectoryTreeAsync(1, 2, 32, ct: TestContext.Current.CancellationToken);
            Assert.True(Directory.Exists(root));
            var allFiles = Directory.GetFiles(root, "*", SearchOption.AllDirectories);
            Assert.True(allFiles.Length >= 2);
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    private static void TryDeleteRoot(IOTempServiceOptions options)
    {
        if (Directory.Exists(options.RootDirectory))
            Directory.Delete(options.RootDirectory, true);
    }
}