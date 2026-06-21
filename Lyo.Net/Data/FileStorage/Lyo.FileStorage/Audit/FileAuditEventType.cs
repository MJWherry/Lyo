namespace Lyo.FileStorage.Audit;

public enum FileAuditEventType
{
    Save = 0,
    Read = 1,
    Delete = 2,
    MigrateDeks = 3,
    RotateDeks = 4,
    PresignedRead = 5,
    MultipartBegin = 6,
    MultipartComplete = 7,
    MultipartAbort = 8,
    AccessLinkAllowed = 9,
    AccessLinkDenied = 10,
    DirectUploadBegin = 11,
    DirectUploadComplete = 12,
    DirectUploadFailed = 13,
    Copy = 14,
    StagedUploadBegin = 15,
    StagedUploadComplete = 16,
    StagedUploadFailed = 17,
    StagedUploadCommit = 18,
    StagedUploadAbort = 19
}