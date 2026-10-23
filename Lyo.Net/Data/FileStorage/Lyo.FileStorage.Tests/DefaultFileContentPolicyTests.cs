using Lyo.FileStorage.Models;
using Lyo.FileStorage.Policy;

namespace Lyo.FileStorage.Tests;

public sealed class DefaultFileContentPolicyTests
{
    [Fact]
    public async Task ValidateAsync_NullMimeAndFileNameLists_AllowsAnyValue()
    {
        var policy = new DefaultFileContentPolicy(Options());
        await policy.ValidateAsync(Context("archive.jar", "application/java-archive"), TestCt);
    }

    [Fact]
    public async Task ValidateAsync_EmptyContentTypeWhitelist_RejectsEveryType()
    {
        var policy = new DefaultFileContentPolicy(Options(o => o.AllowedContentTypes = []));
        await Assert.ThrowsAsync<FilePolicyRejectedException>(() => policy.ValidateAsync(Context("a.pdf", "application/pdf"), TestCt));
    }

    [Fact]
    public async Task ValidateAsync_EmptyContentTypeBlacklist_AllowsType()
    {
        var policy = new DefaultFileContentPolicy(Options(o => o.DeniedContentTypes = []));
        await policy.ValidateAsync(Context("a.pdf", "application/pdf"), TestCt);
    }

    [Fact]
    public async Task ValidateAsync_AllowedContentTypes_RejectsTypeOutsideList()
    {
        var policy = new DefaultFileContentPolicy(Options(o => o.AllowedContentTypes = ["application/pdf"]));
        await policy.ValidateAsync(Context("a.pdf", "application/pdf"), TestCt);
        await Assert.ThrowsAsync<FilePolicyRejectedException>(() => policy.ValidateAsync(Context("app.jar", "application/java-archive"), TestCt));
    }

    [Fact]
    public async Task ValidateAsync_DeniedContentTypes_RejectsListedTypeAndAllowsOthers()
    {
        var policy = new DefaultFileContentPolicy(Options(o => o.DeniedContentTypes = ["application/java-archive"]));
        await policy.ValidateAsync(Context("a.pdf", "application/pdf"), TestCt);
        await Assert.ThrowsAsync<FilePolicyRejectedException>(() => policy.ValidateAsync(Context("app.jar", "application/java-archive"), TestCt));
    }

    [Fact]
    public async Task ValidateAsync_EmptyFileNameWhitelist_RejectsEveryName()
    {
        var policy = new DefaultFileContentPolicy(Options(o => o.AllowedFileNamePatterns = []));
        await Assert.ThrowsAsync<FilePolicyRejectedException>(() => policy.ValidateAsync(Context("report.pdf", "application/pdf"), TestCt));
    }

    [Fact]
    public async Task ValidateAsync_EmptyFileNameBlacklist_AllowsName()
    {
        var policy = new DefaultFileContentPolicy(Options(o => o.DeniedFileNamePatterns = []));
        await policy.ValidateAsync(Context("report.pdf", "application/pdf"), TestCt);
    }

    [Fact]
    public async Task ValidateAsync_AllowedFileNamePattern_MatchesPdfAndRejectsJarAndMissingName()
    {
        var policy = new DefaultFileContentPolicy(Options(o => o.AllowedFileNamePatterns = ["\\.pdf$"]));
        await policy.ValidateAsync(Context("report.pdf", "application/pdf"), TestCt);
        await Assert.ThrowsAsync<FilePolicyRejectedException>(() => policy.ValidateAsync(Context("evil.jar", "application/pdf"), TestCt));
        await Assert.ThrowsAsync<FilePolicyRejectedException>(() => policy.ValidateAsync(Context(null, "application/pdf"), TestCt));
    }

    [Fact]
    public async Task ValidateAsync_DeniedFileNamePattern_RejectsJarAndAllowsPdf()
    {
        var policy = new DefaultFileContentPolicy(Options(o => o.DeniedFileNamePatterns = ["\\.jar$"]));
        await policy.ValidateAsync(Context("report.pdf", "application/pdf"), TestCt);
        await Assert.ThrowsAsync<FilePolicyRejectedException>(() => policy.ValidateAsync(Context("evil.jar", "application/java-archive"), TestCt));
    }

    private static DiskFileStorageOptions Options(Action<DiskFileStorageOptions>? configure = null)
    {
        var options = new DiskFileStorageOptions { RootDirectoryPath = "unused" };
        configure?.Invoke(options);
        return options;
    }

    private static FileSavePolicyContext Context(string? name, string? contentType) => new() { ByteLength = 1, OriginalFileName = name, ContentType = contentType };
}