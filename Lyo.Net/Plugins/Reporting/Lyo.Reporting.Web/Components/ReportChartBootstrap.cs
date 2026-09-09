using Lyo.Reporting.Models;
using Lyo.Reporting.Models.Composition;
using Lyo.Reporting.Models.Controls;
using Lyo.Reporting.Models.Models;
using Microsoft.AspNetCore.Components;

namespace Lyo.Reporting.Web.Components;

/// <summary>Chart.js bootstrap shared by <see cref="ReportViewer{T}" /> and the design canvas.</summary>
public static class ReportChartBootstrap
{
    /// <summary>True when any nested block is a chart.</summary>
    public static bool SectionHasChart(Section section)
        => SectionBody.WalkControls(section.Controls).OfType<Block>().Any(b => b.ContentType == ContentType.Chart) || section.Subsections.Any(SectionHasChart);

    /// <summary>True when the report contains at least one chart block.</summary>
    public static bool ReportHasChart(IEnumerable<Section> sections) => sections.Any(SectionHasChart);

    /// <summary>Script tags that load Chart.js and draw every canvas with <c>data-lyo-chart</c>.</summary>
    public static RenderFragment Render(bool inlineChartScript, string chartScriptUrl)
        => b => {
            if (!inlineChartScript && string.IsNullOrWhiteSpace(chartScriptUrl))
                return;

            b.OpenElement(0, "script");
            if (inlineChartScript)
                b.AddMarkupContent(1, ChartScript.Bundle);
            else
                b.AddAttribute(1, "src", chartScriptUrl);

            b.CloseElement();
            b.OpenElement(2, "script");
            b.AddMarkupContent(3, LoaderScript);
            b.CloseElement();
        };

    private static readonly string LoaderScript = $$"""

        (function () {
            if (window.__lyoChartWatch) return;
            window.__lyoChartWatch = true;
            function drawOne(canvas) {
                if (typeof Chart === 'undefined') return;
                var raw = canvas.getAttribute('{{Constants.Charts.ConfigAttribute}}');
                if (!raw) return;
                if (canvas.dataset.lyoChartFp === raw && canvas.dataset.lyoChartDrawn) return;
                try {
                    var existing = typeof Chart.getChart === 'function' ? Chart.getChart(canvas) : null;
                    if (existing) existing.destroy();
                    new Chart(canvas, JSON.parse(raw));
                    canvas.dataset.lyoChartDrawn = '1';
                    canvas.dataset.lyoChartFp = raw;
                } catch (e) {
                    console.error('Failed to draw report chart', e);
                }
            }
            function draw() {
                document.querySelectorAll('canvas[{{Constants.Charts.ConfigAttribute}}]').forEach(drawOne);
            }
            var obs = new MutationObserver(draw);
            obs.observe(document.documentElement, { childList: true, subtree: true, attributes: true, attributeFilter: ['{{Constants.Charts.ConfigAttribute}}'] });
            if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', draw);
            else draw();
        })();

        """;
}
