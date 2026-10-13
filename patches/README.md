# Showcase Editor Patch

This overlay adds presentation, a live scoreboard, demo roster metadata, local model-import and rig-integration tools without replacing the upstream simulation or neural-motion data. It is a development overlay, not a finished NBA 2K-level game.

## Install

Run `tools/Install-ShowcasePatch.ps1` from the repository root. It initializes the pinned `NeuralCourt` submodule when possible, backs up the Unity Package Manager manifest, copies the overlay, and adds glTFast 6.20.0 if no version is already configured. Open `NeuralCourt/Assets/Scenes/BasketballDemo.unity` in Unity 6 and wait for package resolution and compilation.

To run the local batch-mode import/compile check from the repository root:

```powershell
.\tools\Test-UnityProject.ps1
```

The script can accept `-UnityPath`, `-ProjectPath`, and `-LogPath`. A successful batch-mode compile is not a Play Mode test and does not validate visual fidelity or controller behavior.

## Unity menu

- **0 — Apply NBA 2K Gameplay Camera Framing:** adjusts the player-follow camera framing.
- **1 — Apply TV Presentation Polish:** adjusts URP presentation settings, shadows and ambient light.
- **2 — Apply Lakers vs Celtics Palette:** color palette only; not an authentic uniform or supported roster.
- **3 — Audit Active Scene:** basic scene/component counts.
- **4 — Configure Selected Model as Humanoid:** configures an FBX rig and attempts to create a Humanoid Avatar.
- **5 — Import Local Model and Texture Exports:** copies supported local model/texture files into ignored private assets.
- **6 — Preview Selected Imported Model in Separate Scene:** inspect scale and materials separately.
- **7 — Audit Selected Player Rig:** checks the Avatar, bones and weighted skin mesh.
- **8 — Replace In-Game Player Visual with Selected Model (LOCAL):** validates a skinned Humanoid model and replaces the local synthetic visual after backing it up.
- **9 — Apply Broadcast HUD and 2013 Heat vs 2008 Celtics Roster:** applies the live scoreboard and demo player identity metadata. Clock and match rules still require Play Mode testing.
- **10 — Download CC0 Basketball Props Library:** downloads CC0 assets from the checked-in manifest.
- **11 — Download CC0 Indoor Sports Hall:** downloads a CC0 community-gym starter environment.
- **12 — Install CC0 Basketball Mesh on Live Ball:** replaces the ball render mesh while retaining the existing physics/controller.
- **13 — Create Separate CC0 Sports Hall Preview:** creates an additive local preview scene.
- **14 — Run Full Game Readiness Audit:** writes a structural report under `Assets/NBA2K12Showcase/Reports`.
- **15 — Assign P01-P10 Player Models by Filename:** maps compatible local models to player indices 0–9.
- **16 — One-Click Core Setup:** runs the default roster/HUD, USB gamepad setup, matchup, camera and presentation configuration.
- **17 — Heat vs Celtics; 18 — Heat vs Warriors; 19 — Celtics vs Warriors:** select one of the three supported matchup presets.

For the first setup, open the demo scene and run **16 — One-Click Core Setup**, then **14 — Run Full Game Readiness Audit**. Run the game in Play Mode and test movement, dribbling, passing, shooting, score updates, possession changes, pause/restart and a wired Xbox or PlayStation controller.

The three supported teams are the **2013 Miami Heat**, **2008 Boston Celtics**, and **2017 Golden State Warriors**. Matchup presets configure roster identity/team selection; they do not create authentic NBA 2K player geometry, faces or uniforms.

## Private model integration

Place separate compatible exports named `P01_Name.fbx` through `P10_Name.fbx` in your local import folder. Use items 4, 6 and 7 to configure, preview and audit a model, then use item 15 to assign available models. Each player needs a valid Humanoid Avatar, suitable bones and a weighted `SkinnedMeshRenderer`. Preview success alone does not prove compatibility with live animation.

The CC0 props and sports hall are legal environment references, not authentic NBA 2K12 assets and not substitutes for detailed athletes or a broadcast-quality arena. Do not commit private or extracted commercial game assets to this public repository.

See [docs/CC0_ASSET_LIBRARY.md](../docs/CC0_ASSET_LIBRARY.md), [docs/NBA2K12_ASSET_PIPELINE.md](../docs/NBA2K12_ASSET_PIPELINE.md), and [docs/HIGH_FIDELITY_ACCEPTANCE_GATE.md](../docs/HIGH_FIDELITY_ACCEPTANCE_GATE.md) for asset sourcing, technical limitations and the completion criteria.
