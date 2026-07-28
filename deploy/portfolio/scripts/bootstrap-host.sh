#!/usr/bin/env bash
# One-time (or rare) sync of compose files onto an EC2 host via SSM Session Manager SCP-equivalent.
# Prefer: from a machine with AWS creds + SSM, or bake into AMI later.
#
# Usage:
#   ./bootstrap-host.sh api <instance-id>
#   ./bootstrap-host.sh web <instance-id>
set -euo pipefail

ROLE="${1:?usage: $0 <api|web> <instance-id>}"
INSTANCE_ID="${2:?usage: $0 <api|web> <instance-id>}"
ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"
SRC="$ROOT/deploy/portfolio/$ROLE"

if [[ ! -d "$SRC" ]]; then
  echo "missing $SRC" >&2
  exit 1
fi

TMP_TAR="$(mktemp -t lyo-portfolio-XXXXXX.tar.gz)"
tar -czf "$TMP_TAR" -C "$SRC" .
B64="$(base64 -w0 "$TMP_TAR")"
rm -f "$TMP_TAR"

aws ssm send-command \
  --instance-ids "$INSTANCE_ID" \
  --document-name "AWS-RunShellScript" \
  --parameters "commands=[
    \"set -euxo pipefail\",
    \"mkdir -p /opt/lyo/portfolio/${ROLE}\",
    \"echo '${B64}' | base64 -d | tar -xzf - -C /opt/lyo/portfolio/${ROLE}\",
    \"ls -la /opt/lyo/portfolio/${ROLE}\"
  ]" \
  --output text

echo "Synced $ROLE compose files to $INSTANCE_ID:/opt/lyo/portfolio/$ROLE"
