# Asset Import Status

Last updated: 2026-10-12

## Verified in the referenced Unity project

Source: https://github.com/henry753951/neuralcourt-basketball

- Unity 6 / URP demo scene: `Assets/Scenes/BasketballDemo.unity`
- Human athlete mesh: `Assets/AI4AnimationRemake/Characters/CrowdEyesTwin/Models/BSD_Athlete_Male_01.fbx`
- The appearance system supports skin variants, team jersey/short colors, and number meshes.
- The retargeter accepts a valid Unity Humanoid Avatar and drives it from the canonical 26-bone basketball rig.
- Court, hoop, passing, shooting, possession, and match simulation systems are present.

## Added in this repository

- Wider/elevated gameplay camera preset and URP presentation settings.
- Team color palette test and active-scene audit.
- Local-only FBX/OBJ/DAE and texture import plus isolated preview scene.
- Selected-model Humanoid setup and bone/renderer audit.
- Local player-visual replacement tool with a backup outside the Unity project.
- Per-player visual variant component, assignable by Player Index, for ten separate rigged model prefabs.

These are tools that support integration; they do not supply real NBA 2K12 assets or prove the resulting game is high fidelity.

## Not imported or verified

- NBA 2K12 player models or face textures.
- Official NBA team logo/wordmark textures.
- Verified NBA 2K12 jersey UV maps, cloth meshes, or original motion clips.
- Commentary, soundtrack, broadcast assets.
- A final game build visually verified in Unity Play Mode.

The earlier `NBA 2K12 Showcase Edition(1).lnk` attachment was an RPCS3 shortcut, not the disc image or actual game data.

## Tool compatibility boundary

NBA 2K12 PC jersey/cyber-face importers are documented for PC `.iff` files. This does not prove they support PS3 `.SDAT` containers. A decrypted SDAT output is not a validated mesh until its payload has been identified. See `docs/NBA2K12_ASSET_PIPELINE.md`.

## Public repository asset rule

Do not commit ripped game files, extracted NBA 2K12 archives, copyrighted logos, player face textures, commentary, or game audio without redistribution rights. Imported assets are intended for the local-only `Assets/NBA2K12Showcase/PrivateAssets` folder.

## Next verified asset milestone

Export one skinned player model plus matching body/face/jersey/shorts textures. Import it locally, audit its Humanoid rig, preview, then integrate it into the shared visual prefab. Then repeat for distinct roster slots and team uniforms. Do not claim integration until each model visibly animates on an on-court player in Play Mode.
