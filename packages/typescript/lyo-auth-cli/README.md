# lyo-auth-cli

`lyo-auth` talks to the auth HTTP API. It does not replace the C# `lyo` tool.

```bash
lyo-auth me --base-url http://localhost:5251
lyo-auth tokens list
lyo-auth tokens create --name laptop --scopes auth.tokens.read
lyo-auth tokens revoke <id>
```

`--base-url` or `LYO_API_BASE_URL` is required. `--token` or `LYO_API_TOKEN` is optional.

`tokens create` prints the plaintext secret once. `tokens list` never includes it. Revoke keeps the token row.
