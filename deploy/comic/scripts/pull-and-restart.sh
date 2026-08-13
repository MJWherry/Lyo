#!/usr/bin/env bash
# Pull latest images and restart a compose stack on an EC2 host.
# Usage: pull-and-restart.sh <app|db> [compose-dir]
set -euo pipefail

ROLE="${1:?usage: $0 <app|db> [compose-dir]}"
COMPOSE_DIR="${2:-.}"

cd "$COMPOSE_DIR"

if [[ -f .env ]]; then
  set -a
  # shellcheck disable=SC1091
  source .env
  set +a
fi

docker compose pull || true
docker compose up -d --remove-orphans
docker compose ps
