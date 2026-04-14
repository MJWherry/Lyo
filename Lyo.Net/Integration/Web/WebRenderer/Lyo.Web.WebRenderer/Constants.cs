namespace Lyo.Web.WebRenderer;

/// <summary>Consolidated constants for the WebRenderer library.</summary>
public static class Constants
{
    /// <summary>Metric names and tags.</summary>
    public static class Metrics
    {
        public const string RenderToHtmlDuration = "webrenderer.render_to_html.duration";
        public const string RenderToHtmlSuccess = "webrenderer.render_to_html.success";
        public const string RenderToHtmlFailure = "webrenderer.render_to_html.failure";
        public const string RenderToHtmlSizeBytes = "webrenderer.render_to_html.size_bytes";

        public const string RenderToHtmlBytesDuration = "webrenderer.render_to_html_bytes.duration";
        public const string RenderToHtmlBytesSuccess = "webrenderer.render_to_html_bytes.success";
        public const string RenderToHtmlBytesFailure = "webrenderer.render_to_html_bytes.failure";
        public const string RenderToHtmlBytesSizeBytes = "webrenderer.render_to_html_bytes.size_bytes";

        public const string RenderToFileDuration = "webrenderer.render_to_file.duration";
        public const string RenderToFileSuccess = "webrenderer.render_to_file.success";
        public const string RenderToFileFailure = "webrenderer.render_to_file.failure";

        public const string ConvertHtmlToPdfDuration = "webrenderer.convert_html_to_pdf.duration";
        public const string ConvertHtmlToPdfSuccess = "webrenderer.convert_html_to_pdf.success";
        public const string ConvertHtmlToPdfFailure = "webrenderer.convert_html_to_pdf.failure";
        public const string ConvertHtmlToPdfSizeBytes = "webrenderer.convert_html_to_pdf.size_bytes";
        public const string ConvertHtmlToPdfInputSizeBytes = "webrenderer.convert_html_to_pdf.input_size_bytes";

        public static class Tags
        {
            public const string ComponentType = "component_type";
            public const string Operation = "operation";
        }
    }
}