# lyo-import-api-client

Import client: POST /Import and preview helpers.

Depends on `lyo-api-client`. Ships `py.typed`.

## Install

Install dependencies from this repo first (they are separate packages), then this one:

```bash
cd packages/python/lyo-import-api-client
pip install -e ../lyo-api-client
pip install -e ".[dev]"
```

## Tests

```bash
cd packages/python/lyo-import-api-client
pytest
```

Import the client as `lyo_import_api_client`.
