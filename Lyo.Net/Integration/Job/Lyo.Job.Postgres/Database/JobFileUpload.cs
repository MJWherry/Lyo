namespace Lyo.Job.Postgres.Database;

/// <summary>Entity matching job.sql schema with encrypted_data_encryption_key and data_encryption_key_version.</summary>
public class JobFileUpload
{
    public Guid Id { get; set; }

    public DateTime UploadTimestamp { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public string OriginalFilename { get; set; } = null!;

    public long OriginalSize { get; set; }

    public byte[] OriginalHash { get; set; } = null!;

    public string SourceDirectory { get; set; } = null!;

    public string SourceFilename { get; set; } = null!;

    public long SourceSize { get; set; }

    public byte[] SourceHash { get; set; } = null!;

    public byte[] EncryptedDataEncryptionKey { get; set; } = null!;

    public int DataEncryptionKeyVersion { get; set; }
}