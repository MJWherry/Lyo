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
    MultipartAbort = 8
}