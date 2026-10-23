using System.Security.Cryptography;
using System.Text;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.FileMetadataStore.Models;
using Lyo.FileMetadataStore.Postgres;
using Lyo.FileMetadataStore.Postgres.Database;
using Lyo.Seed;
using Microsoft.EntityFrameworkCore;

namespace Lyo.FileMetadataStore.Seed;

/// <summary>EF seed of files with a tiny UTF-8 blob, one revision, audit event, multipart session, and download link. Schema <c>filestore</c>.</summary>
public sealed class FileMetadataEfSeedContributor : SeedContributor, IEfSeedSource
{
    /// <summary>Plaintext stored in each file blob.</summary>
    public const string BlobText = "seed";

    /// <inheritdoc />
    public override string Name => "File metadata";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    public string Schema => PostgresFileMetadataStoreOptions.Schema;

    /// <inheritdoc />
    public DbContext CreateContext(string connectionString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        var options = new DbContextOptionsBuilder<FileMetadataStoreDbContext>().UseNpgsql(connectionString, n => n.MigrationsHistoryTable("__EFMigrationsHistory", Schema)).Options;
        return new FileMetadataStoreDbContext(options);
    }

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        graph.OnClear(ClearAsync);
        var now = DateTime.UtcNow;
        var blob = Encoding.UTF8.GetBytes(BlobText);
        var hash = SHA256.HashData(blob);
        var files = graph.Entity(
            options.Count, i => {
                var id = LyoGuid.CreateV7();
                return new FileMetadataEntity {
                    Id = id.ToString(),
                    OriginalFileName = $"notes-{i}.txt",
                    OriginalFileSize = blob.Length,
                    OriginalFileHash = hash,
                    SourceFileName = $"notes-{i}.txt",
                    SourceFileSize = blob.Length,
                    SourceFileHash = hash,
                    HashAlgorithm = "Sha256",
                    ContentType = "text/plain",
                    Charset = "utf-8",
                    Timestamp = now,
                    PathPrefix = "files"
                };
            });

        files.ForEach(_ => 1, (file, _) => new FileDataEntity { FileId = Guid.Parse(file.Id), Data = blob });
        files.ForEach(
            _ => 1, (file, _) => new FileRevisionEntity {
                Id = LyoGuid.CreateV7(),
                ParentId = Guid.Parse(file.Id),
                StoredKey = $"files/{file.Id}",
                CreatedAt = now
            });

        files.ForEach(
            _ => 1, (file, _) => new FileAuditEventEntity {
                Id = LyoGuid.CreateV7(),
                EventType = "Created",
                Timestamp = now,
                FileId = Guid.Parse(file.Id),
                Outcome = "success"
            });

        files.ForEach(
            _ => 1, (file, _) => new MultipartUploadSessionEntity {
                SessionId = LyoGuid.CreateV7(),
                CreatedUtc = now,
                ExpiresUtc = now.AddHours(1),
                TargetFileId = Guid.Parse(file.Id),
                OriginalFileName = file.OriginalFileName,
                ContentType = "text/plain",
                Status = 0,
                ProviderKind = 0,
                ProviderState = "{}",
                PartSizeBytes = blob.Length
            });

        files.ForEach(
            _ => 1,
            (file, _) => new FileDownloadAccessLinkEntity {
                Id = LyoGuid.CreateV7(),
                FileId = Guid.Parse(file.Id),
                TokenHash = SHA256.HashData(Encoding.UTF8.GetBytes(file.Id)),
                CreatedUtc = now,
                ExpiresAtUtc = now.AddDays(-1)
            });
    }

    private static async Task ClearAsync(SeedSession session, CancellationToken ct)
    {
        if (session.Transport is not EfSeedTransport<FileMetadataStoreDbContext> ef)
            throw new SeedException("File metadata OnClear requires EfSeedTransport<FileMetadataStoreDbContext>.");

        var db = ef.Context;
        await db.FileDownloadAccessLinks.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.MultipartUploadSessions.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.FileAuditEvents.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.FileRevisions.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.FileData.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.FileMetadata.ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }
}