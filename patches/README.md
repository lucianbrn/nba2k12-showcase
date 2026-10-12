# Showcase Editor Patch

This overlay adds presentation, private model import, and rig integration tools without replacing the upstream simulation or neural-motion data.

1. Run `tools/Install-ShowcasePatch.ps1` from the root of this repository.
2. Open `NeuralCourt/Assets/Scenes/BasketballDemo.unity` in Unity 6.
3. Wait for compilation, then use the `Tools > NBA2K12 Showcase` menu.

- **0 - Apply NBA 2K Gameplay Camera Framing:** widens/elevates the existing player-follow camera.
- **1 - Apply TV Presentation Polish:** configures URP post-processing, global grade, HDR/MSAA and shadows.
- **2 - Apply Lakers vs Celtics Palette:** applies a color-only test; these are not authentic uniforms.
- **3 - Audit Active Scene:** reports player, skinned-mesh, renderer, camera and appearance-component counts.
- **4 - Configure Selected Model as Humanoid:** sets the selected private FBX's Unity rig type and generates an Avatar.
- **5 - Import Local Model and Texture Exports:** imports local FBX/OBJ/DAE and texture files into the Git-ignored `PrivateAssets` folder.
- **6 - Preview Selected Imported Model in Separate Scene:** checks a model in isolation.
- **7 - Audit Selected Player Rig:** checks the retargeter's required Humanoid bones and reports renderer/material names.
- **8 - Replace In-Game Player Visual with Selected Model (LOCAL):** backs up the existing visual prefab outside the Unity project, then swaps its fallback model for the selected imported Humanoid model. This modifies the local NeuralCourt submodule only.

After menu item 8, configure `ShowcasePlayerVisualVariant` on the source visual prefab to assign up to ten model prefabs by Player Index (P1-P10 = 0-9). This is how distinct faces/body meshes can be mapped to roster slots instead of cloning a single model. Each variant must have a valid Humanoid Avatar and matching texture/material assignments.

The tools are an integration path, not the assets themselves. Previewing a model or swapping one fallback does not prove that the project meets the visual acceptance gate. See [docs/NBA2K12_ASSET_PIPELINE.md](../docs/NBA2K12_ASSET_PIPELINE.md) and [docs/HIGH_FIDELITY_ACCEPTANCE_GATE.md](../docs/HIGH_FIDELITY_ACCEPTANCE_GATE.md).
