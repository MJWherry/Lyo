# Lyo.Html

Parses HTML with AngleSharp. Visible text skips script and style. Sanitize keeps a small set of elements and http, https, and mailto links.

## Features

- VisibleText omits script and style.
- Sanitize drops script, event handlers, and javascript URLs.

## Examples

### Visible text

```csharp
var text = new HtmlService().VisibleText("<p>Hello</p><script>secret</script>");
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `AngleSharp` `1.5.0` (direct, third-party)