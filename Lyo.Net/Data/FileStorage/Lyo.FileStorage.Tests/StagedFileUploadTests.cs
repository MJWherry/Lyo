using Lyo.FileStorage.Models;
using Lyo.FileStorage.Multipart;
using Lyo.FileStorage.Staged;
using Lyo.FileStorage.Tests.Support;

namespace Lyo.FileStorage.Tests;

public sealed class StagedFileUploadTests
{
    private static LocalStagedFileUploadService CreateStagedService(LocalFileStorageTestScope scope, InMemoryStagedFileUploadStore? store = null)
    {
        store ??= new();
        return new(scope.Storage, store, scope.Options);
    }

    private static LocalFileStorageTestScope CreateScope(Action<DiskFileStorageOptions>? configure = null)
        => LocalFileStorageTestScope.Create(o => {
            o.DirectUploadReceiveBaseUri = "https://tests.invalid";
            o.StagePutRouteRelativePath = "Workbench/FileStorage/stage";
            configure?.Invoke(o);
            return o;
        });

    [Fact]
    public async Task LocalStaged_BeginPutCompleteCommit_Plain_Roundtrips()
    {
        using var scope = CreateScope();
        var staged = CreateStagedService(scope);
        var payload = "staged-plain-payload"u8.ToArray();
        var begin = await staged.BeginAsync(new() { DeclaredMaxSizeBytes = payload.Length, OriginalFileName = "plain.txt" }, TestContext.Current.CancellationToken);
        Assert.Equal(MultipartUploadProviderKind.Local, begin.ProviderKind);
        Assert.Contains(begin.StageId.ToString("D"), begin.PresignedPutUrl, StringComparison.Ordinal);
        await using (var body = new MemoryStream(payload))
            await staged.ReceiveWorkbenchStagePutAsync(begin.StageId, body, TestContext.Current.CancellationToken);

        var completed = await staged.CompleteAsync(begin.StageId, ct: TestContext.Current.CancellationToken);
        Assert.Equal(StagedUploadStatus.Uploaded, completed.Status);
        Assert.Equal(payload.Length, completed.ObservedSizeBytes);
        Assert.NotNull(completed.ContentHash);
        var file = await staged.CommitAsync(begin.StageId, new(), TestContext.Current.CancellationToken);
        Assert.Equal(payload, await scope.Storage.GetFileAsync(file.Id, ct: TestContext.Current.CancellationToken));
        var finalStage = await staged.GetAsync(begin.StageId, TestContext.Current.CancellationToken);
        Assert.Equal(StagedUploadStatus.Committed, finalStage.Status);
        Assert.Equal(file.Id, finalStage.CommittedFileId);
    }

    [Fact]
    public async Task LocalStaged_CompleteWithoutBacking_ThrowsFileNotFound()
    {
        using var scope = CreateScope();
        var staged = CreateStagedService(scope);
        var begin = await staged.BeginAsync(new() { DeclaredMaxSizeBytes = 10, OriginalFileName = "missing.bin" }, TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<FileNotFoundException>(() => staged.CompleteAsync(begin.StageId, ct: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task LocalStaged_Abort_RemovesStageObject()
    {
        using var scope = CreateScope();
        var staged = CreateStagedService(scope);
        var payload = "abort-me"u8.ToArray();
        var begin = await staged.BeginAsync(new() { DeclaredMaxSizeBytes = payload.Length, OriginalFileName = "abort.bin" }, TestContext.Current.CancellationToken);
        await using (var body = new MemoryStream(payload))
            await staged.ReceiveWorkbenchStagePutAsync(begin.StageId, body, TestContext.Current.CancellationToken);

        await staged.AbortAsync(begin.StageId, TestContext.Current.CancellationToken);
        var stage = await staged.GetAsync(begin.StageId, TestContext.Current.CancellationToken);
        Assert.Equal(StagedUploadStatus.Aborted, stage.Status);
        var stagePath = Path.Combine(scope.Options.RootDirectoryPath, ".stage", begin.StageId.ToString("N"), "object");
        Assert.False(File.Exists(stagePath));
    }

    [Fact]
    public async Task LocalStaged_Events_FireOnBeginCompleteCommit()
    {
        using var scope = CreateScope();
        var staged = CreateStagedService(scope);
        var presignedFired = false;
        var completedFired = false;
        var committedFired = false;
        staged.PresignedCreated += (_, _) => presignedFired = true;
        staged.UploadCompleted += (_, _) => completedFired = true;
        staged.Committed += (_, _) => committedFired = true;
        var payload = "event-test"u8.ToArray();
        var begin = await staged.BeginAsync(new() { DeclaredMaxSizeBytes = payload.Length, OriginalFileName = "evt.bin" }, TestContext.Current.CancellationToken);
        Assert.True(presignedFired);
        await using (var body = new MemoryStream(payload))
            await staged.ReceiveWorkbenchStagePutAsync(begin.StageId, body, TestContext.Current.CancellationToken);

        await staged.CompleteAsync(begin.StageId, ct: TestContext.Current.CancellationToken);
        Assert.True(completedFired);
        await staged.CommitAsync(begin.StageId, new(), TestContext.Current.CancellationToken);
        Assert.True(committedFired);
    }

    [Fact]
    public async Task LocalStaged_BeginWithoutReceiveUri_ThrowsNotSupported()
    {
        using var scope = LocalFileStorageTestScope.Create();
        var staged = CreateStagedService(scope);
        await Assert.ThrowsAsync<NotSupportedException>(() => staged.BeginAsync(new() { DeclaredMaxSizeBytes = 10 }, TestContext.Current.CancellationToken));
    }
}