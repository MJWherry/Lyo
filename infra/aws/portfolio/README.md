# AWS portfolio infra (Terraform)

Two EC2 instances in a single public subnet:

| Host | Role |
|------|------|
| **api** | Docker Compose: `Lyo.TestApi` + Postgres 16 + RabbitMQ |
| **web** | Docker Compose: Next.js portfolio (BFF) + optional Caddy TLS |

The web security group can reach API `:5251`. The browser never talks to TestApi.

## Prerequisites

- Terraform ≥ 1.5, AWS credentials with VPC/EC2/ECR/SSM permissions
- Optional remote state: S3 + DynamoDB (uncomment backend in `environments/prod/main.tf`)

## Apply

```bash
cd infra/aws/portfolio/environments/prod
cp terraform.tfvars.example terraform.tfvars   # edit admin_cidrs
export TF_VAR_postgres_password='…'
export TF_VAR_rabbitmq_password='…'
terraform init
terraform plan
terraform apply
```

Outputs include `lyo_api_base_url`, ECR repo URLs, and instance IDs for GitHub Actions deploy via SSM.

## Post-apply deploy

1. CI pushes images to ECR (`lyo-portfolio-testapi`, `lyo-portfolio-web`).
2. On **api** EC2: copy `deploy/portfolio/api` compose + `.env` (Postgres password from SSM), `docker compose up -d`.
3. On **web** EC2: set `LYO_API_BASE_URL` to the `lyo_api_base_url` output, `docker compose up -d`.

User-data installs Docker; first deploy is driven by GitHub Actions deploy jobs (SSM Run Command) or manual SSH/SSM.
