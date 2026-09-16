# lyo-import-cli

`lyo-import` posts a file to `POST /Import` and prints the returned rows and issues. It does not save them.

```bash
lyo-import ./rows.csv --format Csv --base-url http://localhost:5251
lyo-import ./rows.json --format Json --json-mode Array
lyo-import ./book.xlsx --format Xlsx --sheet Sheet1
```

`--base-url` or `LYO_API_BASE_URL` is required. `--token` or `LYO_API_TOKEN` is optional. Formats: Json, Csv, Xlsx, Yaml, Xml, Toml.
