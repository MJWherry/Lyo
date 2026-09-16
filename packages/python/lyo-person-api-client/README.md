# lyo-person-api-client

Person query client, typed models, and query builders for Lyo APIs.

Depends on `lyo-api-client`. Ships `py.typed`.

## Install

```bash
cd packages/python/lyo-person-api-client
pip install -e ../lyo-api-client
pip install -e ".[dev]"
```

## Usage

```python
from lyo_api_client import ApiClient, ApiClientError
from lyo_person_api_client import PersonApiClient, baseline_query, is_query_res

api = ApiClient("http://localhost:5251", token="optional-token")
person_api = PersonApiClient(api)

try:
    res = person_api.query_person(baseline_query(start=0, amount=10))
except ApiClientError as err:
    print(err, err.status, err.details)
else:
    if not is_query_res(res.data):
        raise RuntimeError("Unexpected response shape.")
    for person in res.data["items"] or []:
        print(person["firstName"], person["lastName"])
```

Request models keep snake_case attribute names and serialize to the wire shape the API
expects (PascalCase keys, `$type` discriminators):

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

Optional fields left unset are omitted from the payload. Pass `None` explicitly to send JSON `null`.

## Tests

```bash
cd packages/python/lyo-person-api-client
pytest
```
