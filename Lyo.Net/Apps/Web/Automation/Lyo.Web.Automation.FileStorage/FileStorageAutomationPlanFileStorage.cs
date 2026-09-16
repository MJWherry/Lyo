using Lyo.Exceptions;
using Lyo.FileStorage.Abstractions;
using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Web.Automation.FileStorage;

/// <summary>Sends plan files and viewport PNGs to an <see cref="IFileStorageService" /> the host already configured.</summary>
public sealed class FileStorageAutomationPlanFileStorage(IServiceScopeFactory scopes, Func<IServiceProvider, IFileStorageService> resolve) : IAutomationPlanFileStorage
{
    /// <inheritdoc />
    public Task<IReadOnlyList<string>> UploadDirectoryAsync(string sourceDirectory, string destinationPrefix, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(sourceDirectory);
        return UseAsync(async files => {
            var ids = new List<string>();
            foreach (var path in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories)) {
                var saved = await files.SaveFileAsync(path, Path.GetFileName(path), pathPrefix: destinationPrefix, ct: ct).ConfigureAwait(false);
                ids.Add(saved.Id.ToString("D"));
            }

            return (IReadOnlyList<string>)ids;
        });
    }

    /// <inheritdoc />
    public Task<string?> SaveSnapshotAsync(byte[] png, Guid runId, Guid stepId, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(png);
        return SaveAsync(png, AutomationRunStoragePaths.ScreenshotFileName(stepId), AutomationRunStoragePaths.Step(runId, stepId), "image/png", ct);
    }

    /// <inheritdoc />
    public Task<string?> SaveAsync(byte[] data, string fileName, string pathPrefix, string contentType, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(pathPrefix);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(contentType);
        return UseAsync(async files => {
            var saved = await files.SaveFileAsync(data, fileName, contentType: contentType, pathPrefix: pathPrefix, ct: ct).ConfigureAwait(false);
            return (string?)saved.Id.ToString("D");
        });
    }

    /// <inheritdoc />
    public Task<byte[]?> ReadSnapshotAsync(string fileId, CancellationToken ct)
    {
        if (!Guid.TryParse(fileId, out var id))
            return Task.FromResult<byte[]?>(null);

        return UseAsync(async files => {
            try {
                return await files.GetFileAsync(id, ct: ct).ConfigureAwait(false);
            }
            catch (FileNotFoundException) {
                return null;
            }
        });
    }

    private async Task<T> UseAsync<T>(Func<IFileStorageService, Task<T>> action)
    {
        await using var scope = scopes.CreateAsyncScope();
        return await action(resolve(scope.ServiceProvider)).ConfigureAwait(false);
    }
}
