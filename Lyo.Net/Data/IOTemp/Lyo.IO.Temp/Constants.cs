namespace Lyo.IO.Temp;

/// <summary>Consolidated constants for the IO Temp library.</summary>
public static class Constants
{
    /// <summary>Constants for IO temp service and session metrics.</summary>
    public static class Metrics
    {
        public const string CreateServiceDirectoryDuration = "io.temp.service.directory.create.duration";
        public const string CreateServiceDirectorySuccess = "io.temp.service.directory.create.success";
        public const string CreateServiceDirectoryFailure = "io.temp.service.directory.create.failure";

        public const string DisposeServiceDirectoryDuration = "io.temp.service.directory.dispose.duration";
        public const string DisposeServiceDirectorySuccess = "io.temp.service.directory.dispose.success";
        public const string DisposeServiceDirectoryFailure = "io.temp.service.directory.dispose.failure";

        public const string ActiveSessionCount = "io.temp.sessions.active";

        public const string CreateSessionDuration = "io.temp.session.create.duration";
        public const string CreateSessionSuccess = "io.temp.session.create.success";
        public const string CreateSessionFailure = "io.temp.session.create.failure";

        public const string SessionCreated = "io.temp.session.created";
        public const string DisposeSessionDuration = "io.temp.session.dispose.duration";
        public const string DisposeSessionSuccess = "io.temp.session.dispose.success";
        public const string DisposeSessionFailure = "io.temp.session.dispose.failure";

        public const string CreateFileDuration = "io.temp.file.create.duration";
        public const string CreateFileSuccess = "io.temp.file.create.success";
        public const string CreateFileFailure = "io.temp.file.create.failure";

        public const string CreateDirectoryDuration = "io.temp.directory.create.duration";
        public const string CreateDirectorySuccess = "io.temp.directory.create.success";
        public const string CreateDirectoryFailure = "io.temp.directory.create.failure";

        public const string CleanupDuration = "io.temp.cleanup.duration";
        public const string CleanupSuccess = "io.temp.cleanup.success";
    }
}