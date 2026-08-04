# AWS portfolio infra (Terraform)

Two EC2 instances in a single public subnet:

| Host | Role |
|------|------|
| **api** | Docker Compose: `Lyo.Portfolio.Api` + Postgres 16 + RabbitMQ |
| **web** | Docker Compose: Next.js portfolio (BFF) + optional Caddy TLS |

The web security group can reach API `:5251`. The browser never talks to the Portfolio API directly.

Seed people/config/job/reporting data yourself via the HTTP APIs after deploy (no Bogus seeder).

## Prerequisites

- Terraform ≥ 1.5, AWS credentials with VPC/EC2/ECR/SSM permissions
- Optional remote state: S3 + DynamoDB (uncomment backend in `environments/prod/main.tf`)

## Apply (local only)

Terraform is **not** run from GitHub Actions. Apply from your machine:

```bash
cd infra/aws/portfolio/environments/prod
cp terraform.tfvars.example terraform.tfvars   # edit admin_cidrs
export TF_VAR_postgres_password='…'
export TF_VAR_rabbitmq_password='…'
terraform init
terraform plan
terraform apply
```

If you previously had ECR `lyo-portfolio-testapi`, rename state before apply:

```bash
terraform state mv aws_ecr_repository.testapi aws_ecr_repository.api
# then apply — ECR name change still recreates the repo; re-push images after
```

Outputs include `lyo_api_base_url`, `ecr_api_url`, `ecr_web_url`, and instance IDs.

## Google OAuth (dashboard)

1. Create an OAuth client in Google Cloud; set authorized redirect URI to
   `https://<api-public-or-proxy>/auth/callback/google` (must match `GoogleAuth:RedirectUri`).
2. Put `ClientId` / `ClientSecret` / `RedirectUri` in the API host `.env` (or SSM → compose env).
3. Allowlist portfolio and Gateway origins in `LyoOidcBff:AllowedReturnOrigins`.

Domain redirects (e.g. mjwherry → lyo) stay at Cloudflare — not in this repo.

## Images + deploy (GitHub Actions, manual)

Both workflows are **`workflow_dispatch` only** — nothing runs on push/PR.

1. **Docker - Build Portfolio** — choose `api` or `web`; builds and pushes
   `lyo-portfolio-api` / `lyo-portfolio-web` tagged
   `{run_id}.{run_number}.{run_attempt}` (see the run title / step summary).
2. **Deploy - Portfolio** — choose `api` or `web` and paste that image tag.
   Deploys via SSM to the matching EC2 host (`deploy/portfolio/scripts/deploy-via-ssm.sh`).

First-time host bootstrap (compose files onto `/opt/lyo/portfolio/{api,web}`):

```bash
deploy/portfolio/scripts/bootstrap-host.sh api <instance-id>
deploy/portfolio/scripts/bootstrap-host.sh web <instance-id>
```
