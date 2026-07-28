# lyo-api-client (Python)

Python client for the Lyo API. One pip-installable distribution with two importable packages,
mirroring the TypeScript packages in `packages/typescript/`:

- `lyo_api_client` — core, reusable API client foundation (no domain-specific endpoints):
  generic request/response types, bearer-token auth, normalized `ApiClientError`, and a
  pluggable transport with a zero-dependency stdlib default.
- `lyo_person_api_client` — typed Person Query API client built on top: request contracts,
  query builders, runtime response guards, and `PersonApiClient`.

No runtime dependencies. Ships `py.typed` so consumers get full type checking.

## Install

```bash
pip install ./packages/python/lyo-api-client
# or for development:
pip install -e "./packages/python/lyo-api-client[dev]"
```

## Usage

Works out of the box against a running API:

```python
from lyo_api_client import ApiClient
from lyo_person_api_client import PersonApiClient, baseline_query, is_query_res

api = ApiClient("http://localhost:5251", token="optional-token")
person_api = PersonApiClient(api)

res = person_api.query_person(baseline_query(start=0, amount=10))
if not is_query_res(res.data):
    raise RuntimeError("Unexpected response shape.")

for person in res.data["items"] or []:
    print(person["firstName"], person["lastName"])
```

Non-2xx responses raise `ApiClientError` with `status` and `details` (the parsed
problem-details payload when the API returned one):

```python
from lyo_api_client import ApiClientError

try:
    person_api.query_person(baseline_query())
except ApiClientError as err:
    print(err, err.status, err.details)
```

### Building custom queries

Request models keep snake_case attribute names and serialize to the exact wire shape the
API expects (PascalCase keys, `$type` discriminators):

```python
from lyo_person_api_client import (
    ConditionClause,
    GroupClause,
    QueryConcreteReq,
    SortBy,
    build_options,
)

request = QueryConcreteReq(
    options=build_options(),
    start=0,
    amount=50,
    where_clause=GroupClause("And", [
        ConditionClause("FirstName", "NotEquals", None),
        ConditionClause("IsActive", "Equals", True),
    ]),
    sort_by=[SortBy("LastName", "Asc", priority=0)],
)
res = person_api.query_person(request)
```

Optional fields left unset are omitted from the payload entirely; pass `None` explicitly to
send a JSON `null` (mirroring the undefined/null distinction in the TypeScript client).

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

## Tests

```bash
cd packages/python/lyo-api-client
pip install -e ".[dev]"
pytest
```
