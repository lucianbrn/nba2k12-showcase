#!/usr/bin/env bash
# NBA2K12 Showcase installer for Linux/macOS.
# Mirrors tools/Install-ShowcasePatch.ps1 (Windows) step for step:
#   1. Fetch the pinned NeuralCourt submodule (or clone the pinned commit for a ZIP extract).
#   2. Copy the showcase overlay into NeuralCourt/Assets/NBA2K12Showcase.
#   3. Apply the two idempotent gamepad source patches to the pinned upstream files (with backups).
#   4. Add glTFast 6.20.0 to Packages/manifest.json if absent (with a backup).
# Requires: git, python3. This script does not open Unity and does not validate Play Mode.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
UPSTREAM_ROOT="$REPO_ROOT/NeuralCourt"
SCENE="$UPSTREAM_ROOT/Assets/Scenes/BasketballDemo.unity"
SOURCE="$REPO_ROOT/patches/Assets/NBA2K12Showcase"
TARGET="$UPSTREAM_ROOT/Assets/NBA2K12Showcase"
MANIFEST="$UPSTREAM_ROOT/Packages/manifest.json"
UPSTREAM_URL="https://github.com/henry753951/neuralcourt-basketball.git"
UPSTREAM_COMMIT="84961525d7783df7b6e449391195924c91d315eb"
STAMP="$(date +%Y%m%d_%H%M%S)"

[ -f "$REPO_ROOT/.gitmodules" ] || { echo "Run this script from a clone or ZIP extract of https://github.com/lucianbrn/nba2k12-showcase." >&2; exit 1; }
command -v git >/dev/null || { echo "git is required." >&2; exit 1; }
command -v python3 >/dev/null || { echo "python3 is required." >&2; exit 1; }

if [ ! -f "$SCENE" ]; then
  if [ -d "$REPO_ROOT/.git" ]; then
    echo "Initializing the pinned NeuralCourt submodule..."
    git -C "$REPO_ROOT" submodule update --init --recursive
  else
    if [ -d "$UPSTREAM_ROOT" ] && [ -n "$(ls -A "$UPSTREAM_ROOT" 2>/dev/null)" ]; then
      echo "NeuralCourt exists but the expected scene is missing. Stopping to avoid deleting data: $UPSTREAM_ROOT" >&2; exit 1
    fi
    mkdir -p "$UPSTREAM_ROOT"
    echo "ZIP extract detected. Downloading the pinned Unity source project..."
    git -C "$UPSTREAM_ROOT" clone "$UPSTREAM_URL" .
    git -C "$UPSTREAM_ROOT" checkout "$UPSTREAM_COMMIT"
  fi
  [ -f "$SCENE" ] || { echo "The Unity demo scene is still missing after the submodule/clone step. No further changes were made." >&2; exit 1; }
fi

[ -d "$SOURCE" ] || { echo "Patch source is missing: $SOURCE" >&2; exit 1; }
[ -f "$MANIFEST" ] || { echo "Unity Packages/manifest.json was not found: $MANIFEST" >&2; exit 1; }

mkdir -p "$TARGET"
cp -R "$SOURCE"/. "$TARGET"/

# --- Upstream source patches (exact string replacements, idempotent, guarded) ---
python3 - "$UPSTREAM_ROOT" "$STAMP" <<'PYEOF'
import pathlib, sys
root = pathlib.Path(sys.argv[1]); stamp = sys.argv[2]

provider = root / "Assets/AI4AnimationRemake/Runtime/Control/BasketballKeyboardMouseInputProvider.cs"
if provider.exists():
    original = provider.read_text(encoding="utf-8")
    patched = original.replace(
        "Sprint = gamepad.leftStickButton.isPressed,",
        "Sprint = gamepad.rightTrigger.ReadValue() > 0.35f || gamepad.leftStickButton.isPressed,")
    patched = patched.replace(
        "Shoot = gamepad.buttonNorth.isPressed,",
        "Shoot = gamepad.buttonWest.isPressed || gamepad.buttonNorth.isPressed,")
    if patched != original:
        backup = provider.with_name(provider.name + ".nba2k12-backup." + stamp)
        backup.write_text(original, encoding="utf-8")
        provider.write_text(patched, encoding="utf-8")
        print(f"Applied USB gamepad sprint/shoot mappings. Backup: {backup}")
    else:
        print("USB gamepad sprint/shoot mappings are already applied.")
else:
    print(f"Warning: gamepad input provider not found at {provider}. Upstream mappings retained.")

match_path = root / "Assets/AI4AnimationRemake/Runtime/Control/BasketballMatchController.cs"
if not match_path.exists():
    sys.exit(f"Could not find match controller source: {match_path}")
original = match_path.read_text(encoding="utf-8").replace("\r\n", "\n")
nl = "\n"
patched = original.replace(
    "            bool primaryHeld = allowHumanInput && pointerAvailable && mouse != null &&" + nl +
    "                               mouse.leftButton.isPressed;",
    "            Gamepad gamepad = Gamepad.current;" + nl +
    "            bool primaryHeld = allowHumanInput && (" + nl +
    "                (pointerAvailable && mouse != null && mouse.leftButton.isPressed) ||" + nl +
    "                (gamepad != null && (gamepad.buttonWest.isPressed || gamepad.buttonNorth.isPressed)));")
patched = patched.replace(
    "            if (activeOwnsBall && keyboard != null &&" + nl +
    "                keyboard.spaceKey.wasPressedThisFrame && !passCommitted)",
    "            bool gamepadShootPressed = allowHumanInput && gamepad != null &&" + nl +
    "                (gamepad.buttonWest.wasPressedThisFrame || gamepad.buttonNorth.wasPressedThisFrame);" + nl +
    "            bool keyboardShootPressed = keyboard != null && keyboard.spaceKey.wasPressedThisFrame;" + nl +
    "            if (activeOwnsBall && (keyboardShootPressed || gamepadShootPressed) && !passCommitted)")
if patched != original:
    if "bool gamepadShootPressed = allowHumanInput" not in patched or "Gamepad gamepad = Gamepad.current;" not in patched:
        sys.exit("Could not safely apply the gamepad shot latch patch to BasketballMatchController.cs. No controller source was overwritten.")
    backup = match_path.with_name(match_path.name + ".nba2k12-backup." + stamp)
    backup.write_text(original, encoding="utf-8")
    match_path.write_text(patched, encoding="utf-8")
    print(f"Applied gamepad shot/steal routing to the central match controller. Backup: {backup}")
elif "bool gamepadShootPressed = allowHumanInput" in original and "Gamepad gamepad = Gamepad.current;" in original:
    print("Gamepad shot/steal routing is already applied.")
else:
    sys.exit("The pinned BasketballMatchController.cs did not match the expected source; no controller source was overwritten.")

manifest_path = root / "Packages/manifest.json"
import json
manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
deps = manifest.get("dependencies")
if deps is None:
    sys.exit("Packages/manifest.json has no dependencies object.")
if "com.unity.cloud.gltfast" not in deps:
    backup = manifest_path.with_name(manifest_path.name + ".backup." + stamp)
    backup.write_text(manifest_path.read_text(encoding="utf-8"), encoding="utf-8")
    deps["com.unity.cloud.gltfast"] = "6.20.0"
    manifest_path.write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    print(f"Added glTFast 6.20.0. Original manifest backed up to: {backup}")
elif str(deps["com.unity.cloud.gltfast"]) != "6.20.0":
    print(f"glTFast is already configured as {deps['com.unity.cloud.gltfast']}. The installer did not change your chosen version.")
else:
    print("glTFast 6.20.0 is already in the project manifest.")
PYEOF

PRIVATE_ROOT="$TARGET/PrivateAssets"
mkdir -p "$PRIVATE_ROOT"
if [ ! -f "$PRIVATE_ROOT/.gitignore" ]; then
  printf '# Local proprietary/third-party imports are never committed.\n*\n!.gitignore\n' > "$PRIVATE_ROOT/.gitignore"
fi

echo ""
echo "NBA2K12 Showcase editor patch installed."
echo "Unity project: $UPSTREAM_ROOT"
echo "Open the NeuralCourt folder in Unity Hub (Unity 6000.5.3f1), then open Assets/Scenes/BasketballDemo.unity."
echo "Use Tools > NBA2K12 Showcase > 16 - One-Click Core Setup, then 20 - Create Playable Matchup Menu, then press Play from NBA2K12Menu."
