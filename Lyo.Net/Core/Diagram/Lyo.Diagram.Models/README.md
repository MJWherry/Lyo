# Lyo.Diagram.Models

The stored diagram model. A `DiagramDocument` has a name and one or more `DiagramPage` values. Each page holds vertex and edge `DiagramCell` values with draw.io geometry (bounds, relative connectors, waypoints, floating endpoints). Parent links express groups. Cell ids `0` and `1` stay reserved for the draw.io root and are not stored.

`IDiagramStore` loads and saves documents. `ListAsync` returns `DiagramSummary` (id, name, last saved) and leaves the cell payload out. The in-memory store lives in [`Lyo.Diagram`](../Lyo.Diagram/README.md). PostgreSQL lives in [`Lyo.Diagram.Postgres`](../Lyo.Diagram.Postgres/README.md).

## Features

- **Document.** `DiagramDocument`, `DiagramPage`, `DiagramCell`, `DiagramGeometry`, `DiagramPoint`.
- **Summary.** `DiagramSummary` is the list row: id, name, and last-saved time.
- **Store.** `IDiagramStore` get, list, upsert by id, and delete.

## Examples

### A page with a shape and a connector

```csharp
var diagram = new DiagramDocument {
    Id = LyoGuid.CreateV7(),
    Name = "Checkout",
    Pages = [
        new DiagramPage {
            Id = "flow",
            Name = "Flow",
            Cells = [
                new DiagramCell {
                    Id = "start",
                    Value = "Start",
                    IsVertex = true,
                    Geometry = new DiagramGeometry { X = 40, Y = 40, Width = 120, Height = 60 },
                },
            ],
        },
    ],
};
```