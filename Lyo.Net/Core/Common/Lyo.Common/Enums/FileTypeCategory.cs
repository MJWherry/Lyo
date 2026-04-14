namespace Lyo.Common.Enums;

/// <summary>Represents file type categories for grouping file types.</summary>
public enum FileTypeCategory
{
    /// <summary>Unknown or unsupported file type category</summary>
    Unknown = 0,

    /// <summary>Document file types (PDF, Word, Excel, etc.)</summary>
    Documents,

    /// <summary>Image file types (JPEG, PNG, GIF, etc.)</summary>
    Images,

    /// <summary>Audio file types (MP3, WAV, OGG, etc.)</summary>
    Audio,

    /// <summary>Compressed/archive file types (ZIP, RAR, 7Z, etc.)</summary>
    Compressed,

    /// <summary>Encrypted file types (GPG, ENC, etc.)</summary>
    Encrypted,

    /// <summary>Data/text file types (CSV, TXT, JSON, XML, etc.)</summary>
    DataFiles
}