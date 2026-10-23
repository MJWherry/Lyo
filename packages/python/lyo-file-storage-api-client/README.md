# lyo-file-storage-api-client

FileStorage client: upload, download, presign, and multipart routes.

Depends on `lyo-api-client`. Ships `py.typed`.

## Install

Install dependencies from this repo first (they are separate packages), then this one:

```bash
cd packages/python/lyo-file-storage-api-client
pip install -e ../lyo-api-client
pip install -e ".[dev]"
```

## Tests

```bash
cd packages/python/lyo-file-storage-api-client
pytest
```

Import the client as `lyo_file_storage_api_client`.

```python
from lyo_api_client import ApiClient
from lyo_file_storage_api_client import FileStorageApiClient

files = FileStorageApiClient(ApiClient("http://localhost:5251"))
files.stores()

archive = FileStorageApiClient(ApiClient("http://localhost:5251"), store_route="FileStorage/archive")
archive.health()
```

`stores()` requests `GET FileStorage/stores`. `store_route` replaces the `FileStorage` base. The metadata query stays `FileStorage/FileMetadata`.
