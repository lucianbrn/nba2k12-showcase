# Showcase Editor Patch

This overlay adds editor-side presentation and validation tools without rewriting the upstream simulation or neural-motion data.

1. Run `tools/Install-ShowcasePatch.ps1` from the root of this repository.
2. Open `NeuralCourt/Assets/Scenes/BasketballDemo.unity` in Unity 6.
3. Wait for compilation, then use the `Tools > NBA2K12 Showcase` menu.

- **1 - Apply TV Presentation Polish:** configures URP post-processing, global grade, HDR/MSAA, shadow quality and ambient light.
- **2 - Apply Lakers vs Celtics Palette:** creates team appearance profiles and assigns them to team IDs 0 and 1 using the existing appearance API. Saves the active demo scene.
- **3 - Audit Active Scene:** reports actual counts for team members, skinned meshes, renderers, cameras and appearance components.

The palette is a color-only starting point; this overlay adds no official logos, player likenesses or NBA 2K12 files.
