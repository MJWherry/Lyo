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
        options.ValidateUploadFilters();
        _options = options;
    }

    public Task ValidateAsync(FileSavePolicyContext context, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(context);
        if (context.ByteLength < 0)
            throw new FilePolicyRejectedException($"Upload size {context.ByteLength} is negative.");

        if (_options.MaxUploadSizeBytes is { } max && context.ByteLength > max)
            throw new FilePolicyRejectedException($"Upload size {context.ByteLength} exceeds maximum {_options.MaxUploadSizeBytes} bytes.");

        ThrowIfContentTypeRejected(context.ContentType);
        FileNamePatternFilter.ThrowIfRejected(_options, context.OriginalFileName);
        return Task.CompletedTask;
    }

    private void ThrowIfContentTypeRejected(string? contentType)
    {
        var allowed = _options.AllowedContentTypes;
        var denied = _options.DeniedContentTypes;
        if (allowed is null && denied is null)
            return;

        // An empty allow-list that was set on purpose means deny everything. That surfaces a likely misconfiguration instead of silently allowing any upload.
        if (allowed is not null) {
            if (contentType.IsNullOrWhitespace())
                throw new FilePolicyRejectedException("Content-Type is required for this storage configuration.");

            if (allowed.Count == 0)
                throw new FilePolicyRejectedException("Allowed content types list is empty; no content types are permitted by current policy.");

            var allowedNorm = contentType.Trim();
            if (!allowed.Any(candidate => string.Equals(candidate, allowedNorm, StringComparison.OrdinalIgnoreCase)))
                throw new FilePolicyRejectedException($"Content-Type '{contentType}' is not allowed.");

            return;
        }

        if (contentType.IsNullOrWhitespace())
            return;

        var deniedNorm = contentType.Trim();
        if (denied!.Any(candidate => string.Equals(candidate, deniedNorm, StringComparison.OrdinalIgnoreCase)))
            throw new FilePolicyRejectedException($"Content-Type '{contentType}' is not allowed.");
    }
}