# Asset Import Status

Last updated: 2026-10-12

## Verified in the referenced Unity project

Source: https://github.com/henry753951/neuralcourt-basketball

- Unity 6 / URP demo scene: `Assets/Scenes/BasketballDemo.unity`
- Synthetic human athlete mesh: `Assets/AI4AnimationRemake/Characters/CrowdEyesTwin/Models/BSD_Athlete_Male_01.fbx`
- Appearance code for skin variants, jersey/short colors, and jersey-number display.
- Humanoid retargeter that drives an Animator Avatar from the canonical 26-bone basketball rig.
- Court, hoops, passing, shooting, ball possession, and 5-on-5 match systems.

## Added in the showcase repository

- Wider/elevated gameplay camera preset and URP TV-grade post-processing.
- Live broadcast HUD tied to the real score tracker and possession manager.
- 12-minute quarter clock, 24-second shot clock, shot-clock turnover, offensive-rebound 14-second reset, pause, period-end and overtime/final flow.
- 2011-12 Lakers/Celtics demonstration roster names, positions and numbers.
- Local-only FBX/OBJ/DAE/GLB/GLTF and texture import plus isolated preview.
- Selected-model Humanoid configuration, bone/skinning audit and backed-up local visual-patch tool.
- Per-player visual variants across Player Index 0–9.
- CC0 asset library downloader for basketball props and a separate indoor community-sports-hall reference scene.
- CC0 basketball mesh installer that preserves existing ball physics.
- Full readiness audit reports.

These scripts are committed but **have not been compiled or executed in the user's Unity editor in this environment**. The HUD/clock logic and import calls require actual Play Mode validation.

## Not imported or verified

- NBA 2K12 player models or face textures.
- Official NBA jersey UVs/logos/wordmarks or authentic player-name textures.
- NBA 2K12 original motion clips.
- Commentary/audio/broadcast package.
- Final 2K-quality arena or skin/uniform detail.
- A stable final build verified in Unity Play Mode.

The earlier `NBA 2K12 Showcase Edition(1).lnk` attachment was an RPCS3 shortcut, not the disc image or game data.

## Third-party assets

- CC0 basketball geometry and a community indoor sports hall can be downloaded through `Tools > NBA2K12 Showcase` after glTFast resolves. The downloader checks the current pack manifest's CC0 1.0 label/license before downloading and saves the manifest/license record beside the local models. These are static props/settings, not NBA players.
- NeuralCourt's athlete has its own third-party notice file. Follow its terms before redistributing the upstream assets.

## Tool compatibility boundary

NBA 2K12 PC jersey/cyber-face importers are documented for PC `.iff` files. This does not prove they support PS3 `.SDAT` containers. A decrypted SDAT output is not a validated mesh until its payload is identified. See `docs/NBA2K12_ASSET_PIPELINE.md`.

## Next verified asset milestone

Export a real player model with mesh, UVs and skeleton/weights plus matching face/body/uniform textures. Import, audit and preview it, then integrate it into the player visual and verify movement, shooting, passing and landing deformation at gameplay camera distance. Do not claim integration until it works on an on-court player in Play Mode.
