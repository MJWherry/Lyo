# Lyo.Web.Reporting

A powerful, fluent API for building and rendering professional reports in .NET applications. Built on Blazor components, this library provides a type-safe, builder-pattern approach
to creating structured reports with sections, columns, grids, content blocks, and metrics visualizations.

## Features

- ✅ **Fluent Builder API** - Type-safe, chainable methods for building reports
- ✅ **Rich Content Types** - Sections, columns, grids, text, HTML, lists, code blocks, quotes, and charts
- ✅ **Metrics Visualization** - Built-in support for visualizing metrics data with Chart.js
- ✅ **Customizable Styling** - CSS styling support at report, section, and element levels
- ✅ **Blazor Component Rendering** - Uses Blazor components for HTML generation
- ✅ **Type-Safe Parameters** - Generic type support for report parameters/options
- ✅ **Metadata Support** - Attach custom metadata to reports
- ✅ **Nested Sections** - Support for subsections and hierarchical organization

## Quick Start

### 1. Build a Report

```csharp
using Lyo.Web.Reporting.Builders;
using Lyo.Web.Reporting.Models;

var report = ReportBuilder<string>.New()
    .SetTitle("My First Report")
    .SetSubtitle("Generated Report")
    .AddSection("Summary", section => section
        .AddColumn("Total Items", 150)
        .AddColumn("Status", "Active")
    )
    .Build();
```

### 2. Render the Report

```csharp
using Lyo.Web.Reporting.Components;
using Lyo.Web.WebRenderer;

// Render to HTML file
var parameters = new Dictionary<string, object> { { "Report", report } };
await webRenderer.RenderToFileAsync<ReportViewer<string>>(
    "/path/to/output.html", 
    parameters);
```

## Core Concepts

### Report Structure

A report consists of:

- **Title & Subtitle** - Header information
- **Description** - Optional description text
- **Sections** - Main content containers
    - **Columns** - Key-value pairs displayed in a grid
    - **Grids** - Tables with rows and columns
    - **Content Blocks** - Text, HTML, lists, code, quotes, charts
    - **Subsections** - Nested sections for organization
- **Footer** - Optional footer text
- **Metadata** - Custom key-value pairs
- **Styles** - Custom CSS styles

### Report Parameters

Reports are generic and can accept typed parameters:

```csharp
public record SalesReportOptions(
    string Period,
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalRevenue
);

var report = ReportBuilder<SalesReportOptions>.New(options)
    .SetTitle("Sales Report")
    .AddSection(section => section
        .AddColumn("Period", options.Period)
        .AddColumn("Revenue", options.TotalRevenue.ToString("C"))
    )
    .Build();
```

## Building Reports

### Basic Report

```csharp
var report = ReportBuilder<MyOptions>.New()
    .SetTitle("Report Title")
    .SetSubtitle("Report Subtitle")
    .SetDescription("This is a description of the report.")
    .SetFooter("Generated on " + DateTime.Now.ToString("yyyy-MM-dd"))
    .Build();
```

### Report with Parameters

```csharp
var options = new MyOptions { Period = "Q1 2024" };
var report = ReportBuilder<MyOptions>.New(options)
    .SetTitle("Report Title")
    .Build();
```

### Adding Metadata

```csharp
var report = ReportBuilder<MyOptions>.New()
    .AddMetadata("ReportId", Guid.NewGuid())
    .AddMetadata("GeneratedAt", DateTime.Now)
    .AddMetadata("Version", "1.0")
    .Build();
```

### Custom Styling

```csharp
var report = ReportBuilder<MyOptions>.New()
    .AddStyle("font-family", "Arial, sans-serif")
    .AddStyle("color", "#333333")
    .Build();
```

## Sections

Sections are the main containers for report content. Each section can contain columns, grids, content blocks, and subsections.

### Simple Section

```csharp
.AddSection("Section Title", section => section
    .AddColumn("Label", "Value")
    .AddColumn("Status", "Active")
)
```

### Section with Subtitle and Description

```csharp
.AddSection(section => section
    .SetTitle("Section Title")
    .SetSubtitle("Section Subtitle")
    .SetDescription("Section description text")
    .AddColumn("Key", "Value")
)
```

### Collapsible Section

```csharp
.AddSection(section => section
    .SetTitle("Details")
    .SetCollapsed(true)  // Collapsed by default
    .AddText("This section is collapsed by default.")
)
```

### Nested Subsections

```csharp
.AddSection("Main Section", section => section
    .AddSubsection("Subsection 1", sub => sub
        .AddColumn("Item", "Value")
    )
    .AddSubsection("Subsection 2", sub => sub
        .AddText("Subsection content")
    )
)
```

## Columns

Columns display key-value pairs in a responsive grid layout.

### Basic Columns

```csharp
.AddSection(section => section
    .AddColumn("Name", "John Doe")
    .AddColumn("Email", "john@example.com")
    .AddColumn("Age", 30)
)
```

### Columns with Formatting

```csharp
.AddSection(section => section
    .AddColumn("Revenue", 125000.50m.ToString("C"), width: "200px", alignment: "right")
    .AddColumn("Count", 45.ToString(), alignment: "right")
)
```

### Columns from Dictionary

```csharp
var data = new Dictionary<string, object?>
{
    { "Total Sales", 125000.50m },
    { "Orders", 45 },
    { "Average Order", 2777.79m }
};

.AddSection(section => section
    .AddColumns(data)
)
```

### Advanced Column Builder

```csharp
.AddSection(section => section
    .AddColumn(column => column
        .SetLabel("Revenue")
        .SetValue(125000.50m)
        .SetWidth("200px")
        .SetAlignment("right")
        .SetEmphasized(true)
        .AddStyle("font-weight", "bold")
    )
)
```

## Grids (Tables)

Grids provide tabular data display with headers, formatting, and styling options.

### Basic Grid

```csharp
.AddSection(section => section
    .AddGrid("Sales Data", grid => grid
        .AddColumn("Product", "Product")
        .AddColumn("Quantity", "Quantity", alignment: "right")
        .AddColumn("Price", "Price", alignment: "right")
        .AddRow("Widget A", "100", "$1,000.00")
        .AddRow("Widget B", "50", "$500.00")
    )
)
```

### Grid with Headers and Formatting

```csharp
.AddSection(section => section
    .AddGrid(grid => grid
        .SetTitle("Product Sales")
        .SetCaption("Sales data for Q1 2024")
        .SetShowHeaders(true)
        .SetBordered(true)
        .SetStriped(true)
        .AddColumn("Product", "Product")
        .AddColumn("Sales", "Sales", alignment: "right", width: "150px")
        .AddRow("Product A", "$10,000")
        .AddRow("Product B", "$8,500")
    )
)
```

### Grid from Objects

```csharp
var products = new[]
{
    new { Name = "Product A", Price = 100.00m, Quantity = 10 },
    new { Name = "Product B", Price = 200.00m, Quantity = 5 }
};

.AddSection(section => section
    .AddGrid("Products", grid => grid
        .AddColumn("Name", "Name")
        .AddColumn("Price", "Price", alignment: "right")
        .AddColumn("Quantity", "Quantity", alignment: "right")
        .AddRowsFromObjects(products, p => new object?[]
        {
            p.Name,
            p.Price.ToString("C"),
            p.Quantity
        })
    )
)
```

### Grid with Custom Styling

```csharp
.AddSection(section => section
    .AddGrid(grid => grid
        .SetTitle("Custom Grid")
        .AddStyle("border", "2px solid #2563eb")
        .AddColumn("Column 1", "Field1")
        .AddColumn("Column 2", "Field2")
        .AddRow("Value 1", "Value 2")
    )
)
```

## Content Blocks

Content blocks provide various ways to display text and formatted content.

### Text Content

```csharp
.AddSection(section => section
    .AddText("This is a paragraph of text.")
    .AddText("This is another paragraph.")
)
```

### HTML Content

```csharp
.AddSection(section => section
    .AddHtml("<div><strong>Bold text</strong> and <em>italic text</em></div>")
)
```

### Lists

```csharp
// Bullet list
.AddSection(section => section
    .AddList(
        "Item 1",
        "Item 2",
        "Item 3"
    )
)

// Numbered list
.AddSection(section => section
    .AddNumberedList(
        "First item",
        "Second item",
        "Third item"
    )
)

// List from collection
var items = new[] { "Item A", "Item B", "Item C" };
.AddSection(section => section
    .AddList(items)
)
```

### Code Blocks

```csharp
.AddSection(section => section
    .AddContentBlock(ReportContentType.Code, 
        "public void Method()\n{\n    // Code here\n}")
)
```

### Quotes

```csharp
.AddSection(section => section
    .AddContentBlock(ReportContentType.Quote,
        "The only way to do great work is to love what you do.")
)
```

### Custom Content Blocks

```csharp
.AddSection(section => section
    .AddContentBlock(block => block
        .SetContentType(ReportContentType.Html)
        .SetContent("<div>Custom HTML content</div>")
        .AddStyle("padding", "20px")
    )
)
```

## Metrics Visualization

The library includes built-in support for visualizing metrics data using Chart.js.

### Prerequisites

Ensure you have `Lyo.Metrics` referenced and `IMetrics` registered in your DI container:

```csharp
services.AddLyoMetrics(); // Registers MetricsService as IMetrics
```

### Counter Metrics Chart

```csharp
using Lyo.Web.Reporting.Extensions;

var report = ReportBuilder<MyOptions>.New()
    .AddSection("Metrics", section => section
        .AddCounterChart(metrics, "Request Counters")
    )
    .Build();
```

### Filtered Counter Chart

```csharp
.AddSection(section => section
    .AddCounterChart(
        metrics,
        "API Metrics",
        counterNames: new[] { "api.requests.total", "api.requests.errors" }
    )
)
```

### Gauge Metrics Chart

```csharp
.AddSection(section => section
    .AddGaugeChart(metrics, "System Health")
)
```

### Histogram Metrics Chart

```csharp
.AddSection(section => section
    .AddHistogramChart(metrics, "Response Times")
)
```

### Complete Metrics Dashboard

```csharp
.AddSection(section => section
    .AddMetricsDashboard(metrics, "System Metrics")
)
```

### Filtering Options

#### Filter by Prefix

```csharp
.AddSection(section => section
    .AddCounterChartByPrefix(metrics, "api.", "API Metrics")
    .AddCounterChartByPrefix(metrics, "db.", "Database Metrics")
)
```

#### Filter by Tags

```csharp
var tags = new Dictionary<string, string> 
{ 
    { "environment", "production" } 
};

.AddSection(section => section
    .AddCounterChartByTags(metrics, tags, "Production Metrics")
)
```

#### Filter with Custom Predicate

```csharp
.AddSection(section => section
    .AddCounterChartFiltered(
        metrics,
        counter => counter.Value > 1000,
        "High-Value Counters"
    )
)
```

## Rendering Reports

### Using WebRendererService

The library integrates with `Lyo.Web.WebRenderer` for rendering reports to HTML files.

```csharp
using Lyo.Web.WebRenderer;
using Lyo.Web.Reporting.Components;

// Register WebRendererService
services.AddWebRendererService(options => {
    options.BrowserExePath = "/path/to/chrome";
});

// Render report
var report = ReportBuilder<MyOptions>.New()
    .SetTitle("My Report")
    .Build();

var parameters = new Dictionary<string, object> 
{ 
    { "Report", report } 
};

await webRenderer.RenderToFileAsync<ReportViewer<MyOptions>>(
    "/path/to/output.html",
    parameters
);
```

### Rendering in Blazor Applications

```razor
@page "/report"
@using Lyo.Web.Reporting.Components
@using Lyo.Web.Reporting.Models

<ReportViewer Report="@report" />

@code {
    private Report<MyOptions> report = ReportBuilder<MyOptions>.New()
        .SetTitle("My Report")
        .Build();
}
```

## Complete Examples

### Sales Report

```csharp
public record SalesReportOptions(
    string Period,
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalRevenue,
    int TotalOrders
);

var options = new SalesReportOptions(
    "Q4 2024",
    DateTime.Now.AddMonths(-3),
    DateTime.Now,
    125000.50m,
    45
);

var report = ReportBuilder<SalesReportOptions>.New(options)
    .SetTitle("Sales Report")
    .SetSubtitle("Quarterly Performance Summary")
    .SetDescription("This report provides an overview of sales performance.")
    .AddMetadata("ReportId", Guid.NewGuid())
    .AddMetadata("GeneratedAt", DateTime.Now)
    .AddSection("Summary", section => section
        .AddColumn("Period", options.Period)
        .AddColumn("Start Date", options.StartDate.ToString("MM/dd/yyyy"))
        .AddColumn("End Date", options.EndDate.ToString("MM/dd/yyyy"))
        .AddColumn("Total Revenue", options.TotalRevenue.ToString("C"), 
            width: "200px", alignment: "right")
        .AddColumn("Total Orders", options.TotalOrders.ToString(), 
            alignment: "right")
    )
    .AddSection("Revenue Breakdown", section => section
        .AddText("Revenue breakdown by product category:")
        .AddGrid("Revenue by Category", grid => grid
            .AddColumn("Category", "Category")
            .AddColumn("Revenue", "Revenue", alignment: "right")
            .AddColumn("Orders", "Orders", alignment: "right")
            .AddRow("Electronics", "$75,000.00", "25")
            .AddRow("Clothing", "$30,000.50", "15")
            .AddRow("Home & Garden", "$20,000.00", "5")
        )
    )
    .AddSection("Key Metrics", section => section
        .AddColumn("Average Order Value", 
            (options.TotalRevenue / options.TotalOrders).ToString("C"))
        .AddColumn("Revenue Growth", "+15.3%")
        .AddColumn("Customer Satisfaction", "4.8/5.0")
    )
    .SetFooter("Generated by Lyo Reporting System")
    .Build();
```

### Employee Performance Report

```csharp
public record EmployeePerformanceOptions(
    string EmployeeName,
    string Position,
    DateOnly StartDate,
    DateOnly EndDate
);

var options = new EmployeePerformanceOptions(
    "John Doe",
    "Software Engineer",
    new DateOnly(2024, 1, 1),
    new DateOnly(2024, 12, 31)
);

var projects = new[]
{
    new { Name = "Project Alpha", Score = 95, DurationWeeks = 8 },
    new { Name = "Project Beta", Score = 88, DurationWeeks = 12 },
    new { Name = "Project Gamma", Score = 92, DurationWeeks = 6 }
};

var report = ReportBuilder<EmployeePerformanceOptions>.New(options)
    .SetTitle("Employee Performance Report")
    .SetSubtitle($"Performance Review: {options.EmployeeName}")
    .AddSection("Employee Information", section => section
        .AddColumn("Name", options.EmployeeName)
        .AddColumn("Position", options.Position)
        .AddColumn("Review Period", 
            $"{options.StartDate:MM/dd/yyyy} - {options.EndDate:MM/dd/yyyy}")
    )
    .AddSection("Project Performance", section => section
        .AddText("Performance metrics for all projects:")
        .AddGrid("Projects", grid => grid
            .SetStriped(true)
            .SetBordered(true)
            .AddColumn("Project Name", "Name")
            .AddColumn("Score", "Score", alignment: "right")
            .AddColumn("Duration (weeks)", "DurationWeeks", alignment: "right")
            .AddRowsFromObjects(projects, p => new object?[]
            {
                p.Name,
                $"{p.Score}%",
                p.DurationWeeks
            })
        )
    )
    .AddSection("Achievements", section => section
        .AddSubsection("Technical Skills", sub => sub
            .AddText("Demonstrated strong technical capabilities:")
            .AddList(
                "Led implementation of microservices architecture",
                "Improved code quality metrics by 25%",
                "Mentored 3 junior developers"
            )
        )
        .AddSubsection("Collaboration", sub => sub
            .AddText("Excellent collaboration and communication:")
            .AddNumberedList(
                "Participated in 15+ code reviews",
                "Presented technical solutions to stakeholders",
                "Collaborated across 3 different teams"
            )
        )
    )
    .SetFooter("Confidential - For Internal Use Only")
    .Build();
```

### Metrics Dashboard Report

```csharp
using Lyo.Metrics;
using Lyo.Web.Reporting.Extensions;

var report = ReportBuilder<MetricsDashboardOptions>.New()
    .SetTitle("Metrics Dashboard")
    .SetSubtitle($"Environment: Production | Period: Last 7 Days")
    .AddSection(section => section
        .AddMetricsDashboard(metrics, "System Metrics Overview")
    )
    .AddSection("Selected Metrics", section => section
        .AddCounterChart(
            metrics,
            "API Request Counters",
            counterNames: new[] { "api.requests.total", "api.requests.errors" }
        )
        .AddGaugeChart(
            metrics,
            "System Resource Usage",
            gaugeNames: new[] { "cpu.usage", "memory.usage" }
        )
    )
    .SetFooter($"Generated on {DateTime.Now:MMMM dd, yyyy}")
    .Build();
```

## API Reference

### ReportBuilder<T>

Main builder for creating reports.

#### Methods

- `SetTitle(string title)` - Sets the report title
- `SetSubtitle(string subtitle)` - Sets the report subtitle
- `SetDescription(string description)` - Sets the report description
- `SetParameters(T parameters)` - Sets the report parameters
- `SetFooter(string footer)` - Sets the report footer
- `AddMetadata(string key, object? value)` - Adds metadata
- `AddMetadata(Dictionary<string, object?> metadata)` - Adds multiple metadata entries
- `AddStyle(string property, string value)` - Adds CSS style
- `AddSection(Action<SectionBuilder> configure)` - Adds a section
- `AddSection(string title, Action<SectionBuilder>? configure)` - Adds a section with title
- `Build()` - Builds and returns the report

#### Static Methods

- `ReportBuilder<T>.New()` - Creates a new builder
- `ReportBuilder<T>.New(T parameters)` - Creates a new builder with parameters

### SectionBuilder

Builder for creating report sections.

#### Methods

- `SetTitle(string title)` - Sets section title
- `SetSubtitle(string subtitle)` - Sets section subtitle
- `SetDescription(string description)` - Sets section description
- `SetCollapsed(bool collapsed)` - Sets whether section is collapsed
- `AddStyle(string property, string value)` - Adds CSS style
- `AddColumn(string label, object? value, string? width, string? alignment)` - Adds a column
- `AddColumn(Action<ColumnBuilder> configure)` - Adds a column using builder
- `AddColumns(Dictionary<string, object?> columns)` - Adds multiple columns
- `AddGrid(Action<GridBuilder> configure)` - Adds a grid
- `AddGrid(string title, Action<GridBuilder>? configure)` - Adds a grid with title
- `AddSubsection(Action<SectionBuilder> configure)` - Adds a subsection
- `AddSubsection(string title, Action<SectionBuilder>? configure)` - Adds a subsection with title
- `AddText(string text)` - Adds text content
- `AddHtml(string html)` - Adds HTML content
- `AddList(params string[] items)` - Adds a bullet list
- `AddNumberedList(params string[] items)` - Adds a numbered list
- `AddContentBlock(ReportContentType type, string content)` - Adds a content block
- `AddContentBlock(Action<ContentBlockBuilder> configure)` - Adds a content block using builder
- `Build()` - Builds and returns the section

### GridBuilder

Builder for creating tables/grids.

#### Methods

- `SetTitle(string title)` - Sets grid title
- `SetCaption(string caption)` - Sets grid caption
- `SetShowHeaders(bool showHeaders)` - Sets whether to show headers
- `SetShowRowNumbers(bool showRowNumbers)` - Sets whether to show row numbers
- `SetStriped(bool striped)` - Sets striped rows
- `SetBordered(bool bordered)` - Sets borders
- `AddStyle(string property, string value)` - Adds CSS style
- `AddColumn(string header, string? field, string? width, string? alignment)` - Adds a column
- `AddColumn(Action<GridColumnBuilder> configure)` - Adds a column using builder
- `AddRow(params object?[] cells)` - Adds a row
- `AddRowsFromObjects<T>(IEnumerable<T> items, Func<T, object?[]> selector)` - Adds rows from objects
- `Build()` - Builds and returns the grid

### Metrics Extensions

Extension methods for `SectionBuilder` to add metrics visualizations.

#### Methods

- `AddCounterChart(IMetrics metrics, string title, IEnumerable<string>? counterNames, int height)` - Adds counter bar chart
- `AddGaugeChart(IMetrics metrics, string title, IEnumerable<string>? gaugeNames, int height)` - Adds gauge doughnut chart
- `AddHistogramChart(IMetrics metrics, string title, IEnumerable<string>? histogramNames, int height)` - Adds histogram line chart
- `AddMetricsDashboard(IMetrics metrics, string title)` - Adds complete metrics dashboard
- `AddCounterChartByPrefix(IMetrics metrics, string prefix, string title, int height)` - Adds counter chart filtered by prefix
- `AddCounterChartByTags(IMetrics metrics, Dictionary<string, string> tags, string title, int height)` - Adds counter chart filtered by tags
- `AddCounterChartFiltered(IMetrics metrics, Func<CounterData, bool> predicate, string title, int height)` - Adds counter chart with custom filter

## Content Types

### ReportContentType Enum

- `Text` - Plain text paragraph
- `Html` - HTML markup
- `List` - Bullet list
- `NumberedList` - Numbered list
- `Code` - Code block with syntax highlighting
- `Quote` - Blockquote
- `Chart` - Chart visualization (for metrics)

## Styling

### Report-Level Styling

```csharp
var report = ReportBuilder<MyOptions>.New()
    .AddStyle("font-family", "Arial, sans-serif")
    .AddStyle("color", "#333333")
    .Build();
```

### Section-Level Styling

```csharp
.AddSection(section => section
    .AddStyle("background-color", "#f0f0f0")
    .AddStyle("padding", "20px")
)
```

### Element-Level Styling

```csharp
.AddColumn(column => column
    .AddStyle("font-weight", "bold")
    .AddStyle("color", "#2563eb")
)
```

## Best Practices

1. **Use Typed Parameters** - Leverage the generic type parameter for type-safe report options
2. **Organize with Sections** - Use sections to logically group related content
3. **Use Subsections** - Break down complex sections into subsections
4. **Leverage Builders** - Use builder methods for complex configurations
5. **Add Metadata** - Include metadata for tracking and debugging
6. **Consistent Styling** - Use consistent styling across reports
7. **Filter Metrics** - Use filtering options to show only relevant metrics




<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Web.Reporting.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `Microsoft.AspNetCore.Components.Web` | `[10,)` |

### Project references

- `Lyo.Metrics`

## Public API (generated)

Top-level `public` types in `*.cs` (*17*). Nested types and file-scoped namespaces may omit some entries.

- `ColumnBuilder`
- `ContentBlockBuilder`
- `GridBuilder`
- `GridColumnBuilder`
- `GridRowBuilder`
- `MetricsExtensions`
- `Report`
- `ReportBuilder`
- `ReportColumn`
- `ReportContentBlock`
- `ReportContentType`
- `ReportExamples`
- `ReportGrid`
- `ReportGridColumn`
- `ReportGridRow`
- `ReportSection`
- `SectionBuilder`

<!-- LYO_README_SYNC:END -->

## License

[Your License Here]
