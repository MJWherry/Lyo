#!/usr/bin/env bash
# Deploy a specific ECR image tag to the comic app EC2 (UI + API on one host).
#
# Usage:
#   AWS_REGION=us-east-2 NAME_PREFIX=lyo-comic \
#     ./deploy-via-ssm.sh <api|web> <image_tag>
set -euo pipefail

SERVICE="${1:?usage: $0 <api|web> <image_tag>}"
IMAGE_TAG="${2:?usage: $0 <api|web> <image_tag>}"
AWS_REGION="${AWS_REGION:-us-east-2}"
NAME_PREFIX="${NAME_PREFIX:-lyo-comic}"
SSM_PREFIX="/${NAME_PREFIX}/prod"

case "$SERVICE" in
  api|web) ;;
  *)
    echo "Invalid service: $SERVICE (expected api|web)" >&2
    exit 1
    ;;
esac

if [[ -z "$IMAGE_TAG" ]]; then
  echo "image_tag is required" >&2
  exit 1
fi

ACCOUNT="$(aws sts get-caller-identity --query Account --output text)"
ECR_REGISTRY="${ACCOUNT}.dkr.ecr.${AWS_REGION}.amazonaws.com"

if [[ "$SERVICE" == "api" ]]; then
  REPO="${NAME_PREFIX}-api"
  IMAGE_ENV="COMIC_API_IMAGE"
else
  REPO="${NAME_PREFIX}-web"
  IMAGE_ENV="COMIC_WEB_IMAGE"
fi

IMAGE="${ECR_REGISTRY}/${REPO}:${IMAGE_TAG}"
INSTANCE_TAG="${NAME_PREFIX}-app"

echo "Recent tags for ${REPO}:"
aws ecr describe-images \
  --repository-name "$REPO" \
  --region "$AWS_REGION" \
  --query 'sort_by(imageDetails,& imagePushedAt)[-20:].imageTags[]' \
  --output text 2>/dev/null | tr '\t' '\n' | sed '/^$/d' | tac || true

if ! aws ecr describe-images \
  --repository-name "$REPO" \
  --region "$AWS_REGION" \
  --image-ids "imageTag=${IMAGE_TAG}" \
  --query 'imageDetails[0].imageTags' \
  --output text >/dev/null; then
  echo "Tag '${IMAGE_TAG}' not found in ECR repo ${REPO}" >&2
  exit 1
fi
echo "Verified ${IMAGE}"

INSTANCE_ID="$(aws ec2 describe-instances \
  --region "$AWS_REGION" \
  --filters "Name=tag:Name,Values=${INSTANCE_TAG}" "Name=instance-state-name,Values=running" \
  --query 'Reservations[0].Instances[0].InstanceId' \
  --output text)"

if [[ -z "$INSTANCE_ID" || "$INSTANCE_ID" == "None" ]]; then
  echo "No running EC2 instance with Name=${INSTANCE_TAG}" >&2
  exit 1
fi
echo "Target instance: ${INSTANCE_ID}"

APP_PUBLIC_IP="$(aws ec2 describe-instances \
  --region "$AWS_REGION" \
  --instance-ids "$INSTANCE_ID" \
  --query 'Reservations[0].Instances[0].PublicIpAddress' \
  --output text)"

COMMANDS_JSON="$(
  {
    echo "set -euxo pipefail"
    echo "aws ecr get-login-password --region ${AWS_REGION} | docker login --username AWS --password-stdin ${ECR_REGISTRY}"
    echo "cd /opt/lyo/comic/app"
    echo "mkdir -p /opt/lyo/comic/app /var/lyo/comic-files"
    echo "fetch_param() { aws ssm get-parameter --region ${AWS_REGION} --name ${SSM_PREFIX}/\$1 --with-decryption --query Parameter.Value --output text; }"
    echo "upsert() { local k=\"\$1\" v=\"\$2\"; grep -q \"^\${k}=\" .env 2>/dev/null && sed -i \"s|^\${k}=.*|\${k}=\${v}|\" .env || echo \"\${k}=\${v}\" >> .env; }"
    echo "touch .env"
    echo "export ${IMAGE_ENV}=${IMAGE}"
    echo "upsert ${IMAGE_ENV} ${IMAGE}"
    echo "upsert POSTGRES_HOST \"\$(fetch_param postgres_host)\""
    echo "upsert POSTGRES_DB \"\$(fetch_param postgres_db)\""
    echo "upsert POSTGRES_USER \"\$(fetch_param postgres_user)\""
    echo "upsert POSTGRES_PASSWORD \"\$(fetch_param postgres_password)\""
    echo "upsert LYO_COMIC_API_BASE_URL http://api:5000"
    echo "upsert LYO_COMIC_PUBLIC_AUTH_URL http://${APP_PUBLIC_IP}:5000"
    echo "docker pull ${IMAGE}"
    echo "docker compose up -d --remove-orphans"
    echo "docker compose ps"
  } | jq -R . | jq -s '{commands: .}'
)"

COMMAND_ID="$(aws ssm send-command \
  --region "$AWS_REGION" \
  --instance-ids "$INSTANCE_ID" \
  --document-name AWS-RunShellScript \
  --parameters "$COMMANDS_JSON" \
  --query 'Command.CommandId' \
  --output text)"

echo "SSM command ${COMMAND_ID} on ${INSTANCE_ID}"
echo "Waiting..."

if ! aws ssm wait command-executed \
  --region "$AWS_REGION" \
  --command-id "$COMMAND_ID" \
  --instance-id "$INSTANCE_ID"; then
  echo "SSM command failed or timed out" >&2
  aws ssm get-command-invocation \
    --region "$AWS_REGION" \
    --command-id "$COMMAND_ID" \
    --instance-id "$INSTANCE_ID" \
    --query '{Status:Status,StatusDetails:StatusDetails,Stdout:StandardOutputContent,Stderr:StandardErrorContent}' \
    --output json >&2 || true
  exit 1
fi

STATUS="$(aws ssm get-command-invocation \
  --region "$AWS_REGION" \
  --command-id "$COMMAND_ID" \
  --instance-id "$INSTANCE_ID" \
  --query 'Status' \
  --output text)"

if [[ "$STATUS" != "Success" ]]; then
  echo "SSM status=${STATUS}" >&2
  aws ssm get-command-invocation \
    --region "$AWS_REGION" \
    --command-id "$COMMAND_ID" \
    --instance-id "$INSTANCE_ID" \
    --query '{Status:Status,Stdout:StandardOutputContent,Stderr:StandardErrorContent}' \
    --output json >&2 || true
  exit 1
fi

echo "Deployed ${SERVICE} → ${IMAGE} on ${INSTANCE_ID}"

if [[ -n "${GITHUB_STEP_SUMMARY:-}" ]]; then
  {
    echo "## Deploy"
    echo ""
    echo "| Field | Value |"
    echo "|-------|-------|"
    echo "| Service | \`${SERVICE}\` |"
    echo "| Image | \`${IMAGE}\` |"
    echo "| Instance | \`${INSTANCE_ID}\` |"
    echo "| SSM | \`${COMMAND_ID}\` |"
  } >> "$GITHUB_STEP_SUMMARY"
fi
