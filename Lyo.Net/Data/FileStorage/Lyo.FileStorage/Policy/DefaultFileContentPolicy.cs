using Lyo.Common.Core.Extensions;
using Lyo.Exceptions;
using Lyo.FileStorage.Models;

namespace Lyo.FileStorage.Policy;

public sealed class DefaultFileContentPolicy : IFileContentPolicy
{
    private readonly FileStorageServiceBaseOptions _options;

    public DefaultFileContentPolicy(FileStorageServiceBaseOptions options)
    {
        ArgumentHelpers.ThrowIfNull(options);
        _options = options;
    }

    public Task ValidateAsync(FileSavePolicyContext context, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(context);
        if (context.ByteLength < 0)
            throw new FilePolicyRejectedException($"Upload size {context.ByteLength} is negative.");

        if (_options.MaxUploadSizeBytes is { } max && context.ByteLength > max)
            throw new FilePolicyRejectedException($"Upload size {context.ByteLength} exceeds maximum {_options.MaxUploadSizeBytes} bytes.");

        // null means the caller did not restrict content types (allow all).
        // An empty allow-list that was set on purpose means deny everything. That surfaces a likely misconfiguration instead of silently allowing any upload.
        var allowed = _options.AllowedContentTypes;
        if (allowed is null)
            return Task.CompletedTask;

        if (context.ContentType.IsNullOrWhitespace())
            throw new FilePolicyRejectedException("Content-Type is required for this storage configuration.");

        if (allowed.Count == 0)
            throw new FilePolicyRejectedException("Allowed content types list is empty; no content types are permitted by current policy.");

        var ctNorm = context.ContentType.Trim();
        var ok = allowed.Any(a => string.Equals(a, ctNorm, StringComparison.OrdinalIgnoreCase));
        if (!ok)
            throw new FilePolicyRejectedException($"Content-Type '{context.ContentType}' is not allowed.");

        return Task.CompletedTask;
    }
}