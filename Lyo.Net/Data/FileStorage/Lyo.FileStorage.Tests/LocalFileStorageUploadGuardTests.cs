using Lyo.FileStorage.Policy;
using Lyo.FileStorage.Tests.Support;

namespace Lyo.FileStorage.Tests;

public sealed class LocalFileStorageUploadGuardTests
{
    [Fact]
    public async Task SaveFileAsync_VerifyFileSignature_RejectsZipNamedPdfAndSavesRealPdf()
    {
        using var scope = LocalFileStorageTestScope.Create(o => {
            o.VerifyFileSignature = true;
            return o;
        });

        var before = Directory.GetFiles(scope.Options.RootDirectoryPath, "*", SearchOption.AllDirectories);
        var zip = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x00, 0x00 };
        await Assert.ThrowsAsync<FilePolicyRejectedException>(() => scope.Storage.SaveFileAsync(zip, "report.pdf", ct: TestCt));
        var afterReject = Directory.GetFiles(scope.Options.RootDirectoryPath, "*", SearchOption.AllDirectories);
        Assert.Equal(before, afterReject);
        var pdf = "%PDF-1.4\n"u8.ToArray();
        var saved = await scope.Storage.SaveFileAsync(pdf, "report.pdf", ct: TestCt);
        Assert.Equal("report.pdf", saved.OriginalFileName);
    }

    [Fact]
    public async Task RenameFileAsync_DeniedFileNamePattern_RejectsExe()
    {
        using var scope = LocalFileStorageTestScope.Create(o => {
            o.DeniedFileNamePatterns = ["\\.exe$"];
            return o;
        });

        var saved = await scope.Storage.SaveFileAsync("hello"u8.ToArray(), "notes.txt", ct: TestCt);
        await Assert.ThrowsAsync<FilePolicyRejectedException>(() => scope.Storage.RenameFileAsync(saved.Id, new() { OriginalFileName = "virus.exe" }, TestCt));
    }
}