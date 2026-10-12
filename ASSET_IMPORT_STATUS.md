# Asset Import Status

Last updated: 2026-10-12

## Verified in the referenced Unity project

Source: https://github.com/henry753951/neuralcourt-basketball

- Unity 6 / URP demo scene: `Assets/Scenes/BasketballDemo.unity`
- Human athlete mesh: `Assets/AI4AnimationRemake/Characters/CrowdEyesTwin/Models/BSD_Athlete_Male_01.fbx`
- Appearance code supports skin variants, team jersey/short colors, and jersey-number display.
- Court, floor texture, markings, hoop materials, ball possession, passing, shooting, and player simulation systems are present.
- Third-party asset notices: `Assets/AI4AnimationRemake/Characters/CrowdEyesTwin/THIRD_PARTY_NOTICES.md`.

The upstream README specifies Unity `6000.5.3f1` and describes the project as a research/engineering prototype. The upstream repository has mixed third-party asset terms; check notices before redistribution.

## Added in this repository

- Wider/elevated gameplay camera starting-pose preset.
- URP TV-style post-processing and rendering-quality tool.
- Lakers/Celtics-inspired team color profile test.
- Scene audit menu.
- Local-only FBX/OBJ/DAE and texture import utility plus isolated model preview scene.

These are development tools and presentation groundwork. They do not establish NBA 2K12-level visual quality, and imported models are not automatically connected to the match's animation rig.

## Not imported or verified

- NBA 2K12 player models or face textures.
- Official NBA team logo/wordmark textures.
- Verified NBA 2K12 jersey UV maps, cloth meshes, or original motion clips.
- Commentary, soundtrack, or broadcast assets.
- A final game build verified in Unity Play Mode.

The earlier `NBA 2K12 Showcase Edition(1).lnk` attachment was a Windows shortcut to RPCS3, not the disc image or actual game files.

## Tool compatibility boundary

The researched NBA 2K12 PC jersey/cyber-face importer tools work on PC `.iff` files. That does not prove they support the PS3 `.SDAT` containers in the user's edition. A decrypted PS3 output is not a verified mesh until its payload is identified. See `docs/NBA2K12_ASSET_PIPELINE.md`.

## Public repository asset rule

Do not commit ripped game files, extracted NBA 2K12 archives, copyrighted logos, player face textures, commentary, or game audio to this public repository without redistribution rights. Imported assets are intended for the local-only `Assets/NBA2K12Showcase/PrivateAssets` folder.

## Next verified asset milestone

Export one player model with mesh, UVs, and skeleton/weights, together with matching textures. Import and preview locally, then verify bone mapping/retargeting, cloth and face material assignment, animation deformation, and gameplay at actual camera distance. Do not claim integration until that model works on an on-court player in Play Mode.
