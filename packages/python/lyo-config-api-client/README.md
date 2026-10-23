# lyo-config-api-client

Config client: resolve, including 304, and definition/binding management.

Depends on `lyo-api-client`. Ships `py.typed`.

## Install

Install dependencies from this repo first (they are separate packages), then this one:

```bash
cd packages/python/lyo-config-api-client
pip install -e ../lyo-api-client
pip install -e ".[dev]"
```

## Tests

```bash
cd packages/python/lyo-config-api-client
pytest
```

Import the client as `lyo_config_api_client`.
