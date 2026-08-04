# Portfolio deploy (Docker Compose)

CI (manual only): **Docker - Build Portfolio** pushes an ECR tag; **Deploy - Portfolio**
pulls that tag onto the api/web EC2 host. Terraform stays local
(`infra/aws/portfolio`).

API image: **`lyo-portfolio-api`** (`Lyo.Portfolio.Api`). Web: **`lyo-portfolio-web`**.

## API host (`api/`)

```bash
cd deploy/portfolio/api
cp .env.example .env   # set POSTGRES_PASSWORD; optional GoogleAuth__*
docker compose build
docker compose up -d
curl -fsS http://localhost:5251/health
```

Local file storage is mounted at `/var/lyo/portfolio-files` in the `api` service.

## Web host (`web/`)

```bash
cd deploy/portfolio/web
cp .env.example .env   # LYO_API_BASE_URL=http://<api-private-ip>:5251
docker compose build
docker compose up -d
# TLS (optional):
# docker compose --profile tls up -d
```

## Local laptop (both)

Run API compose, then web with `LYO_API_BASE_URL=http://host.docker.internal:5251` (Docker Desktop) or the API container's published port on the host network.
