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

        public const string CreateRandomFileDuration = "io.temp.file.random.create.duration";
        public const string CreateRandomFileSuccess = "io.temp.file.random.create.success";
        public const string CreateRandomFileFailure = "io.temp.file.random.create.failure";

        public const string SimulateDirectoryDuration = "io.temp.directory.simulate.duration";
        public const string SimulateDirectorySuccess = "io.temp.directory.simulate.success";
        public const string SimulateDirectoryFailure = "io.temp.directory.simulate.failure";

        public const string CreateTextFileDuration = "io.temp.file.text.create.duration";
        public const string CreateTextFileSuccess = "io.temp.file.text.create.success";
        public const string CreateTextFileFailure = "io.temp.file.text.create.failure";

        public const string CreateCsvFileDuration = "io.temp.file.csv.create.duration";
        public const string CreateCsvFileSuccess = "io.temp.file.csv.create.success";
        public const string CreateCsvFileFailure = "io.temp.file.csv.create.failure";

        public const string CreateJsonFileDuration = "io.temp.file.json.create.duration";
        public const string CreateJsonFileSuccess = "io.temp.file.json.create.success";
        public const string CreateJsonFileFailure = "io.temp.file.json.create.failure";

        public const string CreateZipFileDuration = "io.temp.file.zip.create.duration";
        public const string CreateZipFileSuccess = "io.temp.file.zip.create.success";
        public const string CreateZipFileFailure = "io.temp.file.zip.create.failure";

        public const string AppendToFileDuration = "io.temp.file.append.duration";
        public const string AppendToFileSuccess = "io.temp.file.append.success";
        public const string AppendToFileFailure = "io.temp.file.append.failure";

        public const string CopyFromDuration = "io.temp.copy.from.duration";
        public const string CopyFromSuccess = "io.temp.copy.from.success";
        public const string CopyFromFailure = "io.temp.copy.from.failure";

        public const string MoveFromDuration = "io.temp.move.from.duration";
        public const string MoveFromSuccess = "io.temp.move.from.success";
        public const string MoveFromFailure = "io.temp.move.from.failure";

        public const string WriteFileDuration = "io.temp.file.write.duration";
        public const string WriteFileSuccess = "io.temp.file.write.success";
        public const string WriteFileFailure = "io.temp.file.write.failure";

        public const string DeleteFileDuration = "io.temp.file.delete.duration";
        public const string DeleteFileSuccess = "io.temp.file.delete.success";
        public const string DeleteFileFailure = "io.temp.file.delete.failure";

        public const string DeleteDirectoryDuration = "io.temp.directory.delete.duration";
        public const string DeleteDirectorySuccess = "io.temp.directory.delete.success";
        public const string DeleteDirectoryFailure = "io.temp.directory.delete.failure";

        public const string CreateXmlFileDuration = "io.temp.file.xml.create.duration";
        public const string CreateXmlFileSuccess = "io.temp.file.xml.create.success";
        public const string CreateXmlFileFailure = "io.temp.file.xml.create.failure";

        public const string ExtractZipFileDuration = "io.temp.file.zip.extract.duration";
        public const string ExtractZipFileSuccess = "io.temp.file.zip.extract.success";
        public const string ExtractZipFileFailure = "io.temp.file.zip.extract.failure";

        public const string CreateDirectoryTreeDuration = "io.temp.directory.tree.create.duration";
        public const string CreateDirectoryTreeSuccess = "io.temp.directory.tree.create.success";
        public const string CreateDirectoryTreeFailure = "io.temp.directory.tree.create.failure";
    }
}