using System.Globalization;
using System.Net;
using System.Text.Json;
using Lyo.Reporting.Models.Controls;

namespace Lyo.Reporting.Models.Composition;

/// <summary>
/// Builds Chart.js canvas markup for structured <see cref="Block.ChartKind" /> blocks. Does not execute Query or Sproc.
/// Legacy charts keep HTML in <see cref="Block.Content" /> and skip this helper.
/// </summary>
public static class ReportChartMarkup
{
    /// <summary>
    /// Canvas HTML for <paramref name="block" />, or null when the kind is unknown or there is no series to draw.
    /// </summary>
    /// <param name="block">Chart block (kind, series, caption, height).</param>
    /// <param name="canvasId">Stable canvas id. Designer paint passes the live block's id so a series copy does not change it.</param>
    public static string? TryBuild(Block block, string? canvasId = null)
    {
        if (block.ChartKind is null)
            return null;

        var series = ParseSeries(block.ListItems);
        if (series.Labels.Count == 0 && block.ChartKind is not ChartKind.Gauge)
            return null;

        var height = block.Level is > 0 ? block.Level.Value : block.ChartKind is ChartKind.Sparkline ? 48 : 300;
        var title = block.Caption ?? string.Empty;
        var chartId = canvasId ?? StableCanvasId(block);
        var config = block.ChartKind switch {
            ChartKind.Bar => Bar(series),
            ChartKind.Line => Line(series, sparkline: false),
            ChartKind.Sparkline => Line(series, sparkline: true),
            ChartKind.Doughnut => Doughnut(series),
            ChartKind.Pie => Pie(series),
            ChartKind.Gauge => Gauge(series),
            _ => null
        };
        return config is null ? null : Wrap(chartId, title, height, config);
    }

    /// <summary>Fills <see cref="Block.ListItems" /> as <c>label|value</c> from dataset rows. Missing data clears the list.</summary>
    public static void ApplyFromRows(Block block, IReadOnlyList<IReadOnlyDictionary<string, object?>>? rows)
        => block.ListItems = SeriesFromRows(block, rows);

    /// <summary>Builds <c>label|value</c> series without mutating <paramref name="block" />. Used by designer paint.</summary>
    public static List<string> SeriesFromRows(Block block, IReadOnlyList<IReadOnlyDictionary<string, object?>>? rows)
    {
        var items = new List<string>();
        if (rows is null || rows.Count == 0)
            return items;

        var labelField = string.IsNullOrWhiteSpace(block.ChartLabelField) ? "label" : block.ChartLabelField!;
        var valueField = string.IsNullOrWhiteSpace(block.ChartValueField) ? "value" : block.ChartValueField!;
        foreach (var row in rows) {
            var label = Lookup(row, labelField)?.ToString() ?? string.Empty;
            var value = Lookup(row, valueField);
            items.Add($"{label}|{FormatNumber(value)}");
        }

        return items;
    }

    /// <summary>Stable canvas id so Chart.js can redraw the same node after a Blazor re-render.</summary>
    public static string StableCanvasId(Block block)
    {
        var payload = string.Join("\\n", [block.ChartKind?.ToString() ?? string.Empty, block.Caption ?? string.Empty, block.Level?.ToString(CultureInfo.InvariantCulture) ?? string.Empty, ..block.ListItems ?? []]);
        var content = (uint)StringComparer.Ordinal.GetHashCode(payload);
        var identity = (uint)System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(block);
        return "chart-" + identity.ToString("x8", CultureInfo.InvariantCulture) + content.ToString("x8", CultureInfo.InvariantCulture);
    }

    private static (List<string> Labels, List<double> Values) ParseSeries(List<string>? items)
    {
        var labels = new List<string>();
        var values = new List<double>();
        if (items is null)
            return (labels, values);

        foreach (var item in items) {
            var split = item.IndexOf('|');
            var label = split < 0 ? item : item[..split];
            var raw = split < 0 ? "0" : item[(split + 1)..];
            labels.Add(label);
            values.Add(double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var n) ? n : 0);
        }

        return (labels, values);
    }

    private static object Bar((List<string> Labels, List<double> Values) series)
        => new {
            type = "bar",
            data = new {
                labels = series.Labels,
                datasets = new[] {
                    new {
                        label = "Value",
                        data = series.Values,
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
        };

    private static object Line((List<string> Labels, List<double> Values) series, bool sparkline)
    {
        object scales = sparkline
            ? new { x = new { display = false }, y = new { display = false } }
            : new { x = new { display = true }, y = new { display = true, beginAtZero = true } };
        return new {
            type = "line",
            data = new {
                labels = series.Labels,
                datasets = new[] {
                    new {
                        label = "Value",
                        data = series.Values,
                        borderColor = "rgba(37, 99, 235, 1)",
                        backgroundColor = "rgba(37, 99, 235, 0.1)",
                        fill = !sparkline,
                        pointRadius = sparkline ? 0 : 3,
                        borderWidth = sparkline ? 1.5 : 2
                    }
                }
            },
            options = new {
                responsive = true,
                maintainAspectRatio = !sparkline,
                plugins = new { legend = new { display = false }, title = new { display = false } },
                scales
            }
        };
    }

    private static object Doughnut((List<string> Labels, List<double> Values) series)
        => new {
            type = "doughnut",
            data = new {
                labels = series.Labels,
                datasets = new[] {
                    new {
                        data = series.Values,
                        backgroundColor = Palette(0.6),
                        borderColor = Palette(1),
                        borderWidth = 1
                    }
                }
            },
            options = new { responsive = true, maintainAspectRatio = true, plugins = new { legend = new { position = "right" } } }
        };

    private static object Pie((List<string> Labels, List<double> Values) series)
        => new {
            type = "pie",
            data = new {
                labels = series.Labels,
                datasets = new[] {
                    new {
                        data = series.Values,
                        backgroundColor = Palette(0.6),
                        borderColor = Palette(1),
                        borderWidth = 1
                    }
                }
            },
            options = new { responsive = true, maintainAspectRatio = true, plugins = new { legend = new { position = "right" } } }
        };

    private static object Gauge((List<string> Labels, List<double> Values) series)
    {
        var value = series.Values.Count > 0 ? series.Values[0] : 0;
        if (value < 0)
            value = 0;
        if (value > 100)
            value = 100;

        var rest = 100 - value;
        return new {
            type = "doughnut",
            data = new {
                labels = new[] { series.Labels.Count > 0 ? series.Labels[0] : "Value", "Remaining" },
                datasets = new[] {
                    new {
                        data = new[] { value, rest },
                        backgroundColor = new[] { "rgba(37, 99, 235, 0.8)", "rgba(226, 232, 240, 1)" },
                        borderWidth = 0
                    }
                }
            },
            options = new {
                responsive = true,
                maintainAspectRatio = true,
                rotation = -90,
                circumference = 180,
                plugins = new { legend = new { display = false } }
            }
        };
    }

    private static string Wrap(string chartId, string title, int height, object config)
    {
        var configJson = WebUtility.HtmlEncode(JsonSerializer.Serialize(config));
        var heading = string.IsNullOrWhiteSpace(title)
            ? string.Empty
            : $"<h3 style=\"margin-bottom: 15px; color: #1e293b;\">{WebUtility.HtmlEncode(title)}</h3>";
        return $"""

            <div style="margin: 20px 0;">
                {heading}
                <canvas id="{chartId}" style="max-height: {height.ToString(CultureInfo.InvariantCulture)}px;" {Constants.Charts.ConfigAttribute}="{configJson}"></canvas>
            </div>
            """;
    }

    private static string[] Palette(double alpha)
    {
        var a = alpha.ToString("F1", CultureInfo.InvariantCulture);
        return [
            $"rgba(37, 99, 235, {a})", "rgba(16, 185, 129, " + a + ")", "rgba(245, 158, 11, " + a + ")", "rgba(239, 68, 68, " + a + ")", "rgba(139, 92, 246, " + a + ")"
        ];
    }

    private static object? Lookup(IReadOnlyDictionary<string, object?> row, string name)
    {
        if (row.TryGetValue(name, out var value))
            return value;

        foreach (var kvp in row) {
            if (string.Equals(kvp.Key, name, StringComparison.OrdinalIgnoreCase))
                return kvp.Value;
        }

        return null;
    }

    private static string FormatNumber(object? value)
    {
        if (value is null)
            return "0";

        if (value is JsonElement el) {
            if (el.ValueKind == JsonValueKind.Number)
                return el.GetRawText();

            value = el.ToString();
        }

        if (value is IFormattable formattable)
            return formattable.ToString(null, CultureInfo.InvariantCulture);

        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "0";
    }
}
