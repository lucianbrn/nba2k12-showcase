#!/usr/bin/env bash
# NBA2K12 Showcase batch-mode validator for Linux/macOS.
# Mirrors tools/Test-UnityProject.ps1: runs a Unity batch-mode import/compile
# of the NeuralCourt project and fails on C# compiler errors or fatal import
# failures in the log. A clean run is NOT a Play Mode or visual-quality test.
#
# Usage:
#   bash tools/Test-UnityProject.sh [UNITY_PATH] [PROJECT_PATH] [LOG_PATH] [TIMEOUT_MINUTES]
# Defaults: auto-detect Unity 6000.5.3f1 under Unity Hub install roots,
# project = <repo>/NeuralCourt, log = <repo>/unity-validation.log, timeout 25.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
UNITY_PATH="${1:-}"
PROJECT_PATH="${2:-$REPO_ROOT/NeuralCourt}"
LOG_PATH="${3:-$REPO_ROOT/unity-validation.log}"
TIMEOUT_MINUTES="${4:-25}"

find_unity() {
  if [ -n "$UNITY_PATH" ] && [ -x "$UNITY_PATH" ]; then echo "$UNITY_PATH"; return 0; fi
  local roots=(
    "$HOME/Unity/Hub/Editor"
    "/opt/unity/editors"
    "/Applications/Unity/Hub/Editor"
    "$HOME/.local/share/Unity/Hub/Editor"
  )
  local r
  for r in "${roots[@]}"; do
    [ -d "$r" ] || continue
    # Prefer the exact project version, then any installed editor.
    if [ -x "$r/6000.5.3f1/Editor/Unity" ]; then echo "$r/6000.5.3f1/Editor/Unity"; return 0; fi
    if [ -x "$r/6000.5.3f1/Unity.app/Contents/MacOS/Unity" ]; then echo "$r/6000.5.3f1/Unity.app/Contents/MacOS/Unity"; return 0; fi
    local any
    any="$(find "$r" -maxdepth 3 \( -name Unity -o -name Unity.app \) -print -quit 2>/dev/null || true)"
    if [ -n "$any" ]; then
      if [ -d "$any" ]; then echo "$any/Contents/MacOS/Unity"; else echo "$any"; fi
      return 0
    fi
  done
  return 1
}

[ -d "$PROJECT_PATH" ] || { echo "Unity project folder not found: $PROJECT_PATH. Run tools/Install-ShowcasePatch.sh first." >&2; exit 1; }
[ -f "$PROJECT_PATH/Assets/Scenes/BasketballDemo.unity" ] || { echo "Expected demo scene is missing from $PROJECT_PATH." >&2; exit 1; }

EDITOR="$(find_unity)" || { echo "Unity editor was not found. Install Unity Hub + Unity 6000.5.3f1, or pass the editor path as the first argument." >&2; exit 1; }

mkdir -p "$(dirname "$LOG_PATH")"
echo "Unity Editor: $EDITOR"
echo "Project:      $PROJECT_PATH"
echo "Log:          $LOG_PATH"
echo "Running batch-mode project import and C# compilation (timeout: ${TIMEOUT_MINUTES} minutes). This does not prove Play Mode gameplay or visual quality."

set +e
timeout "$((TIMEOUT_MINUTES * 60))" "$EDITOR" \
  -accept-apiupdate -batchmode -quit \
  -projectPath "$PROJECT_PATH" \
  -logFile "$LOG_PATH"
EXIT_CODE=$?
set -e

if [ "$EXIT_CODE" -eq 124 ]; then
  echo "Unity batch validation exceeded ${TIMEOUT_MINUTES} minutes. Inspect $LOG_PATH for an import/package stall." >&2
  exit 1
fi

LOG_TEXT=""
[ -f "$LOG_PATH" ] && LOG_TEXT="$(cat "$LOG_PATH")"

COMPILE_ERRORS="$(printf '%s\n' "$LOG_TEXT" | grep -E "error CS[0-9]+:" | sort -u || true)"
if [ -n "$COMPILE_ERRORS" ]; then
  echo "" >&2; echo "C# compile errors:" >&2
  printf '%s\n' "$COMPILE_ERRORS" >&2
  exit 1
fi

FATAL="$(printf '%s\n' "$LOG_TEXT" | grep -E "Script compilation failed|Failed to resolve packages|Aborting batchmode due to failure|Assembly-CSharp.*error" | sort -u || true)"
if [ -n "$FATAL" ]; then
  echo "" >&2; echo "Unity import/compilation failures:" >&2
  printf '%s\n' "$FATAL" >&2
  exit 1
fi

if [ "$EXIT_CODE" -ne 0 ]; then
  echo "Unity exited with code $EXIT_CODE. Review $LOG_PATH" >&2
  exit "$EXIT_CODE"
fi

echo ""
echo "Batch-mode import/compile completed without detected C# compiler or fatal import errors."
echo "Log saved to: $LOG_PATH"
echo "Next: open BasketballDemo.unity, run Tools > NBA2K12 Showcase > 16 - One-Click Core Setup, then test in Play Mode."
