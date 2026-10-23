# lyo-automation-api-client

Automation run client: start, poll, cancel, log, and snapshots.

Depends on `lyo-api-client`. Ships `py.typed`.

## Install

Install dependencies from this repo first (they are separate packages), then this one:

```bash
cd packages/python/lyo-automation-api-client
pip install -e ../lyo-api-client
pip install -e ".[dev]"
```

## Tests

```bash
cd packages/python/lyo-automation-api-client
pytest
```

Import the client as `lyo_automation_api_client`.
