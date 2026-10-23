# Lyo.Diagram

Turns a [`DiagramDocument`](../Lyo.Diagram.Models/README.md) into a diagrams.net file and back. `DiagramBuilder` adds rectangle, rounded, ellipse, and diamond shapes plus an
orthogonal connector. Any other draw.io style string can be passed through.

`DiagramDrawIo.Export` writes an uncompressed `<mxfile>` that diagrams.net opens directly. `DiagramDrawIo.Import` reads that file and the compressed form diagrams.net saves by
default (base64 of raw DEFLATE of `encodeURIComponent` applied to the `mxGraphModel` XML). Import assigns a new document id. The name comes from the `mxfile` `name` attribute,
otherwise the first page name.

`InMemoryDiagramStore` implements `IDiagramStore` for hosts that do not open PostgreSQL. Persist with [`Lyo.Diagram.Postgres`](../Lyo.Diagram.Postgres/README.md).

## Features

- **Builder.** `DiagramBuilder` / `DiagramPageBuilder` with `DiagramShapeKind` presets, a raw style overload, `Size` for the page, and `Connect` waypoints plus anchors.
- **draw.io.** Uncompressed export and import of uncompressed or compressed mxfiles, including pages, labels, styles, geometry, waypoints, parents, and edge endpoints.
- **Memory store.** `InMemoryDiagramStore` upserts by id and lists summaries without requiring the caller to read cells.

## Examples

### Build a page and export it

```csharp
var diagram = new DiagramBuilder("Checkout")
    .Page("flow", "Flow", page =>
    {
        page.Shape("start", "Start", 40, 40, 120, 60, DiagramShapeKind.Rectangle);
        page.Shape("end", "End", 40, 180, 120, 60, DiagramShapeKind.Ellipse);
        page.Connect("go", "start", "end", "next");
    })
    .Build();

var xml = DiagramDrawIo.Export(diagram);
var loaded = DiagramDrawIo.Import(xml);
```

### In-memory store

```csharp
IDiagramStore store = new InMemoryDiagramStore();
await store.SaveAsync(diagram, ct);
var summary = (await store.ListAsync(ct)).Single(item => item.Id == diagram.Id);
```

## What round-trips

Pages, labels, style strings, bounds, relative edge geometry, waypoints, source and target points, parent links, and edge endpoints. Export always emits the draw.io root cells
(`id="0"` and `id="1"`). Import drops them. Editor viewport (`dx` / `dy`) and embedded image binaries are left out.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Diagram.Models` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)