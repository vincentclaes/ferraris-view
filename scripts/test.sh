#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")/.."
.venv/bin/python -m pytest -q
dotnet run --project tests/Coordinates -- data/winksele/area.json
