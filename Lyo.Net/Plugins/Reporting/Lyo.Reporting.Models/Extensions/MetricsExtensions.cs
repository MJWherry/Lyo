using System.Net;
using System.Text.Json;
using Lyo.Metrics;
using Lyo.Metrics.Models;
using Lyo.Reporting.Models.Builders;
using Lyo.Reporting.Models.Controls;

namespace Lyo.Reporting.Models.Extensions;

/// <summary>Extension methods that add metrics visualizations to reports.</summary>
public static class MetricsExtensions
{
    /// <summary>
    /// Wraps a Chart.js configuration in the canvas markup the report viewer bootstraps. Chart blocks carry configuration data only. The viewer owns the script, because
    /// composition JSON is sanitized on render and inline script would be stripped.
    /// </summary>
    private static string GenerateChartHtml(string chartId, string title, int height, object config)
    {
        var configJson = WebUtility.HtmlEncode(JsonSerializer.Serialize(config));
        return $@"
<div style=""margin: 20px 0;"">
    <h3 style=""margin-bottom: 15px; color: #1e293b;"">{WebUtility.HtmlEncode(title)}</h3>
    <canvas id=""{chartId}"" style=""max-height: {height}px;"" {Constants.Charts.ConfigAttribute}=""{configJson}""></canvas>
</div>";
    }

    private static string GenerateBarChartHtml(string chartId, string title, string[] labels, long[] values, int height)
        => GenerateChartHtml(
            chartId, title, height, new {
                type = "bar",
                data = new {
                    labels,
                    datasets = new[] {
                        new {
                            label = "Value",
                            data = values,
                            backgroundColor = "rgba(37, 99, 235, 0.6)",
                            borderColor = "rgba(37, 99, 235, 1)",
                            borderWidth = 1
                        }
                    }
                },
                options = new {
                    responsive = true,
                    maintainAspectRatio = true,
                    plugins = new { legend = new { display = false }, title = new { display = false } },
                    scales = new { y = new { beginAtZero = true } }
                }
            });

    private static string GenerateGaugeChartHtml(string chartId, string title, string[] labels, double[] values, int height)
        => GenerateChartHtml(
            chartId, title, height, new {
                type = "doughnut",
                data = new {
                    labels,
                    datasets = new[] {
                        new {
                            label = "Value",
                            data = values,
                            backgroundColor = new[] {
                                "rgba(37, 99, 235, 0.6)", "rgba(16, 185, 129, 0.6)", "rgba(245, 158, 11, 0.6)", "rgba(239, 68, 68, 0.6)", "rgba(139, 92, 246, 0.6)"
                            },
                            borderColor = new[] { "rgba(37, 99, 235, 1)", "rgba(16, 185, 129, 1)", "rgba(245, 158, 11, 1)", "rgba(239, 68, 68, 1)", "rgba(139, 92, 246, 1)" },
                            borderWidth = 1
                        }
                    }
                },
                options = new { responsive = true, maintainAspectRatio = true, plugins = new { legend = new { position = "right" } } }
            });

    private static string GenerateHistogramChartHtml(string chartId, string title, List<HistogramData> histograms, int height)
    {
        var maxLength = histograms.Count > 0 ? histograms.Max(h => h.Values.Count) : 0;
        var labels = Enumerable.Range(0, maxLength).ToArray();

        // Padding used to live in the emitted script; it happens here now that the block carries data only.
        var datasets = histograms.Select((h, index) => new {
                label = h.Name,
                data = h.Values.Select(v => (double?)v).Concat(Enumerable.Repeat((double?)null, Math.Max(0, maxLength - h.Values.Count))).ToArray(),
                borderColor = GetColor(index),
                backgroundColor = GetColor(index, 0.1),
                fill = false
            })
            .ToArray();

        return GenerateChartHtml(
            chartId, title, height, new {
                type = "line",
                data = new { labels, datasets },
                options = new {
                    responsive = true,
                    maintainAspectRatio = true,
                    plugins = new { legend = new { display = true, position = "top" } },
                    scales = new { y = new { beginAtZero = true } }
                }
            });
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

    /// <param name="builder">Section builder</param>
    extension(SectionBuilder builder)
    {
        /// <summary>Adds a counter-metrics bar chart to the section.</summary>
        /// <param name="metrics">Metrics service (must be MetricsService to export data)</param>
        /// <param name="title">Chart title</param>
        /// <param name="counterNames">Optional list of counter names to include. When null, includes every counter.</param>
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
            return builder.AddBlock(ContentType.Chart, html);
        }

        /// <summary>Adds a gauge-metrics doughnut chart to the section.</summary>
        /// <param name="metrics">Metrics service (must be MetricsService to export data)</param>
        /// <param name="title">Chart title</param>
        /// <param name="gaugeNames">Optional list of gauge names to include. When null, includes every gauge.</param>
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
            return builder.AddBlock(ContentType.Chart, html);
        }

        /// <summary>Adds a histogram-metrics line chart to the section.</summary>
        /// <param name="metrics">Metrics service (must be MetricsService to export data)</param>
        /// <param name="title">Chart title</param>
        /// <param name="histogramNames">Optional list of histogram names to include. When null, includes every histogram.</param>
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
            return builder.AddBlock(ContentType.Chart, html);
        }

        /// <summary>Adds a full metrics dashboard covering every metric type.</summary>
        /// <param name="metrics">Metrics service (must be MetricsService to export data)</param>
        /// <param name="title">Section title</param>
        public SectionBuilder AddMetricsDashboard(IMetrics metrics, string title = "Metrics Dashboard")
            => builder.SetTitle(title).AddCounterChart(metrics).AddGaugeChart(metrics).AddHistogramChart(metrics);

        /// <summary>Adds a counter chart filtered by name prefix.</summary>
        /// <param name="metrics">Metrics service (must be MetricsService to export data)</param>
        /// <param name="prefix">Prefix used to filter counter names</param>
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
            return builder.AddBlock(ContentType.Chart, html);
        }

        /// <summary>Adds a counter chart filtered by tags.</summary>
        /// <param name="metrics">Metrics service (must be MetricsService to export data)</param>
        /// <param name="tags">Tag key-value pairs to filter by</param>
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
            return builder.AddBlock(ContentType.Chart, html);
        }

        /// <summary>Adds a counter chart filtered by a predicate.</summary>
        /// <param name="metrics">Metrics service (must be MetricsService to export data)</param>
        /// <param name="predicate">Function used to filter counters</param>
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
            return builder.AddBlock(ContentType.Chart, html);
        }
    }
}