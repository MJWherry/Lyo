namespace Lyo.Reporting.Models;

/// <summary>Consolidated constants for the Reporting library.</summary>
public static class Constants
{
    /// <summary>REST API route constants.</summary>
    public static class Rest
    {
        public static class Reporting
        {
            public const string Route = "Reporting";
            public const string Definitions = $"{Route}/Definition";
            public const string DefinitionsQuery = $"{Definitions}/QueryConcrete";
            public const string DefinitionParameters = $"{Definitions}/Parameter";
            public const string DefinitionParametersQuery = $"{DefinitionParameters}/QueryConcrete";
            public const string Generations = $"{Route}/Generation";
            public const string GenerationsQuery = $"{Generations}/QueryConcrete";

            /// <summary>POST endpoint that generates a report via <c>ReportService.GenerateAsync</c>.</summary>
            public const string GenerationsGenerate = $"{Generations}/Generate";

            /// <summary>GET endpoint (suffix under <c>Reporting/Generation/{id}</c>) that streams a generation's persisted output.</summary>
            public const string GenerationsDownloadSuffix = "Download";

            /// <summary>POST endpoint (suffix under <c>Reporting/Generation/{id}</c>) that re-runs a past generation from its stored snapshot.</summary>
            public const string GenerationsRerunSuffix = "Rerun";
        }
    }

    /// <summary>Metric names emitted by the reporting system.</summary>
    public static class Metrics
    {
        public const string GenerationStarted = "reporting.generation.started";
        public const string GenerationSucceeded = "reporting.generation.succeeded";
        public const string GenerationFailed = "reporting.generation.failed";
        public const string GenerationCleaned = "reporting.generation.cleaned";
        public const string GenerationCleanupSkipped = "reporting.generation.cleanup_skipped";
        public const string GenerationStuckRecovered = "reporting.generation.stuck_recovered";
    }
}