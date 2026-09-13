#!/bin/bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
UNITY_EDITOR="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity}"
if [[ ! -x "$UNITY_EDITOR" ]]; then
  echo "Unity not found. Set UNITY_EDITOR to your Unity 6 editor executable." >&2
  exit 2
fi
mkdir -p "$ROOT/artifacts"
TARGET_ARGS=(-buildTarget StandaloneOSX)
case "${1:-desktop}" in
  configure) method=Ferraris.Editor.BuildProject.Configure ;;
  desktop) method=Ferraris.Editor.BuildProject.Desktop ;;
  web) method=Ferraris.Editor.BuildProject.Web; TARGET_ARGS=(-buildTarget WebGL) ;;
  quest-configure) method=Ferraris.Editor.BuildProject.ConfigureQuest; TARGET_ARGS=(-buildTarget Android) ;;
  quest) method=Ferraris.Editor.BuildProject.Quest; TARGET_ARGS=(-buildTarget Android) ;;
  tests)
    exec "$UNITY_EDITOR" -batchmode -projectPath "$ROOT/unity/FerrarisVR" -runTests -testPlatform EditMode -testResults "$ROOT/artifacts/unity-tests.xml" -logFile "$ROOT/artifacts/unity-tests.log" ;;
  open) exec "$UNITY_EDITOR" -projectPath "$ROOT/unity/FerrarisVR" ;;
  *) echo "Usage: $0 configure|desktop|web|quest-configure|quest|tests|open" >&2; exit 2 ;;
esac
"$UNITY_EDITOR" -batchmode -quit "${TARGET_ARGS[@]}" -projectPath "$ROOT/unity/FerrarisVR" -executeMethod "$method" -logFile "$ROOT/artifacts/unity-${1:-desktop}.log"

if [[ "${1:-desktop}" == quest ]]; then python3 "$ROOT/scripts/verify-quest.py"; fi
