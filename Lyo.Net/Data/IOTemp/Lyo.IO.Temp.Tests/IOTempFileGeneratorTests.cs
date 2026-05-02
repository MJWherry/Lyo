using System.IO.Compression;
using Lyo.Common.Records;
using Lyo.IO.Temp.Models;

namespace Lyo.IO.Temp.Tests;

public sealed class IOTempFileGeneratorTests
{
    private static IOTempServiceOptions GetTestOptions()
        => new() { TempRoot = Path.Combine(Path.GetTempPath(), "lyo-io-temp-tests"), DirectoryName = Guid.NewGuid().ToString("N") };

    [Fact]
    public void Session_exposes_Generator()
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

    [Fact]
    public void CreateRandomFile_creates_file_with_correct_size()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var path = session.Generator.CreateRandomFile(1024);
            Assert.True(File.Exists(path));
            Assert.Equal(1024, new FileInfo(path).Length);
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void CreateRandomFile_with_zero_bytes_creates_empty_file()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            var path = session.Generator.CreateRandomFile(0);
            Assert.True(File.Exists(path));
            Assert.Equal(0, new FileInfo(path).Length);
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public void CreateRandomFile_with_FileSizeUnitInfo_creates_correct_size()
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
    public void CreateRandomFile_with_name_uses_given_name()
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
    public void CreateRandomFile_registers_in_session_Files()
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
    public void CreateRandomFile_content_is_not_all_zeros()
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
    public async Task CreateRandomFileAsync_creates_file_with_correct_size()
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
    public async Task CreateRandomFileAsync_with_FileSizeUnitInfo_creates_correct_size()
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
    public void CreateRandomFiles_creates_correct_count_and_size()
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
    public void CreateRandomFiles_with_FileSizeUnitInfo_creates_correct_sizes()
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
    public void CreateRandomFiles_with_invalid_count_throws(int count)
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
    public async Task CreateRandomFilesAsync_creates_correct_count()
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
    public void SimulateDirectory_creates_directory_with_files()
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
    public void SimulateDirectory_with_spec_creates_directory_and_registers_tracking()
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
    public void SimulateDirectory_with_name_uses_given_name()
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
    public void SimulateDirectory_with_subdirectories_creates_full_tree()
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
    public void SimulateDirectory_with_FileSizeUnitInfo_flat_factory()
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
    public void SimulateDirectory_null_spec_throws()
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
    public async Task SimulateDirectoryAsync_creates_directory_with_files()
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
    public async Task SimulateDirectoryAsync_with_subdirectories_creates_full_tree()
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
    public void Disposed_session_Generator_throws_on_CreateRandomFile()
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
    public void Disposed_session_Generator_throws_on_SimulateDirectory()
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
    public void CreateRandomFiles_with_name_selector_uses_provided_names()
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
    public async Task CreateRandomFilesAsync_with_name_selector_uses_provided_names()
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
    public void CreateTextFile_creates_file_with_correct_line_count()
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
    public void CreateTextFile_each_line_has_correct_length()
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
    public void CreateTextFile_registers_in_session_Files()
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
    public void CreateTextFile_invalid_args_throw()
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
    public async Task CreateTextFileAsync_creates_file()
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
    public void CreateCsvFile_has_header_plus_data_rows()
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
    public void CreateCsvFile_each_row_has_correct_column_count()
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
    public void CreateCsvFile_registers_in_session_Files()
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
    public async Task CreateCsvFileAsync_creates_file()
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
    public void CreateJsonFile_creates_non_empty_file()
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
    public void CreateJsonFile_content_starts_with_brace()
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
    public void CreateJsonFile_zero_depth_produces_flat_object()
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
    public async Task CreateJsonFileAsync_creates_file()
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
    public void CreateZipFile_creates_valid_zip_archive()
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
    public void CreateZipFile_with_subdirectories_creates_nested_entries()
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
    public void CreateZipFile_is_tracked_in_session()
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
    public void CreateZipFile_with_custom_name_uses_provided_name()
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
    public async Task CreateZipFileAsync_creates_valid_zip_archive()
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
    public void CreateXmlFile_creates_non_empty_file()
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
    public void CreateXmlFile_content_is_valid_xml_with_root_element()
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
    public void CreateXmlFile_is_tracked_in_session()
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
    public void CreateXmlFile_with_name_uses_provided_name()
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
    public async Task CreateXmlFileAsync_creates_valid_xml_file()
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
    public void ExtractZipFile_extracts_all_entries_into_session()
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
    public void ExtractZipFile_tracks_extracted_files()
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
    public void ExtractZipFile_with_custom_dir_name_uses_provided_name()
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
    public void ExtractZipFile_throws_when_zip_does_not_exist()
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
    public async Task ExtractZipFileAsync_extracts_entries()
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
    public void CreateDirectoryTree_creates_nested_directory_structure()
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
    public void CreateDirectoryTree_depth_zero_creates_flat_directory()
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
    public void CreateDirectoryTree_is_tracked_in_session()
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
    public void CreateDirectoryTree_total_files_match_spec()
    {
        var options = GetTestOptions();
        try {
            using var service = new IOTempService(options);
            using var session = service.CreateSession();
            // depth=1, 2 files per dir, 2 dirs per level: root has 2 files + 2 subdirs each with 2 files = 6 files total
            var root = session.Generator.CreateDirectoryTree(1, 2, 16, 2);
            var allFiles = Directory.GetFiles(root, "*", SearchOption.AllDirectories);
            Assert.Equal(6, allFiles.Length);
        }
        finally {
            TryDeleteRoot(options);
        }
    }

    [Fact]
    public async Task CreateDirectoryTreeAsync_creates_nested_structure()
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