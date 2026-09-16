using System.Text.Json;
using Lyo.Exceptions;
using Lyo.FileStorage;
using Lyo.FileStorage.Abstractions;
using Lyo.HomeInventory.Models;
using Lyo.HomeInventory.Models.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.HomeInventory.FileStorage;

/// <summary>Saves FileStorage bytes and writes a HomeInventory attachment row.</summary>
public interface IHomeInventoryAttachmentFiles
{
    /// <summary>Stores the stream under the inventory PathPrefix and inserts an attachment row.</summary>
    Task<HomeAttachmentRecord> SaveAsync(
        string entityType,
        Guid entityId,
        Stream content,
        long length,
        string? fileName,
        HomeAttachmentRole role,
        string? caption = null,
        bool isPrimary = false,
        Guid? tenantId = null,
        CancellationToken ct = default);

    /// <summary>Deletes the attachment row and optionally the stored file.</summary>
    Task DeleteAsync(Guid attachmentId, bool deleteStoredFile = true, Guid? tenantId = null, CancellationToken ct = default);
}

/// <summary>Default <see cref="IHomeInventoryAttachmentFiles" /> over <see cref="IFileStorageService" /> and <see cref="IHomeInventoryStore" />.</summary>
public sealed class HomeInventoryAttachmentFiles : IHomeInventoryAttachmentFiles
{
    private readonly IFileStorageService _files;
    private readonly IHomeInventoryStore _store;

    /// <summary>Creates the helper.</summary>
    public HomeInventoryAttachmentFiles(IFileStorageService files, IHomeInventoryStore store)
    {
        ArgumentHelpers.ThrowIfNull(files);
        ArgumentHelpers.ThrowIfNull(store);
        _files = files;
        _store = store;
    }

    /// <inheritdoc />
    public async Task<HomeAttachmentRecord> SaveAsync(
        string entityType,
        Guid entityId,
        Stream content,
        long length,
        string? fileName,
        HomeAttachmentRole role,
        string? caption = null,
        bool isPrimary = false,
        Guid? tenantId = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(content);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(entityType);
        if (entityId == Guid.Empty)
            throw new ArgumentException("Entity id is required.", nameof(entityId));

        var prefix = HomeInventoryFilePaths.ForEntity(entityType, entityId);
        var metadata = JsonSerializer.SerializeToElement(new Dictionary<string, string> {
            ["entityType"] = entityType,
            ["entityId"] = entityId.ToString("D"),
            ["role"] = role.ToString()
        });
        var stored = await _files.SaveFromStreamAsync(
                content, length, fileName, pathPrefix: prefix, metadata: metadata, ct: ct)
            .ConfigureAwait(false);
        var row = new HomeAttachmentRecord {
            EntityType = entityType,
            EntityId = entityId,
            FileId = stored.Id,
            Role = role,
            Caption = caption,
            IsPrimary = isPrimary
        };
        await _store.SaveAttachmentAsync(row, tenantId, ct).ConfigureAwait(false);
        var listed = await _store.ListAttachmentsAsync(entityType, entityId, tenantId, ct).ConfigureAwait(false);
        return listed.FirstOrDefault(a => a.FileId == stored.Id) ?? row;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid attachmentId, bool deleteStoredFile = true, Guid? tenantId = null, CancellationToken ct = default)
    {
        var existing = await _store.GetAttachmentByIdAsync(attachmentId, tenantId, ct).ConfigureAwait(false);
        await _store.DeleteAttachmentAsync(attachmentId, tenantId, ct).ConfigureAwait(false);
        if (deleteStoredFile && existing != null)
            await _files.DeleteFileAsync(existing.FileId, ct: ct).ConfigureAwait(false);
    }
}

/// <summary>DI registration for the optional HomeInventory FileStorage helper.</summary>
public static class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="IHomeInventoryAttachmentFiles" />. Requires <see cref="IFileStorageService" /> and <see cref="IHomeInventoryStore" />.</summary>
        public IServiceCollection AddHomeInventoryFileStorage()
        {
            ArgumentHelpers.ThrowIfNull(services);
            services.TryAddScoped<IHomeInventoryAttachmentFiles>(sp => new HomeInventoryAttachmentFiles(
                sp.GetRequiredService<IFileStorageService>(),
                sp.GetRequiredService<IHomeInventoryStore>()));
            return services;
        }
    }
}
