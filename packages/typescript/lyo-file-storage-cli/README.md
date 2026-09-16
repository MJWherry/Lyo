# lyo-file-storage-cli

`lyo-files` talks to the FileStorage HTTP API. It does not replace the C# `lyo` tool.

```bash
lyo-files upload ./a.bin --prefix docs --encrypt --key-id k1 --base-url http://localhost:5251
lyo-files download <id> -o ./a.bin
lyo-files presign-get <id>
lyo-files presign-put ./a.bin
lyo-files presign-put --complete <fileId>
lyo-files ls --prefix docs
```

`--base-url` or `LYO_API_BASE_URL` is required. `--token` or `LYO_API_TOKEN` is optional. `--route-prefix` defaults to the `FileStorage` group inside the client.

`upload` and `download` go through the service. `download` always saves the API response. It does not fetch a provider URL. `presign-get` and `presign-put` print the provider URL and stop. Whether to use that URL is up to the caller. `link` creates an API access link, which decrypts and decompresses on download.
