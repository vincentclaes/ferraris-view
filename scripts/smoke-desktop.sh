#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")/.."
mkdir -p artifacts
PLAYER_EXECUTABLE=$(/usr/libexec/PlistBuddy -c 'Print CFBundleExecutable' builds/Winksele1775.app/Contents/Info.plist)
exec "builds/Winksele1775.app/Contents/MacOS/$PLAYER_EXECUTABLE" -ferraris-smoke -evidence-dir "$PWD/artifacts" -logFile "$PWD/artifacts/player-smoke.log" -screen-fullscreen 0 -screen-width 1440 -screen-height 1000 "$@"
