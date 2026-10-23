# lyo-config-cli

`lyo-config` talks to the Config HTTP API. It does not replace the C# `lyo` tool.

```bash
lyo-config get App gateway:local --base-url http://localhost:5251
lyo-config definitions --type App
lyo-config put-definition ./theme.json
lyo-config delete-definition <id>
lyo-config bindings --type App --id gateway:local
lyo-config set --type App --id gateway:local --key Theme --value light --encrypted
lyo-config delete-binding <id>
lyo-config revisions definition <id>
lyo-config revert binding <id> --revision 2
```

`--base-url` or `LYO_API_BASE_URL` is required. `--token` or `LYO_API_TOKEN` is optional. `--api-key` or `LYO_CONFIG_API_KEY` is sent as `X-Api-Key`.

`set` writes plaintext JSON. `--encrypted` turns the definition flag on. The value itself is not encrypted by this tool.
