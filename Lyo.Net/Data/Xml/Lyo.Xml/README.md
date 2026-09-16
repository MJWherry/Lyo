# Lyo.Xml

XmlService.Default writes child elements, omits nulls, and omits the XML declaration. new XmlService(o => { ... }) changes the root name, item name, namespace, attribute mode, naming, numbers, and encoding. Element text that is a canonical number is read as a number unless ReadNumbersFromStrings is false. One instance is safe for concurrent parse and export. Options are fixed after construction.

Byte methods take an optional Encoding. A missing argument uses the service encoding (UTF-8 without a BOM). A character span is already decoded. File and stream reads detect a BOM. Async twins exist on net10.0.

## Features

- XmlService.Default and new XmlService(configure) for camelCase, snake_case, kebab-case, or PascalCase, a custom root, and scalar attributes.
- `[XmlMember(AsAttribute = true)]` or `Member<T>(name, asAttribute: true)` writes one scalar as an attribute. `AsAttribute` false keeps that property an element when `ScalarsAsAttributes` is on.
- Order follows the shared document options: Defined, Alphabetical, or Specified through [MemberOrder] and OrderNames.
- `ExportFormattedToXmlString` writes formatter fields in dictionary order. `ExportColumnsToXmlString` writes a header-to-property map in that same order. `Order` does not reorder either list. A header that is not an XML name is encoded with `XmlConvert.EncodeLocalName`.
- Optional Encoding on ParseBytes, Parse(ReadOnlySpan<byte>), ExportToXmlBytes, and TryExport.
- OmitXmlDeclaration false writes a declaration whose encoding is the service encoding. A namespace is applied to the root and its children.
- ConvertToJson and ConvertFromJson for a file, stream, bytes, and a string. Cross-format conversion is two calls through JSON.
- Write omission of nulls, defaults, empty strings, and empty collections follows the shared document options. An omitted value is a missing element.

## Examples

### Round-trip through XML

```csharp
using Lyo.Xml;

string doc = XmlService.Default.ExportToXmlString(person);
string json = XmlService.Default.ConvertToJson(doc);
var customer = new XmlService(o => o.RootElementName = "customer");
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Json` (direct, lyo)
- `System.Text.Encoding.CodePages` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)