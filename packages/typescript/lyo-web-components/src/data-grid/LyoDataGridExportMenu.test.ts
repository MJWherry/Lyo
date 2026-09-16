import { describe, expect, it } from "vitest";
import { lyoDataGridExportFormats } from "./LyoDataGridExportMenu.js";

describe("lyoDataGridExportFormats", () => {
  it("offers every document format with the same column picker", () => {
    expect(lyoDataGridExportFormats.map((item) => item.format)).toEqual(["Csv", "Xlsx", "Json", "Yaml", "Xml", "Toml"]);
    expect(lyoDataGridExportFormats.map((item) => item.label)).toEqual([
      "Export CSV",
      "Export XLSX",
      "Export JSON",
      "Export YAML",
      "Export XML",
      "Export TOML",
    ]);
  });
});
