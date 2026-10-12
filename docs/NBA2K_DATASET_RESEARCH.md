# NBA2K Dataset: applicability to the Unity showcase

Repository reviewed: https://github.com/luyangzhu/NBA2K-dataset

## What it provides

The repository describes the dataset from *Reconstructing NBA Players* (ECCV 2020), captured from NBA 2K19. It contains RGB captures, triangle meshes with UV coordinates, texture maps, 3D poses, and camera projection matrices. The repository README distinguishes raw per-frame player meshes from registered/resampled meshes intended for consistent-topology research, and identifies 28 released synthetic players.

The dataset is **not a drop-in Unity player pack**. Its documented geometry is OBJ/mesh parts plus NumPy pose/weights/joint data, rather than ready-made FBX prefabs with Unity Humanoid Avatars, animation controllers, and game-ready materials. A conversion pipeline and rig validation are necessary.

## Access and licensing

The README asks users to request the dataset through the linked Google form. It states that current NBA player data is not released due to copyright concerns; the distributed material is captured from synthetic 2K-made players. The listed license is Creative Commons Attribution-NonCommercial-ShareAlike 4.0. Therefore:
- Treat this as a **non-commercial research asset source**, not a source for a commercial game.
- Cite Zhu et al., *Reconstructing NBA Players*, ECCV 2020.
- Indicate changes and follow the ShareAlike requirements when sharing derivatives.
- Do not claim the provided synthetic players are authentic real-player likenesses.
- Do not add dataset files to this public game repository; use local, ignored private assets after access is authorized.

Paper: https://arxiv.org/abs/2007.13303
Dataset/request page: https://github.com/luyangzhu/NBA2K-dataset

## Practical use in this project

The likely useful route—after access is granted—is to evaluate the released synthetic-player rest-pose meshes and pose/weight metadata, convert one example to a properly scaled and textured mesh, construct a valid skeleton/skin binding, export an FBX, then run the project's Humanoid rig audit and in-scene movement/shot animation checks. The README notes the raw collection can require more than 100 GB of disk space, so do not download all raw archives on an 8 GB RAM machine without planning storage first.

This source may help develop more detailed basketball-player geometry and research motion processing, but it does not itself supply a finished commercial-grade NBA game or instantly solve the missing realistic arena, broadcast, gameplay polish, and complete character animation requirements.
