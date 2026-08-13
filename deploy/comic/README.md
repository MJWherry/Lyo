# Comic deploy (Docker Compose)

CI (manual only): **Docker - Build Comic** pushes an ECR tag; **Deploy - Comic**
pulls that tag onto the **app** EC2 (UI + API together). Terraform stays local
(`infra/aws/comic`).

API image: **`lyo-comic-api`** (`Lyo.Comic.Api`). Web: **`lyo-comic-web`**.
Postgres is a **separate db host** (DLM snapshots + nightly `pg_dump` to S3).
No RabbitMQ. File bytes live on the app host at `/var/lyo/comic-files`.

Google OIDC: the browser must reach Comic API `/auth/*` (PKCE cookie is on the
API). Today that is `:5000` on the app EIP, allowlisted to `admin_cidrs`. Later,
Caddy on 80/443 can proxy `/auth` + `/.well-known` so `:5000` stays admin-only
when the UI is public.

## Local laptop (all-in-one)

```bash
cd deploy/comic
cp api/.env.example .env   # POSTGRES_PASSWORD, ComicFileEncryption__KeySecret, GoogleAuth__*, AUTH_COOKIE_SECRET
# add AUTH_COOKIE_SECRET and LYO_COMIC_PUBLIC_AUTH_URL=http://localhost:5000
docker compose up --build
# web http://localhost:3101  api http://localhost:5000/health
```

Postgres is not published (host 5437 is reserved). Web talks to `http://api:5000`
on the compose network. Browser Google 302s use `LYO_COMIC_PUBLIC_AUTH_URL`.

## Prod app host (`app/`)

UI + API on one box. `LYO_COMIC_API_BASE_URL=http://api:5000`. Postgres is the
db EC2 private IP (`POSTGRES_HOST` from SSM).

```bash
deploy/comic/scripts/bootstrap-host.sh app <app-instance-id>
# then Deploy - Comic for api and web image tags
```

TLS (optional): `docker compose --profile tls up -d` with `COMIC_DOMAIN` and
certs. Caddy already splits `/auth/login|callback|…` to the API and the rest
to Next.

## Prod db host (`db/`)

Written by Terraform user_data (Postgres 16, bind-mount data, nightly dump to S3).
Optional re-sync:

```bash
deploy/comic/scripts/bootstrap-host.sh db <db-instance-id>
```
