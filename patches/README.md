# Showcase Editor Patch

This overlay adds presentation, a live scoreboard, demo roster metadata, private model import and rig-integration tools without replacing the upstream simulation/neural motion data.

## Install

Run `tools/Install-ShowcasePatch.ps1` from the repository root. It copies the overlay into the pinned `NeuralCourt` submodule, backs up the Unity Package Manager manifest, and adds glTFast 6.20.0 if no version is already configured. This supports a full Git clone and a GitHub ZIP extract (with Git installed). Open `NeuralCourt/Assets/Scenes/BasketballDemo.unity` in Unity 6 and wait for package resolution/compilation.

## Unity menu

- **0 — Apply NBA 2K Gameplay Camera Framing:** wider/elevated player-follow camera.
- **1 — Apply TV Presentation Polish:** URP color grade, HDR/MSAA request, shadow quality and ambient light.
- **2 — Apply Lakers vs Celtics Palette:** colors only, not authentic uniforms.
- **3 — Audit Active Scene:** simple counts.
- **4 — Configure Selected Model as Humanoid:** sets FBX import rig type and tries to generate a Humanoid Avatar.
- **5 — Import Local Model and Texture Exports:** copies local FBX/OBJ/DAE/GLB/GLTF and texture files into ignored `PrivateAssets`.
- **6 — Preview Selected Imported Model in Separate Scene:** inspect scale/materials separately.
- **7 — Audit Selected Player Rig:** verify Avatar/bones and weighted skin mesh.
- **8 — Replace In-Game Player Visual with Selected Model (LOCAL):** validates a skinned Humanoid model, backs up the original visual prefab outside the project, then replaces the shared synthetic visual in the local submodule.
- **9 — Apply Broadcast HUD and 2011-12 Demo Roster:** adds the live scoreboard, 12-minute quarter clock, 24-second shot clock, shot-clock turnover, pause/resume, period-end flow, and historical demonstration names/numbers.
- **10 — Download CC0 Basketball Props Library:** downloads CC0 1.0 models from the current asset manifest.
- **11 — Download CC0 Indoor Sports Hall:** downloads a CC0 community-gym starter scene.
- **12 — Install CC0 Basketball Mesh on Live Ball:** replaces ball rendering only; keeps physics/authority.
- **13 — Create Separate CC0 Sports Hall Preview:** opens an additive local scene for the gym asset.
- **14 — Run Full Game Readiness Audit:** writes a structural report to `Assets/NBA2K12Showcase/Reports`.

Run items 9, 0, 1, 10, 12, and 14 for the first local setup. The CC0 props and sports centre are a legal content improvement and environment reference; they are not authentic NBA 2K12 assets and do not meet the pro-arena/real-athlete bar by themselves.

For private model integration, use items 4, 5, 6, 7, and 8 in that order. Separate visual models can be assigned on `ShowcasePlayerVisualVariant` by Player Index 0–9. Every variant must be a valid Humanoid Avatar with the required bones and weighted SkinnedMeshRenderer. Preview success does not prove live animation compatibility.

See [docs/NBA2K12_ASSET_PIPELINE.md](../docs/NBA2K12_ASSET_PIPELINE.md) and [docs/HIGH_FIDELITY_ACCEPTANCE_GATE.md](../docs/HIGH_FIDELITY_ACCEPTANCE_GATE.md).
