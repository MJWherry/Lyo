using Lyo.Api.FileStorage.Models;
using Lyo.FileMetadataStore.Models;
using Lyo.IO.Temp.Models;
using Lyo.Web.Components.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lyo.FileStorage.Web.Components.FileStorageManagement;

/// <summary>Kitchen-sink upload protocol on the Files tab (direct PUT, staged, or multipart).</summary>
public enum FileStoreProtocolUploadMode
{
    Direct,
    Staged,
    Multipart
}

/// <summary>Kitchen-sink UI for direct PUT, staged, and multipart upload against the file-storage API.</summary>
public partial class FileStoreProtocolUploads : ComponentBase, IAsyncDisposable
{
    private bool _busy;
    private bool _compress;
    private bool _encrypt;
    private LocalBrowserFilePath? _file;
    private string _keyId = "";
    private string? _originalFileName;
    private string _pathPrefix = "";
    private IIOTempSession? _stagingSession;
    private string _status = "No file selected.";

    [CascadingParameter]
    public FileStorageManagement Host { get; set; } = default!;

    [Parameter]
    public FileStoreProtocolUploadMode Mode { get; set; }

    protected override void OnParametersSet()
    {
        if (_stagingSession != null)
            return;

        _stagingSession = Host.TempService.CreateSession();
    }

    private string Title => Mode switch {
        FileStoreProtocolUploadMode.Direct => "Direct PUT",
        FileStoreProtocolUploadMode.Staged => "Staged upload",
        var _ => "Multipart"
    };

    private string Help => Mode switch {
        FileStoreProtocolUploadMode.Direct => "Begin a client PUT, upload bytes to the returned URL, then complete. Plain objects only.",
        FileStoreProtocolUploadMode.Staged => "Begin a staging PUT, complete, then commit with optional compress/encrypt.",
        var _ => "Begin a multipart session, PUT each part, then complete."
    };

    public async ValueTask DisposeAsync()
    {
        if (_stagingSession is null)
            return;

        await _stagingSession.DisposeAsync();
        _stagingSession = null;
    }

    private Task OnFileReadyAsync(LocalBrowserFilePath file)
    {
        _file = file;
        _originalFileName = file.FileName;
        _status = $"{file.FileName} staged — click Upload.";
        return Task.CompletedTask;
    }

    private Task OnFileRemovedAsync(LocalBrowserFilePath file)
    {
        if (ReferenceEquals(_file, file))
            _file = null;

        _status = $"{file.FileName} removed.";
        return Task.CompletedTask;
    }

    private async Task RunAsync()
    {
        if (_file == null)
            return;

        if ((Mode == FileStoreProtocolUploadMode.Staged || Mode == FileStoreProtocolUploadMode.Multipart) && _encrypt && string.IsNullOrWhiteSpace(_keyId)) {
            _status = "Key id is required when encryption is enabled.";
            Host.Snackbar.Add(_status, Severity.Warning);
            return;
        }

        _busy = true;
        try {
            await using var stream = File.OpenRead(_file.FilePath);
            var length = stream.Length;
            var name = string.IsNullOrWhiteSpace(_originalFileName) ? _file.FileName : _originalFileName;
            var prefix = string.IsNullOrWhiteSpace(_pathPrefix) ? null : _pathPrefix.Trim();
            FileStoreResult? result = Mode switch {
                FileStoreProtocolUploadMode.Direct => await RunDirectAsync(stream, length, name, prefix),
                FileStoreProtocolUploadMode.Staged => await RunStagedAsync(stream, length, name, prefix),
                var _ => await RunMultipartAsync(stream, length, name, prefix)
            };

            _status = result == null ? "Upload finished." : $"Saved {result.OriginalFileName ?? result.SourceFileName} ({result.Id}).";
            Host.Snackbar.Add(_status, Severity.Success);
            await Host.NotifyFilesChangedAsync();
        }
        catch (Exception ex) {
            _status = ex.Message;
            Host.Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task<FileStoreResult?> RunDirectAsync(Stream stream, long length, string name, string? prefix)
    {
        var begin = await Host.ApiClient.PostAsAsync<DirectUploadBeginRequest, DirectUploadBeginResult>(
            Host.FilesApi("direct-upload/begin"),
            new() { OriginalFileName = name, PathPrefix = prefix, DeclaredMaxSizeBytes = length });
        if (begin == null)
            throw new InvalidOperationException("Direct upload begin returned no result.");

        await PutAsync(begin.PresignedPutUrl, stream, begin.RequiredPutHeaders);
        return await Host.ApiClient.PostAsAsync<DirectUploadCompleteRequest, FileStoreResult>(
            Host.FilesApi($"direct-upload/{begin.FileId:D}/complete"),
            new() { ExpectedByteLength = length, OriginalFileName = name });
    }

    private async Task<FileStoreResult?> RunStagedAsync(Stream stream, long length, string name, string? prefix)
    {
        var begin = await Host.ApiClient.PostAsAsync<StagedUploadBeginRequest, StagedUploadBeginResult>(
            Host.FilesApi("stage/begin"),
            new() { OriginalFileName = name, PathPrefix = prefix, DeclaredMaxSizeBytes = length });
        if (begin == null)
            throw new InvalidOperationException("Staged upload begin returned no result.");

        await PutAsync(begin.PresignedPutUrl, stream, begin.RequiredPutHeaders);
        await Host.ApiClient.PostAsAsync<StagedUploadCompleteRequest, object>(
            Host.FilesApi($"stage/{begin.StageId:D}/complete"),
            new() { ExpectedByteLength = length, OriginalFileName = name });
        return await Host.ApiClient.PostAsAsync<StagedUploadCommitRequest, FileStoreResult>(
            Host.FilesApi($"stage/{begin.StageId:D}/commit"),
            new() {
                Compress = _compress,
                Encrypt = _encrypt,
                KeyId = _encrypt ? _keyId : null,
                PathPrefix = prefix
            });
    }

    private async Task<FileStoreResult?> RunMultipartAsync(Stream stream, long length, string name, string? prefix)
    {
        const int partSize = 8 * 1024 * 1024;
        var begin = await Host.ApiClient.PostAsAsync<BeginMultipartRequest, MultipartBeginResponse>(
            Host.FilesApi("multipart/begin"),
            new(partSize, _compress, _encrypt, _encrypt ? _keyId : null, prefix, null, name, null, length));
        if (begin == null)
            throw new InvalidOperationException("Multipart begin returned no result.");

        var parts = new List<CompletedPart>();
        var partNumber = 1;
        var remaining = length;
        while (remaining > 0) {
            var take = (int)Math.Min(partSize, remaining);
            var descriptor = await Host.ApiClient.GetAsAsync<MultipartPartUrlResponse>(
                Host.FilesApi($"multipart/{begin.SessionId:D}/part-url?partNumber={partNumber}"));
            if (descriptor?.PresignedPutUrl == null)
                throw new InvalidOperationException($"No PUT URL for part {partNumber}.");

            await using var slice = new MemoryStream(take);
            var buffer = new byte[take];
            var read = await stream.ReadAsync(buffer.AsMemory(0, take));
            await slice.WriteAsync(buffer.AsMemory(0, read));
            slice.Position = 0;
            var etag = await PutAsync(descriptor.PresignedPutUrl, slice, null);
            parts.Add(new(partNumber, string.IsNullOrWhiteSpace(etag) ? $"part-{partNumber}" : etag));
            remaining -= read;
            partNumber++;
        }

        return await Host.ApiClient.PostAsAsync<CompleteMultipartRequest, FileStoreResult>(
            Host.FilesApi("multipart/complete"),
            new(begin.SessionId, parts));
    }

    private async Task<string?> PutAsync(string url, Stream body, IReadOnlyDictionary<string, string>? headers)
    {
        var client = Host.ApiClient.GetClient();
        var target = ResolvePutUrl(url, client.BaseAddress);
        using var content = new StreamContent(body);
        using var request = new HttpRequestMessage(HttpMethod.Put, target) { Content = content };
        if (headers != null) {
            foreach (var pair in headers) {
                if (!content.Headers.TryAddWithoutValidation(pair.Key, pair.Value))
                    request.Headers.TryAddWithoutValidation(pair.Key, pair.Value);
            }
        }

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return response.Headers.ETag?.Tag?.Trim('"');
    }

    private static Uri ResolvePutUrl(string url, Uri? baseAddress)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var absolute))
            return absolute;

        var relative = url.TrimStart('/');
        return baseAddress == null ? new Uri(relative, UriKind.Relative) : new Uri(baseAddress, relative);
    }
}
