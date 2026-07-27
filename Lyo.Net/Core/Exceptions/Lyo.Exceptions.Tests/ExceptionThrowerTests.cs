namespace Lyo.Exceptions.Tests;

public class ExceptionThrowerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _tempFile;

    public ExceptionThrowerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"lyo-exceptions-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _tempFile = Path.Combine(_tempDir, "file.txt");
        File.WriteAllText(_tempFile, "content");
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private string MissingPath => Path.Combine(_tempDir, "does-not-exist");

    [Fact]
    public void ThrowIfDirectoryNotFound_Path_Existing_DoesNotThrow() => ExceptionThrower.ThrowIfDirectoryNotFound(_tempDir);

    [Fact]
    public void ThrowIfDirectoryNotFound_Path_Missing_Throws()
    {
        var ex = Assert.Throws<DirectoryNotFoundException>(() => ExceptionThrower.ThrowIfDirectoryNotFound(MissingPath));
        Assert.Contains(MissingPath, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ThrowIfDirectoryNotFound_Path_Null_ThrowsArgumentNull()
    {
        string? directoryPath = null;
        Assert.Throws<ArgumentNullException>(() => ExceptionThrower.ThrowIfDirectoryNotFound(directoryPath));
    }

    [Fact]
    public void ThrowIfDirectoryNotFound_DirectoryInfo_Existing_DoesNotThrow()
        => ExceptionThrower.ThrowIfDirectoryNotFound(new DirectoryInfo(_tempDir));

    [Fact]
    public void ThrowIfDirectoryNotFound_DirectoryInfo_Missing_Throws()
        => Assert.Throws<DirectoryNotFoundException>(() => ExceptionThrower.ThrowIfDirectoryNotFound(new DirectoryInfo(MissingPath)));

    [Fact]
    public void ThrowIfDirectoryNotFound_DirectoryInfo_Null_ThrowsArgumentNull()
    {
        DirectoryInfo? directoryInfo = null;
        Assert.Throws<ArgumentNullException>(() => ExceptionThrower.ThrowIfDirectoryNotFound(directoryInfo));
    }

    [Fact]
    public void ThrowIfFileNotAccessible_Path_Accessible_DoesNotThrow() => ExceptionThrower.ThrowIfFileNotAccessible(_tempFile);

    [Fact]
    public void ThrowIfFileNotAccessible_Path_Missing_DoesNotThrow() => ExceptionThrower.ThrowIfFileNotAccessible(MissingPath);

    [Fact]
    public void ThrowIfFileNotAccessible_Path_Null_ThrowsArgumentNull()
    {
        string? filePath = null;
        Assert.Throws<ArgumentNullException>(() => ExceptionThrower.ThrowIfFileNotAccessible(filePath));
    }

    [Fact]
    public void ThrowIfFileNotAccessible_Path_Locked_ThrowsIOException()
    {
        using var exclusive = new FileStream(_tempFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        var ex = Assert.Throws<IOException>(() => ExceptionThrower.ThrowIfFileNotAccessible(_tempFile));
        Assert.Contains(_tempFile, ex.Message, StringComparison.Ordinal);
        Assert.NotNull(ex.InnerException);
    }

    [Fact]
    public void ThrowIfFileNotAccessible_FileInfo_Accessible_DoesNotThrow()
        => ExceptionThrower.ThrowIfFileNotAccessible(new FileInfo(_tempFile));

    [Fact]
    public void ThrowIfFileNotAccessible_FileInfo_Null_ThrowsArgumentNull()
    {
        FileInfo? fileInfo = null;
        Assert.Throws<ArgumentNullException>(() => ExceptionThrower.ThrowIfFileNotAccessible(fileInfo));
    }

    [Fact]
    public void ThrowIfDirectoryNotAccessible_Path_Accessible_DoesNotThrow()
        => ExceptionThrower.ThrowIfDirectoryNotAccessible(_tempDir);

    [Fact]
    public void ThrowIfDirectoryNotAccessible_Path_Missing_DoesNotThrow()
        => ExceptionThrower.ThrowIfDirectoryNotAccessible(MissingPath);

    [Fact]
    public void ThrowIfDirectoryNotAccessible_Path_Null_ThrowsArgumentNull()
    {
        string? directoryPath = null;
        Assert.Throws<ArgumentNullException>(() => ExceptionThrower.ThrowIfDirectoryNotAccessible(directoryPath));
    }

    [Fact]
    public void ThrowIfDirectoryNotAccessible_DirectoryInfo_Accessible_DoesNotThrow()
        => ExceptionThrower.ThrowIfDirectoryNotAccessible(new DirectoryInfo(_tempDir));

    [Fact]
    public void ThrowIfDirectoryNotAccessible_DirectoryInfo_Null_ThrowsArgumentNull()
    {
        DirectoryInfo? directoryInfo = null;
        Assert.Throws<ArgumentNullException>(() => ExceptionThrower.ThrowIfDirectoryNotAccessible(directoryInfo));
    }
}
