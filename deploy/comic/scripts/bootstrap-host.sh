#!/usr/bin/env bash
# One-time sync of compose files onto an EC2 host via SSM.
#
# Usage:
#   ./bootstrap-host.sh app <instance-id>
#   ./bootstrap-host.sh db <instance-id>
set -euo pipefail

ROLE="${1:?usage: $0 <app|db> <instance-id>}"
INSTANCE_ID="${2:?usage: $0 <app|db> <instance-id>}"
ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"
SRC="$ROOT/deploy/comic/$ROLE"

case "$ROLE" in
  app|db) ;;
  *)
    echo "Invalid role: $ROLE (expected app|db)" >&2
    exit 1
    ;;
esac

if [[ ! -d "$SRC" ]]; then
  echo "missing $SRC" >&2
  exit 1
fi

DEST="/opt/lyo/comic"
if [[ "$ROLE" == "app" ]]; then
  DEST="/opt/lyo/comic/app"
fi

TMP_TAR="$(mktemp -t lyo-comic-XXXXXX.tar.gz)"
tar -czf "$TMP_TAR" -C "$SRC" .
B64="$(base64 -w0 "$TMP_TAR")"
rm -f "$TMP_TAR"

aws ssm send-command \
  --instance-ids "$INSTANCE_ID" \
  --document-name "AWS-RunShellScript" \
  --parameters "commands=[
    \"set -euxo pipefail\",
    \"mkdir -p ${DEST}\",
    \"echo '${B64}' | base64 -d | tar -xzf - -C ${DEST}\",
    \"if [ '${ROLE}' = app ] && [ ! -f ${DEST}/.env ] && [ -f ${DEST}/.env.example ]; then cp ${DEST}/.env.example ${DEST}/.env; fi\",
    \"ls -la ${DEST}\"
  ]" \
  --output text

echo "Synced $ROLE compose files to $INSTANCE_ID:${DEST}"
