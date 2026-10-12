# Asset Import Status

Last updated: 2026-10-12

## Verified in the referenced Unity project

Source: https://github.com/henry753951/neuralcourt-basketball

- Unity 6 / URP demo scene: `Assets/Scenes/BasketballDemo.unity`
- Human athlete mesh: `Assets/AI4AnimationRemake/Characters/CrowdEyesTwin/Models/BSD_Athlete_Male_01.fbx`
- Appearance code supports skin variants, team jersey/short colors, and jersey-number display.
- Court, floor texture, markings, hoop materials, ball possession, passing, shooting and player simulation systems are present.
- Third-party asset notices: `Assets/AI4AnimationRemake/Characters/CrowdEyesTwin/THIRD_PARTY_NOTICES.md`.

The upstream README specifies Unity `6000.5.3f1` and identifies itself as a research/engineering prototype. Its root tree does not include a root code LICENSE; verify usage and redistribution rights before copying upstream code outside the submodule.

## Not imported

- NBA 2K12 player models or face textures
- NBA player likenesses
- Official team logo/wordmark textures
- Verified NBA 2K12 jersey UV textures, cloth meshes, or original motion clips
- Commentary, soundtracks, or broadcast assets

The previously attached `NBA 2K12 Showcase Edition(1).lnk` was an RPCS3 Windows shortcut, not the disc image or the game assets.

## Public-repository asset rule

Do not commit ripped game files, extracted NBA 2K12 archives, copyrighted logos, player face textures, commentary, or game audio to this public repo without redistribution rights. Keep any authorized local imports private and note their source/license locally.

## Next verified asset milestone

Import a permission-cleared realistic player/uniform FBX or glTF and textures, then verify scale, rig, materials, number placement, cloth fit and live animation in Unity Play Mode. The current patch configures the upstream uniform-color system and URP post-processing; it does not create authentic licensed uniforms.
