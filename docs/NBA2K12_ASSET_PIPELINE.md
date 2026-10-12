# NBA 2K12 Asset Import Pipeline — Evidence-First

## Why the project needs real assets

The Unity foundation has an animated synthetic athlete and a team-color appearance system. It does **not** contain NBA 2K12 player likenesses, official jerseys, or the original player mesh/UV data. Changing jersey colors alone does not meet the visual target.

## Verified tool boundary

The file previously attached in this conversation was a Windows `.lnk` to RPCS3, not the game's data.

The game edition under investigation is the PS3 release, whose files include large `.SDAT` resources. Decrypting an SDAT output is not proof that it contains an importable mesh or texture archive.

NBA 2K12 modding tools exist for the **PC release**:
- [NBA 2K12 Jerseys Textures Importer/Exporter](https://www.moddingway.com/file/15617/nba-2k12-jerseys-textures-importer-exporter) documents exporting jersey textures from PC `.iff` files such as `uh015.iff`.
- [NBA 2K12 Cyber Faces Textures Importer/Exporter](https://www.moddingway.com/file/15546/nba-2k12-cyber-faces-textures-importer-exporter) documents exporting face textures from PC `.iff` files such as `png0195.iff`.
- The [NBA-Live.com NBA 2K12 tools list](https://www.nba-live.com/forums/viewtopic.php?mobile=on&t=84048) lists the NBA2KX Mod Tool as supporting textures and 3D models in NBA 2K12 PC IFFs.

Those pages describe the PC IFF workflow. They do **not** establish that the tools can unpack PS3 SDAT containers, or that PS3 payloads can be dragged straight into Unity. Do not run a PC IFF tool against an opaque SDAT output and assume extraction succeeded.

This particular game ID, `BLUS-30944`, is listed as the PS3 NBA 2K12: Game of the Year Edition ([SerialStation title record](https://serialstation.com/titles/BLUS/30944)). The [NLSC RED Modding Center listing](https://www.nba-live.com/forums/downloads.php?df_id=8756&mobile=on&view=detail) describes IFF import/export for NBA 2K12–2K14 **PC** assets. A [historical PS3/360 modding discussion](https://forums.operationsports.com/forums/forum/basketball/nba-2k-basketball/nba-2k-last-gen/nba-2k-last-gen-rosters/494647-is-it-possible-to-mod-2k12-on-ps3-or-360) reports that the PC visual-mod workflow does not transfer directly to console disc content. This is evidence of a platform boundary, not proof that offline extraction is impossible; however, I have not found a verified PS3 mesh-export workflow for this project.

**Practical consequence:** a real NBA 2K-style player cannot be produced from the PS3 shortcut, a roster file, or a jersey texture alone. We need a verified model export with mesh/UV/skeleton plus matching face/body/uniform textures. If the PS3 container stage cannot produce that, a legal PC NBA 2K12 IFF export workflow (if you have access to one) or a separately licensed realistic player asset is the viable source for the Unity import path.

## NBA2K-Dataset source supplied for this project

Repository: https://github.com/luyangzhu/NBA2K-dataset

This is a research dataset captured from NBA 2K19, not a ready-to-import asset pack for the 2013 Heat, 2008 Celtics, or 2017 Warriors. Its README says the released download contains data for 28 synthetic players because current NBA player data cannot be redistributed. The full images/pose/mesh archives must be requested through the project's form; the repository says processing the raw archives requires more than 100 GB of disk space.

The data includes OBJ triangle meshes, UV texture maps, pose/bone transform arrays, and mesh weights for linear blend skinning. Those are useful research inputs, but the repository does not provide Unity-ready skinned FBX prefabs or finished animation clips. An import pipeline would still need to reconstruct/retarget the skeleton, preserve weights and UVs, generate usable motion, build uniforms/materials, and validate the result in Unity.

**License constraint:** CC BY-NC-SA 4.0 (University of Washington), which allows non-commercial use with attribution and share-alike requirements. Do not use it for a commercial release without separate permission. Do not describe these synthetic players as authentic LeBron James, Dwyane Wade, Kevin Garnett, or Stephen Curry assets; this dataset does not provide those current-player meshes in the public release.

This source is the best research lead for higher-detail clothed mesh and pose data, but it does not by itself complete the requested team-specific roster, realistic uniform fitting, or animation system. No dataset archives have been downloaded or integrated by this repository update.

## Required format handoff

1. Keep the original disc/ISO and all source archives untouched.
2. Identify and verify a tool that unpacks the PS3 SDAT/container layer into named payload files. Record exact input, output, tool/version, and exit status. Do not proceed until at least one output has recognizable structure as a real archive or asset.
3. If extracted payloads include 2K12 IFFs, verify that a tool supports those *specific extracted IFFs*. PC compatibility is not assumed for PS3 data.
4. Export a player model with mesh, UVs, and skeleton/weights where available, and the matching face, body, jersey, and shorts texture maps. A face texture alone is not a 3D likeness; a jersey UV map only works with compatible geometry/UVs.
5. If a tool exports an unskinned OBJ, treat it as a static preview until a valid skeleton and weights are added. If the source preserves weights, use a documented Blender-to-FBX/Humanoid retarget path and verify all bone mappings.
6. In Unity, choose **Tools > NBA2K12 Showcase > 5 - Import Local Model and Texture Exports**. This imports supported FBX/OBJ/DAE and texture files into the local-only `Assets/NBA2K12Showcase/PrivateAssets` folder.
7. Select the imported FBX asset. Use **Tools > NBA2K12 Showcase > 4 - Configure Selected Model as Humanoid**, then **7 - Audit Selected Player Rig**. The model needs a valid Avatar and the full set of bones the retargeter expects, including toes, feet, hands, spine/chest, neck and head.
8. Use **Tools > NBA2K12 Showcase > 6 - Preview Selected Imported Model in Separate Scene** to inspect scale, UV/material assignment, renderer names and proportions.
9. After the audit passes, use **Tools > NBA2K12 Showcase > 8 - Replace In-Game Player Visual with Selected Model (LOCAL)**. It backs up the source visual prefab under `%LOCALAPPDATA%\\NBA2K12Showcase\\Backups` before swapping the shared fallback model. This modifies the local NeuralCourt submodule; the private game files and modified upstream prefab are not committed to the public repository.
10. To avoid ten copies of one athlete, assign separate imported model prefabs to `ShowcasePlayerVisualVariant` on the source visual prefab using Player Index 0–9 (P1–P10). Each slot must have its own valid Humanoid Avatar if you want distinct face/body meshes. After assigning variants, test the actual 5-on-5 scene and confirm all players animate correctly.

**Important:** do not rerun the upstream `Tools > AI4Animation > Build CrowdEyes-Twin 5v5 Basketball Demo` scene builder after replacing the shared visual prefab unless you first back up the prefab and accept that generated assets/scene files may be rewritten. That builder is intended to regenerate the original synthetic demo, not preserve custom local player assets.

## Private-asset boundary

Assets extracted from NBA 2K12 may be subject to copyright/trademark restrictions. This public repository contains none of those files. `PrivateAssets` is deliberately Git-ignored. Do not push ripped models, face textures, team logos, music, commentary, or other commercial-game content without redistribution rights.

## Actual completion gate

One player asset is not considered integrated until:
- The imported model is visible at actual gameplay distance in Unity Play Mode.
- The correct textures are on the correct material slots with correct sRGB/normal-map settings.
- The rig deforms correctly during movement, sprinting, shots, passes, and landings.
- The player appearance can be assigned to the active 5v5 match without breaking the simulation.
- The team-uniform fit, face/hair, number placement, camera distance, and animation have been visually inspected.

If the PS3 SDAT-to-payload step remains unresolved, report it as a blocker rather than cycling through unverified file scans.
