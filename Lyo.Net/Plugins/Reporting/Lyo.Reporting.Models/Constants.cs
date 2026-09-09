namespace Lyo.Reporting.Models;

/// <summary>Shared constants for the Reporting library.</summary>
public static class Constants
{
    /// <summary>REST API route names.</summary>
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

            /// <summary>POST endpoint that generates a report through <c>ReportService.GenerateAsync</c>.</summary>
            public const string GenerationsGenerate = $"{Generations}/Generate";

            /// <summary>POST endpoint that resolves Query/Sproc parameter Options into picker rows (workbench preview).</summary>
            public const string ResolveParameterOptions = $"{Route}/ResolveParameterOptions";

            /// <summary>GET endpoint (suffix under <c>Reporting/Generation/{id}</c>) that streams a generation's saved output.</summary>
            public const string GenerationsDownloadSuffix = "Download";

            /// <summary>POST endpoint (suffix under <c>Reporting/Generation/{id}</c>) that re-runs an earlier generation from its stored snapshot.</summary>
            public const string GenerationsRerunSuffix = "Rerun";
        }
    }

    /// <summary>Chart rendering contract used by both the chart builders and the report viewer.</summary>
    public static class Charts
    {
        /// <summary>
        /// Attribute on a <c>canvas</c> element holding the chart configuration as JSON. Chart blocks carry data only: the viewer supplies the bootstrap script, because markup
        /// arriving in composition JSON is sanitized and cannot bring its own script.
        /// </summary>
        public const string ConfigAttribute = "data-lyo-chart";

        /// <summary>
        /// Default Chart.js source used by the viewer: the copy shipped as a static web asset of <c>Lyo.Reporting.Web</c>. Server-side PDF rendering has no outbound network
        /// access in most deployments, and a CDN reference is also a live supply-chain dependency for every rendered report, so the bundle is vendored. Hosts that would
        /// rather use a CDN can set <see cref="CdnScriptUrl" /> on the viewer.
        /// </summary>
        public const string DefaultScriptUrl = "_content/Lyo.Reporting.Web/scripts/chart.umd.min.js";

        /// <summary>Public CDN copy of the Chart.js version the local bundle pins. Opt in by assigning it to the viewer's <c>ChartScriptUrl</c>.</summary>
        public const string CdnScriptUrl = "https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js";
    }

    /// <summary>Metric names the reporting system emits.</summary>
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