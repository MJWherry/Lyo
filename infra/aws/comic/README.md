# AWS comic infra (Terraform)

Two EC2 instances in a single public subnet (same shape as Court-Canary, no workers):

| Host | Role |
|------|------|
| **db** | Postgres 16. Nightly DLM EBS snapshots + `pg_dump` gzip to S3. Volume is retained if the instance is destroyed. `:5432` from the **app** SG only. |
| **app** | Docker Compose: `Lyo.Comic.Api` + Next.js comic UI on one box. Web talks to `http://api:5000` on the compose network. Comic files on the host volume. |

## Access (admin CIDRs)

`admin_cidrs` defaults to `24.3.30.20/32` (your IP). **Right now** 80/443/3101/5000 are all that list.

Later, set `web_cidrs = ["0.0.0.0/0"]` to open the UI. Keep `:5000` on `admin_cidrs` only. Put Caddy on 80/443: proxy `/` to Next, and `/auth/*` + `/.well-known/*` to the API so Google OIDC still works without publishing JSON to the world.

JSON/files stay BFF-only (`LYO_COMIC_API_BASE_URL=http://api:5000`). Browsers use `LYO_COMIC_PUBLIC_AUTH_URL` (today `http://<app-eip>:5000`) for the Google login 302.

## Prerequisites

- Terraform ≥ 1.5, AWS credentials with VPC/EC2/ECR/SSM/S3/DLM permissions
- Optional remote state: S3 + DynamoDB (uncomment backend in `environments/prod/main.tf`)

## Apply (local only)

Terraform is **not** run from GitHub Actions. Apply from your machine:

```bash
cd infra/aws/comic/environments/prod
cp terraform.tfvars.example terraform.tfvars   # admin_cidrs already set
# optional: export TF_VAR_postgres_password='…'  (else Terraform generates one into SSM)
terraform init
terraform plan
terraform apply
```

Outputs include `app_public_ip`, `app_instance_id`, `db_instance_id`, `ecr_api_url`, `ecr_web_url`, `backup_bucket`.

## Google OAuth

1. Create an OAuth client in Google Cloud; authorized redirect URI:
   `http://<app-eip>:5000/auth/callback/google` (or `https://<domain>/auth/callback/google` once Caddy fronts `/auth`).
2. Put `ClientId` / `ClientSecret` / `RedirectUri` in the **app** host `.env`.
3. Allowlist the web origin in `LyoOidcBff:AllowedReturnOrigins` (`http://<app-eip>:3101`).

## Images + deploy (GitHub Actions, manual)

Both workflows are **`workflow_dispatch` only** — nothing runs on push/PR.

1. **Docker - Build Comic** — choose `api` or `web`; builds and pushes
   `lyo-comic-api` / `lyo-comic-web` tagged
   `{run_id}.{run_number}.{run_attempt}`.
2. **Deploy - Comic** — choose `api` or `web` and paste that image tag.
   Both land on the **app** host (`Name=lyo-comic-app`) via SSM.

First-time host bootstrap (compose files):

```bash
deploy/comic/scripts/bootstrap-host.sh app <app-instance-id>
# db compose is written by user_data; optional re-sync:
# deploy/comic/scripts/bootstrap-host.sh db <db-instance-id>
```
