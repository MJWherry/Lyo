namespace Lyo.Pdf;

/// <summary>Consolidated constants for the Pdf library.</summary>
public static class Constants
{
    /// <summary>Constants for PDF service metrics.</summary>
    public static class Metrics
    {
        public const string LoadDuration = "pdf.load.duration";
        public const string LoadSuccess = "pdf.load.success";
        public const string LoadFailure = "pdf.load.failure";

        public const string UnloadDuration = "pdf.unload.duration";
        public const string UnloadSuccess = "pdf.unload.success";
        public const string UnloadFailure = "pdf.unload.failure";

        public const string InfoDuration = "pdf.info.duration";
        public const string InfoSuccess = "pdf.info.success";
        public const string InfoFailure = "pdf.info.failure";

        public const string WordsDuration = "pdf.words.duration";
        public const string WordsSuccess = "pdf.words.success";
        public const string WordsFailure = "pdf.words.failure";

        public const string LinesDuration = "pdf.lines.duration";
        public const string LinesSuccess = "pdf.lines.success";
        public const string LinesFailure = "pdf.lines.failure";

        public const string WordsBetweenDuration = "pdf.words.between.duration";
        public const string WordsBetweenSuccess = "pdf.words.between.success";
        public const string WordsBetweenFailure = "pdf.words.between.failure";

        public const string LinesBetweenDuration = "pdf.lines.between.duration";
        public const string LinesBetweenSuccess = "pdf.lines.between.success";
        public const string LinesBetweenFailure = "pdf.lines.between.failure";

        public const string ExtractKeyValueDuration = "pdf.extract.keyvalue.duration";
        public const string ExtractKeyValueSuccess = "pdf.extract.keyvalue.success";
        public const string ExtractKeyValueFailure = "pdf.extract.keyvalue.failure";

        public const string ExtractTableDuration = "pdf.extract.table.duration";
        public const string ExtractTableSuccess = "pdf.extract.table.success";
        public const string ExtractTableFailure = "pdf.extract.table.failure";

        public const string SaveDuration = "pdf.save.duration";
        public const string SaveSuccess = "pdf.save.success";
        public const string SaveFailure = "pdf.save.failure";

        public const string BytesDuration = "pdf.bytes.duration";
        public const string BytesSuccess = "pdf.bytes.success";
        public const string BytesFailure = "pdf.bytes.failure";

        public const string MergeDuration = "pdf.merge.duration";
        public const string MergeSuccess = "pdf.merge.success";
        public const string MergeFailure = "pdf.merge.failure";
    }
}