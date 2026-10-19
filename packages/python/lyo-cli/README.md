# lyo-cli

`lyo` queries and changes a Lyo API. `lyo-mcp` exposes the same calls to a model over stdio.

Both read one profile. The HTTP calls go through `lyo-api-client` and `lyo-auth-api-client`. Listing and minting personal tokens stays on the TypeScript `lyo-auth` command.

## Install

```bash
cd packages/python/lyo-cli
pip install -e ../lyo-api-client
pip install -e ../lyo-auth-api-client
pip install -e ".[dev]"
```

The MCP server is an extra:

```bash
pip install -e ".[mcp]"
```

## Profile

Credentials resolve in this order: `--base-url` and `--token`, then `LYO_API_BASE_URL` and `LYO_API_TOKEN`, then `~/.config/lyo/profile.json`. The file mode is `0600`.

```json
{
  "base_url": "http://localhost:5251",
  "access_token": "jwt-or-lyo-token",
  "refresh_token": "refresh-token",
  "access_expires_at": "2026-10-07T18:00:00Z"
}
```

An explicit token replaces the file token and does not keep the file's refresh token. `lyo auth status` prints the user from `GET /auth/me`. It does not print the token.

A browser login stores an access token and a refresh token. When the access token is expired, the next command calls `POST /auth/refresh` once and writes the new pair back. A pasted `lyo_` PAT or CLI token is sent as-is. It does not refresh. Paste a new one with `lyo auth token` when it stops working.

## Auth

Browser login listens on `http://127.0.0.1:8734`. Add that origin to `LyoSso:AllowedReturnOrigins` on the API host. If port 8734 is taken, the command exits and does not pick another port.

```bash
lyo auth login --base-url http://localhost:5251 --provider google
```

Token paste needs no allow-list change:

```bash
lyo auth token --base-url http://localhost:5251
```

```bash
lyo auth status
lyo auth refresh
lyo auth logout
```

`refresh` rotates the refresh token from a browser login. `logout` sends that refresh token to `POST /auth/logout` and clears the stored credentials. The base URL stays in the file.

## Commands

`--route` is the entity path, such as `/api/Job/Definition` or `/dynamic/JobDefinition`. `--body` is a JSON file, or `-` for stdin. `--select` and `--amount` fill the query body only when that body omits `Select` or `Amount`. The default amount is 20.

```bash
lyo metadata --route /api/Job/Definition
lyo metadata --route /dynamic --entity JobDefinition
lyo query --route /api/Job/Definition --body query.json
lyo query --route /api/Job/Definition --select Id,Name --amount 20
lyo get --route /api/Job/Definition --id JOB_ID
lyo create --route /api/Job/Definition --body create.json
lyo patch --route /api/Job/Definition --body patch.json
lyo update --route /api/Job/Definition --id JOB_ID --body update.json
lyo delete --route /api/Job/Definition --id JOB_ID
```

`query.json`:

```json
{
  "Select": ["Id", "Name"],
  "Amount": 20,
  "whereClause": {
    "$type": "group",
    "Operator": "And",
    "Children": [
      {"$type": "condition", "Field": "Name", "Comparison": "Equals", "Value": "nightly"}
    ]
  }
}
```

## Library

Generic query and CRUD live on `ApiClient`. Handoff login lives on `AuthApiClient`.

```python
from lyo_api_client import ApiClient
from lyo_auth_api_client import AuthApiClient

api = ApiClient("http://localhost:5251", token="optional-token")
api.query_project("/api/Job/Definition", {"Select": ["Id", "Name"], "Amount": 20})
api.create("/api/Job/Definition", {"name": "nightly"})
api.update("/api/Job/Definition", "job-id", {"name": "nightly"})
api.delete_by_id("/api/Job/Definition", "job-id")

auth = AuthApiClient(api)
auth.exchange_handoff("lyoh_code", "http://127.0.0.1:8734")
auth.refresh({"refresh_token": "refresh-token"})
```

`exchange_handoff` posts `{"code": ...}` to `/auth/handoff/exchange` and sets `Origin`. The origin must match the allow-listed return origin, including the port.

## MCP

`lyo-mcp` speaks stdio and uses the same profile. It does not read a token from the MCP config.

Tools:

- `get_metadata(route, entity_type?)`
- `query_project(route, request)` — `Select` is required. `Amount` above 100 is rejected before the call.
- `get_entity(route, entity_id, include?)`

`query_project` accepts a `whereClause` like the query example above (`Equals` inside an `And` group).

`create_entity`, `patch_entity`, `update_entity`, and `delete_entity` are registered only when `LYO_MCP_WRITES=1`. They send the profile token, so the API's auth still applies. There is no bulk delete and no delete-by-query.

Cursor, in your MCP settings (do not commit this with a token):

```json
{
  "mcpServers": {
    "lyo": {
      "command": "lyo-mcp",
      "env": {
        "LYO_MCP_WRITES": "1"
      }
    }
  }
}
```

Leave `LYO_MCP_WRITES` unset, or set it to anything other than `1`, for a read-only server.

## Tests

```bash
cd packages/python/lyo-cli
pytest
```
