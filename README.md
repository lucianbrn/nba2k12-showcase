# NBA 2K12 Showcase Reconstruction

A Unity 6 basketball-game reconstruction project. The codebase uses [NeuralCourt Basketball](https://github.com/henry753951/neuralcourt-basketball) as a pinned Git submodule for the existing 5-on-5 simulation, synthetic athlete, court, ball possession, passing, shooting, and motion-inference foundation.

## Quality target

The end target is a convincing modern NBA 2K/NBA Live-style game—not a stylized prototype. **This project is still in development and does not meet that target yet.** Player identity, fitted uniforms, skinning, animation, arena presentation, and stable 5-on-5 gameplay must pass the [High-Fidelity Acceptance Gate](docs/HIGH_FIDELITY_ACCEPTANCE_GATE.md) before completion can be claimed.

## Current foundation and limitations

The upstream Unity project is a research/engineering prototype. It provides a synthetic skinned athlete—not an NBA player likeness—plus court assets, team appearance profiles, basketball simulation, and motion inference. The patch adds editor tools for a wider 2K-style gameplay camera, URP presentation grading, color profiles, scene audit, and local model/texture import and preview.

Authentic NBA 2K12 player faces, official jerseys, logos, and original animation clips are not currently included or integrated.

## Setup

Clone including the pinned upstream project:

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

Open the `NeuralCourt` folder in Unity Hub using the Unity version specified in its README (`6000.5.3f1`). Open `Assets/Scenes/BasketballDemo.unity` and enter Play Mode.

## Unity tools added

After the patch compiles, use `Tools > NBA2K12 Showcase`:
- **0 - Apply NBA 2K Gameplay Camera Framing:** widens and elevates the player-follow camera starting pose.
- **1 - Apply TV Presentation Polish:** adds restrained URP grading and rendering quality settings.
- **2 - Apply Lakers vs Celtics Palette:** tests the existing jersey/short/number material path. It is a color test, not an official uniform.
- **3 - Audit Active Scene:** reports player, skinned-mesh, renderer, camera, and appearance-component counts.
- **5 - Import Local Model and Texture Exports:** imports supported local model and texture files into a Git-ignored folder.
- **6 - Preview Selected Imported Model in Separate Scene:** creates an isolated scene for checking scale/materials before rig integration.

See [NBA 2K12 Asset Pipeline](docs/NBA2K12_ASSET_PIPELINE.md) for the known PC-IFF vs PS3-SDAT boundary and local import workflow.

## Public repository policy

The repository contains no ripped NBA 2K12 models, faces, jersey textures, logos, commentary, or audio. Do not commit proprietary game assets without redistribution rights. Locally imported assets belong in `Assets/NBA2K12Showcase/PrivateAssets` and are excluded from Git.

## Verification policy

A successful commit, Unity compilation, asset import, or deployment status does not prove a game-quality result. Every change must be inspected in Unity Play Mode and pass its relevant checks in the [acceptance gate](docs/HIGH_FIDELITY_ACCEPTANCE_GATE.md).
