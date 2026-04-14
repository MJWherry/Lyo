using Lyo.FileStorage.Models;

namespace Lyo.FileStorage.Policy;

public sealed class DefaultFileContentPolicy : IFileContentPolicy
{
    private readonly FileStorageServiceBaseOptions _options;

    public DefaultFileContentPolicy(FileStorageServiceBaseOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    public Task ValidateAsync(FileSavePolicyContext context, CancellationToken ct = default)
    {
        if (_options.MaxUploadSizeBytes is { } max && context.ByteLength > max)
            throw new FilePolicyRejectedException($"Upload size {context.ByteLength} exceeds maximum {_options.MaxUploadSizeBytes} bytes.");

        if (_options.AllowedContentTypes is { Count: > 0 } allowed) {
            if (string.IsNullOrWhiteSpace(context.ContentType))
                throw new FilePolicyRejectedException("Content-Type is required for this storage configuration.");

            var ctNorm = context.ContentType.Trim();
            var ok = allowed.Any(a => string.Equals(a, ctNorm, StringComparison.OrdinalIgnoreCase));
            if (!ok)
                throw new FilePolicyRejectedException($"Content-Type '{context.ContentType}' is not allowed.");
        }

        return Task.CompletedTask;
    }
}