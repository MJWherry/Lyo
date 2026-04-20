using System.Text.Json;
using Lyo.Metrics;
using Lyo.Metrics.Models;
using Lyo.Web.Reporting.Builders;
using Lyo.Web.Reporting.Models;

namespace Lyo.Web.Reporting.Extensions;

/// <summary>Extension methods for adding metrics visualizations to reports.</summary>
public static class MetricsExtensions
{
    private static string GenerateBarChartHtml(string chartId, string title, string[] labels, long[] values, int height)
    {
        var labelsJson = JsonSerializer.Serialize(labels);
        var valuesJson = JsonSerializer.Serialize(values);
        return $@"
<div style=""margin: 20px 0;"">
    <h3 style=""margin-bottom: 15px; color: #1e293b;"">{title}</h3>
    <canvas id=""{chartId}"" style=""max-height: {height}px;""></canvas>
</div>
<script src=""https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js""></script>
<script>
(function() {{
    const ctx = document.getElementById('{chartId}');
    if (!ctx) return;
    
    new Chart(ctx, {{
        type: 'bar',
        data: {{
            labels: {labelsJson},
            datasets: [{{
                label: 'Value',
                data: {valuesJson},
                backgroundColor: 'rgba(37, 99, 235, 0.6)',
                borderColor: 'rgba(37, 99, 235, 1)',
                borderWidth: 1
            }}]
        }},
        options: {{
            responsive: true,
            maintainAspectRatio: true,
            plugins: {{
                legend: {{
                    display: false
                }},
                title: {{
                    display: false
                }}
            }},
            scales: {{
                y: {{
                    beginAtZero: true
                }}
            }}
        }}
    }});
}})();
</script>";
    }

    private static string GenerateGaugeChartHtml(string chartId, string title, string[] labels, double[] values, int height)
    {
        var labelsJson = JsonSerializer.Serialize(labels);
        var valuesJson = JsonSerializer.Serialize(values);
        return $@"
<div style=""margin: 20px 0;"">
    <h3 style=""margin-bottom: 15px; color: #1e293b;"">{title}</h3>
    <canvas id=""{chartId}"" style=""max-height: {height}px;""></canvas>
</div>
<script src=""https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js""></script>
<script>
(function() {{
    const ctx = document.getElementById('{chartId}');
    if (!ctx) return;
    
    new Chart(ctx, {{
        type: 'doughnut',
        data: {{
            labels: {labelsJson},
            datasets: [{{
                label: 'Value',
                data: {valuesJson},
                backgroundColor: [
                    'rgba(37, 99, 235, 0.6)',
                    'rgba(16, 185, 129, 0.6)',
                    'rgba(245, 158, 11, 0.6)',
                    'rgba(239, 68, 68, 0.6)',
                    'rgba(139, 92, 246, 0.6)'
                ],
                borderColor: [
                    'rgba(37, 99, 235, 1)',
                    'rgba(16, 185, 129, 1)',
                    'rgba(245, 158, 11, 1)',
                    'rgba(239, 68, 68, 1)',
                    'rgba(139, 92, 246, 1)'
                ],
                borderWidth: 1
            }}]
        }},
        options: {{
            responsive: true,
            maintainAspectRatio: true,
            plugins: {{
                legend: {{
                    position: 'right'
                }}
            }}
        }}
    }});
}})();
</script>";
    }

    private static string GenerateHistogramChartHtml(string chartId, string title, List<HistogramData> histograms, int height)
    {
        var datasets = histograms.Select((h, index) => new {
                label = h.Name,
                data = h.Values.ToArray(),
                borderColor = GetColor(index),
                backgroundColor = GetColor(index, 0.1),
                fill = false
            })
            .ToArray();

        var datasetsJson = JsonSerializer.Serialize(datasets);
        var maxLength = histograms.Count > 0 ? histograms.Max(h => h.Values.Count) : 0;
        var labels = Enumerable.Range(0, maxLength).ToArray();
        var labelsJson = JsonSerializer.Serialize(labels);
        return $@"
<div style=""margin: 20px 0;"">
    <h3 style=""margin-bottom: 15px; color: #1e293b;"">{title}</h3>
    <canvas id=""{chartId}"" style=""max-height: {height}px;""></canvas>
</div>
<script src=""https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js""></script>
<script>
(function() {{
    const ctx = document.getElementById('{chartId}');
    if (!ctx) return;
    
    const datasets = {datasetsJson};
    const labels = {labelsJson};
    
    // Pad datasets to same length
    const paddedDatasets = datasets.map(d => ({{
        ...d,
        data: [...d.data, ...Array(Math.max(0, labels.length - d.data.length)).fill(null)]
    }}));
    
    new Chart(ctx, {{
        type: 'line',
        data: {{
            labels: labels,
            datasets: paddedDatasets
        }},
        options: {{
            responsive: true,
            maintainAspectRatio: true,
            plugins: {{
                legend: {{
                    display: true,
                    position: 'top'
                }}
            }},
            scales: {{
                y: {{
                    beginAtZero: true
                }}
            }}
        }}
    }});
}})();
</script>";
    }

    private static string GetColor(int index, double alpha = 1.0)
    {
        var colors = new[] {
            "rgba(37, 99, 235, {alpha})", // Blue
            "rgba(16, 185, 129, {alpha})", // Green
            "rgba(245, 158, 11, {alpha})", // Yellow
            "rgba(239, 68, 68, {alpha})", // Red
            "rgba(139, 92, 246, {alpha})", // Purple
            "rgba(236, 72, 153, {alpha})" // Pink
        };

        return colors[index % colors.Length].Replace("{alpha}", alpha.ToString("F1"));
    }

    /// <param name="builder">The section builder</param>
    extension(SectionBuilder builder)
    {
        /// <summary>Adds a counter metrics bar chart to the section.</summary>
        /// <param name="metrics">The metrics service (must be MetricsService to export data)</param>
        /// <param name="title">Chart title</param>
        /// <param name="counterNames">Optional list of counter names to include. If null, includes all counters.</param>
        /// <param name="height">Chart height in pixels (default: 300)</param>
        public SectionBuilder AddCounterChart(IMetrics metrics, string title = "Counter Metrics", IEnumerable<string>? counterNames = null, int height = 300)
        {
            if (metrics is not MetricsService metricsService)
                throw new ArgumentException("IMetrics must be MetricsService to export data", nameof(metrics));

            var snapshot = metricsService.Export();
            var counters = counterNames == null ? snapshot.Counters.Values.ToList() : snapshot.Counters.Values.Where(c => counterNames.Contains(c.Name)).ToList();
            if (counters.Count == 0)
                return builder.AddText("No counter metrics available.");

            var chartId = $"chart-counter-{Guid.NewGuid():N}";
            var labels = counters.Select(c => c.Name).ToArray();
            var values = counters.Select(c => c.Value).ToArray();
            var html = GenerateBarChartHtml(chartId, title, labels, values, height);
            return builder.AddContentBlock(ReportContentType.Chart, html);
        }

        /// <summary>Adds a gauge metrics doughnut chart to the section.</summary>
        /// <param name="metrics">The metrics service (must be MetricsService to export data)</param>
        /// <param name="title">Chart title</param>
        /// <param name="gaugeNames">Optional list of gauge names to include. If null, includes all gauges.</param>
        /// <param name="height">Chart height in pixels (default: 300)</param>
        public SectionBuilder AddGaugeChart(IMetrics metrics, string title = "Gauge Metrics", IEnumerable<string>? gaugeNames = null, int height = 300)
        {
            if (metrics is not MetricsService metricsService)
                throw new ArgumentException("IMetrics must be MetricsService to export data", nameof(metrics));

            var snapshot = metricsService.Export();
            var gauges = gaugeNames == null ? snapshot.Gauges.Values.ToList() : snapshot.Gauges.Values.Where(g => gaugeNames.Contains(g.Name)).ToList();
            if (gauges.Count == 0)
                return builder.AddText("No gauge metrics available.");

            var chartId = $"chart-gauge-{Guid.NewGuid():N}";
            var labels = gauges.Select(g => g.Name).ToArray();
            var values = gauges.Select(g => g.Value).ToArray();
            var html = GenerateGaugeChartHtml(chartId, title, labels, values, height);
            return builder.AddContentBlock(ReportContentType.Chart, html);
        }

        /// <summary>Adds a histogram metrics line chart to the section.</summary>
        /// <param name="metrics">The metrics service (must be MetricsService to export data)</param>
        /// <param name="title">Chart title</param>
        /// <param name="histogramNames">Optional list of histogram names to include. If null, includes all histograms.</param>
        /// <param name="height">Chart height in pixels (default: 300)</param>
        public SectionBuilder AddHistogramChart(IMetrics metrics, string title = "Histogram Metrics", IEnumerable<string>? histogramNames = null, int height = 300)
        {
            if (metrics is not MetricsService metricsService)
                throw new ArgumentException("IMetrics must be MetricsService to export data", nameof(metrics));

            var snapshot = metricsService.Export();
            var histograms = histogramNames == null ? snapshot.Histograms.Values.ToList() : snapshot.Histograms.Values.Where(h => histogramNames.Contains(h.Name)).ToList();
            if (histograms.Count == 0)
                return builder.AddText("No histogram metrics available.");

            var chartId = $"chart-histogram-{Guid.NewGuid():N}";
            var html = GenerateHistogramChartHtml(chartId, title, histograms, height);
            return builder.AddContentBlock(ReportContentType.Chart, html);
        }

        /// <summary>Adds a comprehensive metrics dashboard with all metric types.</summary>
        /// <param name="metrics">The metrics service (must be MetricsService to export data)</param>
        /// <param name="title">Section title</param>
        public SectionBuilder AddMetricsDashboard(IMetrics metrics, string title = "Metrics Dashboard")
            => builder.SetTitle(title).AddCounterChart(metrics).AddGaugeChart(metrics).AddHistogramChart(metrics);

        /// <summary>Adds a counter chart filtered by name prefix.</summary>
        /// <param name="metrics">The metrics service (must be MetricsService to export data)</param>
        /// <param name="prefix">The prefix to filter counter names by</param>
        /// <param name="title">Chart title</param>
        /// <param name="height">Chart height in pixels (default: 300)</param>
        public SectionBuilder AddCounterChartByPrefix(IMetrics metrics, string prefix, string title = "Counter Metrics", int height = 300)
        {
            if (metrics is not MetricsService metricsService)
                throw new ArgumentException("IMetrics must be MetricsService to export data", nameof(metrics));

            var snapshot = metricsService.Export();
            var counters = snapshot.Counters.Values.Where(c => c.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
            if (counters.Count == 0)
                return builder.AddText($"No counter metrics found with prefix '{prefix}'.");

            var chartId = $"chart-counter-{Guid.NewGuid():N}";
            var labels = counters.Select(c => c.Name).ToArray();
            var values = counters.Select(c => c.Value).ToArray();
            var html = GenerateBarChartHtml(chartId, title, labels, values, height);
            return builder.AddContentBlock(ReportContentType.Chart, html);
        }

        /// <summary>Adds a counter chart filtered by tags.</summary>
        /// <param name="metrics">The metrics service (must be MetricsService to export data)</param>
        /// <param name="tags">Dictionary of tag key-value pairs to filter by</param>
        /// <param name="title">Chart title</param>
        /// <param name="height">Chart height in pixels (default: 300)</param>
        public SectionBuilder AddCounterChartByTags(IMetrics metrics, Dictionary<string, string> tags, string title = "Counter Metrics", int height = 300)
        {
            if (metrics is not MetricsService metricsService)
                throw new ArgumentException("IMetrics must be MetricsService to export data", nameof(metrics));

            var snapshot = metricsService.Export();
            var counters = snapshot.Counters.Values.Where(c => c.Tags != null && tags.All(tag => c.Tags.ContainsKey(tag.Key) && c.Tags[tag.Key] == tag.Value)).ToList();
            if (counters.Count == 0)
                return builder.AddText("No counter metrics found matching the specified tags.");

            var chartId = $"chart-counter-{Guid.NewGuid():N}";
            var labels = counters.Select(c => c.Name).ToArray();
            var values = counters.Select(c => c.Value).ToArray();
            var html = GenerateBarChartHtml(chartId, title, labels, values, height);
            return builder.AddContentBlock(ReportContentType.Chart, html);
        }

        /// <summary>Adds a counter chart filtered by a predicate function.</summary>
        /// <param name="metrics">The metrics service (must be MetricsService to export data)</param>
        /// <param name="predicate">Function to filter counters</param>
        /// <param name="title">Chart title</param>
        /// <param name="height">Chart height in pixels (default: 300)</param>
        public SectionBuilder AddCounterChartFiltered(IMetrics metrics, Func<CounterData, bool> predicate, string title = "Counter Metrics", int height = 300)
        {
            if (metrics is not MetricsService metricsService)
                throw new ArgumentException("IMetrics must be MetricsService to export data", nameof(metrics));

            var snapshot = metricsService.Export();
            var counters = snapshot.Counters.Values.Where(predicate).ToList();
            if (counters.Count == 0)
                return builder.AddText("No counter metrics found matching the filter criteria.");

            var chartId = $"chart-counter-{Guid.NewGuid():N}";
            var labels = counters.Select(c => c.Name).ToArray();
            var values = counters.Select(c => c.Value).ToArray();
            var html = GenerateBarChartHtml(chartId, title, labels, values, height);
            return builder.AddContentBlock(ReportContentType.Chart, html);
        }
    }
}