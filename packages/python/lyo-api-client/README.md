# lyo-api-client (Python)

Core Python client for Lyo APIs. This package is the transport other domain clients share.
It has no domain endpoints.

Domain clients are sibling directories, one package each, same as `packages/typescript/`:

- `lyo-person-api-client`
- `lyo-job-api-client`
- `lyo-reporting-api-client`
- `lyo-automation-api-client`
- `lyo-import-api-client`
- `lyo-auth-api-client`
- `lyo-config-api-client`
- `lyo-recipe-api-client`
- `lyo-file-storage-api-client`

`lyo` and `lyo-mcp` are documented in `packages/python/lyo-cli`.

No runtime dependencies. Ships `py.typed`.

## Install

```bash
pip install -e "./packages/python/lyo-api-client[dev]"
```

Install this package before a domain client. Each domain package depends on `lyo-api-client`.

## Usage

```python
from lyo_api_client import ApiClient, ApiClientError, ApiRequest

api = ApiClient("http://localhost:5251", token="optional-token")
try:
    response = api.request(ApiRequest(method="GET", path="/health"))
except ApiClientError as err:
    print(err, err.status, err.details)
```

Non-2xx responses raise `ApiClientError` with `status` and `details` (the parsed
problem-details payload when the API returned one).

### Custom transport

The default transport uses `urllib.request` (stdlib). To use httpx, requests, or anything
else, pass a callable taking a `TransportRequest` and returning an `ApiResponse`:

```python
import httpx
from lyo_api_client import ApiClient, ApiResponse, TransportRequest

def httpx_transport(request: TransportRequest) -> ApiResponse:
    res = httpx.request(request.method, request.url, headers=request.headers, content=request.body)
    data = None
    try:
        data = res.json()
    except ValueError:
        pass
    return ApiResponse(status=res.status_code, ok=res.is_success, headers=dict(res.headers), data=data, raw_body=res.text)

api = ApiClient("http://localhost:5251", transport=httpx_transport)
```

Transports must not raise on non-2xx statuses; report them via `ApiResponse.ok` and the
client will normalize the error.

Bytes bodies, such as multipart uploads from `multipart_form`, are sent as-is. A caller-supplied
`Content-Type` replaces the default `application/json`.

## Tests

```bash
cd packages/python/lyo-api-client
pip install -e ".[dev]"
pytest
```
