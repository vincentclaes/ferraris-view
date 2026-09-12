#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")/.."
if [[ ! -f builds/web/index.html ]]; then
  echo "Run bash scripts/unity.sh web first." >&2
  exit 1
fi
exec "${VERCEL_CLI:-vercel}" deploy builds/web --project toenland --scope vincentclaes-projects --yes "$@"
