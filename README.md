# NBA 2K12 Showcase Reconstruction

A Unity 6 basketball-game reconstruction project. It uses [NeuralCourt Basketball](https://github.com/henry753951/neuralcourt-basketball) as a pinned submodule for the 5-on-5 simulation, synthetic athlete, court, passing, shooting, ball possession, and motion-inference foundation.

## Non-negotiable quality target

The target is a convincing modern NBA 2K/NBA Live-style game, not a stylized prototype. **The current project is in development and does not yet meet that target.** Full-size realistic players, fitted uniforms, skinning, believable basketball motion, arena presentation, and stable 5-on-5 play must pass the [High-Fidelity Acceptance Gate](docs/HIGH_FIDELITY_ACCEPTANCE_GATE.md). Code existing or compiling is not proof of visual quality.

## Setup

Clone with the pinned source submodule:

```powershell
git clone --recurse-submodules https://github.com/lucianbrn/nba2k12-showcase.git
```

If you downloaded the repository ZIP, Git is required for the installer to download the missing `NeuralCourt` submodule at the pinned revision. From the extracted repository root, run:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\Install-ShowcasePatch.ps1
```

The installer copies the Unity overlay, backs up `Packages/manifest.json` before adding glTFast 6.20.0, and supports both a Git clone and a ZIP extract. After it finishes, open the `NeuralCourt` folder in Unity Hub with the upstream documented Unity version (`6000.5.3f1`). Open `Assets/Scenes/BasketballDemo.unity` and wait for Unity to finish package resolution and compilation.

## Unity tools

Use `Tools > NBA2K12 Showcase` after scripts compile:

- **0 — Apply NBA 2K Gameplay Camera Framing:** wider/elevated player-follow camera.
- **1 — Apply TV Presentation Polish:** URP post-processing, HDR/MSAA request, shadows, and ambient grade.
- **2 — Apply Lakers vs Celtics Palette:** a color-only test; not an authentic uniform.
- **3 — Audit Active Scene:** basic scene counts.
- **4 — Configure Selected Model as Humanoid:** prepare a selected private FBX for Avatar setup.
- **5 — Import Local Model and Texture Exports:** import local FBX/OBJ/DAE/GLB/GLTF and textures into a Git-ignored folder.
- **6 — Preview Selected Imported Model in Separate Scene:** isolate a model for visual checks.
- **7 — Audit Selected Player Rig:** checks the Humanoid Avatar, required bones, and skinned mesh.
- **8 — Replace In-Game Player Visual with Selected Model (LOCAL):** validates the selected model, backs up the upstream visual prefab to `%LOCALAPPDATA%\NBA2K12Showcase\Backups`, and modifies the local NeuralCourt submodule. Do not rerun the upstream scene builder afterward unless you intend to regenerate the synthetic demo.
- **9 — Apply Broadcast HUD and 2011-12 Demo Roster:** adds the live scoreboard, 12-minute quarter clock, 24-second shot clock, pause/end-of-period flow, and demo player names/numbers.
- **10 — Download CC0 Basketball Props Library:** downloads the asset provider's currently listed CC0 1.0 basketball court/hoop/ball/furniture GLBs plus the manifest and license record.
- **11 — Download CC0 Indoor Sports Hall:** downloads a CC0 community sports-centre starter scene.
- **12 — Install CC0 Basketball Mesh on Live Ball:** replaces the ball's renderer with the downloaded 240 mm GLB mesh while retaining the original physics root.
- **13 — Create Separate CC0 Sports Hall Preview:** creates a separate additive preview scene, not the main basketball arena.
- **14 — Run Full Game Readiness Audit:** writes a local report with player counts, valid Humanoid Avatar counts, weighted meshes, ball/court references, camera, and blockers.

### First Unity run

1. Run menu item **9** to set the roster and attach the live HUD.
2. Run **0** and **1** for gameplay framing and URP presentation.
3. Run **10**, wait for the downloads and Unity's GLB import, then run **12** to install the CC0 basketball mesh.
4. Run **14** and review the generated readiness report.
5. Enter Play Mode and test movement, passing, shooting, scoring, shot-clock violations, quarter transitions, and pause/resume.

Menu items 10–13 download/use CC0 **static setting and prop geometry**. The sports centre is a community gym reference, not a pro NBA arena. It has no basketball logic, colliders, or animated players.

## Real players and uniforms

The existing athlete is a synthetic, CC0/attributed-source model, not an NBA likeness. The demo roster provides names and jersey numbers for Lakers/Celtics-era presentation, but it does not extract the actual NBA 2K12 roster. Team palette colors are not real uniforms.

The PS3 reference asset provided earlier was only an RPCS3 shortcut. NBA 2K12 PC mod tools are documented for PC `.iff` containers; that is not proof they support the PS3 `.SDAT` files. See [docs/NBA2K12_ASSET_PIPELINE.md](docs/NBA2K12_ASSET_PIPELINE.md). A real player is not considered integrated until the mesh, UV textures, weighted skeleton, Humanoid Avatar, and on-court animation all work together in Play Mode.

No ripped NBA 2K12 player models, face textures, jersey maps, official logos, commentary, or audio are bundled in this public repository. Local imports belong in `Assets/NBA2K12Showcase/PrivateAssets` and are excluded from Git. Do not publish commercial-game assets without redistribution rights.

## Verification

Run **Tools > NBA2K12 Showcase > 14 - Run Full Game Readiness Audit** after setup. It reports structure and known blockers only; it cannot judge visual quality. Every player, uniform, animation, camera shot, and arena setting still needs inspection in Unity Play Mode. Until the non-negotiable acceptance gate is passed, the project remains **in development**.
