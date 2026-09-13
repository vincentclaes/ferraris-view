#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")/.."
if [[ ! -f builds/web/index.html ]]; then
  echo "Run bash scripts/unity.sh web first." >&2
  exit 1
fi
# Exclude numbered sync copies of old generated pages and bundles.
printf '**/* [0-9]*\n' > builds/web/.vercelignore
exec "${VERCEL_CLI:-vercel}" deploy builds/web --project land-van-weleer --scope vincentclaes-projects --yes "$@"
