# NBA 2K12 Showcase Reconstruction

A Unity 6 basketball-game reconstruction project. It pins [NeuralCourt Basketball](https://github.com/henry753951/neuralcourt-basketball) as a submodule for the existing 5-on-5 simulation, synthetic athlete, court, ball possession, passing, shooting, and motion-inference foundation.

## Quality target

The end target is a convincing modern NBA 2K/NBA Live-style game—not a stylized prototype. **This project remains in development and does not meet that target yet.** Realistic player identity, fitted uniforms, skinning, animation, arena presentation, and stable 5-on-5 gameplay must pass the [High-Fidelity Acceptance Gate](docs/HIGH_FIDELITY_ACCEPTANCE_GATE.md) before completion can be claimed.

## Setup

Clone with the source submodule:

```powershell
git clone --recurse-submodules https://github.com/lucianbrn/nba2k12-showcase.git
```

If already cloned without submodules:

```powershell
git submodule update --init --recursive
```

Install the editor overlay:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\Install-ShowcasePatch.ps1
```

Open the `NeuralCourt` folder in Unity Hub using the upstream documented Unity version (`6000.5.3f1`). Open `Assets/Scenes/BasketballDemo.unity` and enter Play Mode.

## Unity editor tools

After the patch compiles, use `Tools > NBA2K12 Showcase`:
- **0** — apply a wider 2K-style gameplay-camera starting pose.
- **1** — apply restrained URP presentation grading and rendering-quality settings.
- **2** — test team-color profiles (not authentic uniforms).
- **3** — audit the active scene.
- **4** — configure a selected private model as a Humanoid.
- **5** — import local model/texture exports into a Git-ignored folder.
- **6** — preview an imported model in a separate scene.
- **7** — audit the selected model's Avatar, bones, renderers, and materials.
- **8** — replace the shared in-game player visual with a selected model. The original prefab is backed up under your local AppData folder; the change is local to the downloaded NeuralCourt submodule.

For distinct players, import separate FBX model assets and assign them to `ShowcasePlayerVisualVariant` by Player Index (P1-P10 = 0-9). Models need valid Humanoid Avatars and properly assigned materials/textures. A model being importable is not sufficient evidence that it deforms correctly during play.

## Authentic assets

The reference attachment was an RPCS3 shortcut, not game data. NBA 2K12 PC tools are documented for PC `.iff` containers; this does not establish support for the PS3 `.SDAT` format. See [NBA 2K12 Asset Pipeline](docs/NBA2K12_ASSET_PIPELINE.md) for the verified boundary and the extraction/import handoff.

No ripped NBA 2K12 models, face textures, uniforms, logos, commentary, or audio are included here. Local imports belong in `Assets/NBA2K12Showcase/PrivateAssets`, whose files are excluded from Git. Do not publish commercial-game assets without redistribution rights.

## Verification policy

Commits, compilation, file imports, and a successful launch are not proof of game-quality visuals. Every feature must be inspected in Unity Play Mode and meet its acceptance checks before it is called complete.
