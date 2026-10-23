# lyo-auth-api-client

Auth client: session, personal tokens, and admin Auth resources.

Depends on `lyo-api-client`. Ships `py.typed`.

## Install

Install dependencies from this repo first (they are separate packages), then this one:

```bash
cd packages/python/lyo-auth-api-client
pip install -e ../lyo-api-client
pip install -e ".[dev]"
```

## Session

`me` is `GET /auth/me`. `exchange_handoff(code, origin)` posts `{"code": ...}` to `/auth/handoff/exchange` and sets the `Origin` header. `origin` must match the allow-listed return origin, including the port. `refresh({"refresh_token": ...})` rotates a browser session. `logout({"refresh_token": ...})` revokes that refresh token. A pasted `lyo_` PAT or CLI token does not use refresh or logout.

## Tests

```bash
cd packages/python/lyo-auth-api-client
pytest
```

Import the client as `lyo_auth_api_client`.
