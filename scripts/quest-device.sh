#!/bin/bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
UNITY_EDITOR="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity}"
ADB="${ADB:-${UNITY_EDITOR%/Unity.app/Contents/MacOS/Unity}/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb}"
if [[ ! -x "$ADB" ]]; then
  echo "ADB not found. Install Unity Android Build Support or set ADB to its executable." >&2
  exit 2
fi
case "${1:-devices}" in
  devices) exec "$ADB" devices -l ;;
  install|run|logs) ;;
  *) echo "Usage: $0 devices|install|run|logs [device-serial]" >&2; exit 2 ;;
esac
ADB_COMMAND=("$ADB")
if [[ -n "${2:-}" ]]; then ADB_COMMAND+=(-s "$2"); fi
# ADB rejects absent, unauthorized or ambiguous devices before any install.
if [[ "$("${ADB_COMMAND[@]}" get-state)" != device ]]; then
  echo "Connect and unlock the Quest, enable developer mode, and accept its USB debugging prompt." >&2
  exit 3
fi
case "$1" in
  install)
    [[ -f "$ROOT/builds/Winksele1775.apk" ]] || { echo "Build the APK first: bash scripts/unity.sh quest" >&2; exit 2; }
    exec "${ADB_COMMAND[@]}" install -r "$ROOT/builds/Winksele1775.apk" ;;
  run) exec "${ADB_COMMAND[@]}" shell monkey -p be.ferrarisview.winksele -c android.intent.category.LAUNCHER 1 ;;
  logs) exec "${ADB_COMMAND[@]}" logcat -s Unity AndroidRuntime ;;
esac
