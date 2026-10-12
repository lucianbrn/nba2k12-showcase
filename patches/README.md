# Showcase Editor Patch

This overlay adds editor-side presentation, private asset import, and validation tools without rewriting the upstream simulation or neural-motion data.

1. Run `tools/Install-ShowcasePatch.ps1` from the root of this repository.
2. Open `NeuralCourt/Assets/Scenes/BasketballDemo.unity` in Unity 6.
3. Wait for compilation, then use the `Tools > NBA2K12 Showcase` menu.

- **0 - Apply NBA 2K Gameplay Camera Framing:** widens/elevates the existing player-follow camera starting pose and preserves its orbit/input path.
- **1 - Apply TV Presentation Polish:** configures URP post-processing, global grade, HDR/MSAA, shadow quality, and ambient light.
- **2 - Apply Lakers vs Celtics Palette:** assigns two team color profiles. These are not authentic uniforms.
- **3 - Audit Active Scene:** reports actual player, skinned-mesh, renderer, camera, and appearance-component counts.
- **5 - Import Local Model and Texture Exports:** imports supported local files into the Git-ignored `PrivateAssets` folder.
- **6 - Preview Selected Imported Model in Separate Scene:** lets you inspect scale/materials before rig integration.

See [docs/NBA2K12_ASSET_PIPELINE.md](../docs/NBA2K12_ASSET_PIPELINE.md) for the known PC-IFF vs PS3-SDAT boundary and [docs/HIGH_FIDELITY_ACCEPTANCE_GATE.md](../docs/HIGH_FIDELITY_ACCEPTANCE_GATE.md) for the completion bar.

The tools are development groundwork. They do not themselves provide authentic NBA 2K12 models or prove that an imported model animates in gameplay.
