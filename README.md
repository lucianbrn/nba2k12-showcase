# NBA 2K12 Showcase Reconstruction

A Unity 6 basketball-game development project that tracks the public NeuralCourt 5-on-5 basketball simulation as a Git submodule and layers project-owned presentation tools on top.

## Status

This is a reconstruction project, not a finished AAA game or an NBA 2K12 asset dump. The referenced Unity project provides a synthetic skinned athlete, court assets, team appearance profiles, basketball ball/pass/shoot systems and motion inference. It is not an NBA player likeness and does not include authentic NBA 2K12 faces, uniforms, logos or animation clips.

## Setup

Clone with submodules:

```powershell
git clone --recurse-submodules https://github.com/lucianbrn/nba2k12-showcase.git
```

If already cloned without submodules, run `git submodule update --init --recursive`.

Run the editor-patch installer before opening Unity:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\Install-ShowcasePatch.ps1
```

Open the `NeuralCourt` folder in Unity Hub using the editor version in its README (Unity 6, `6000.5.3f1`). Open `Assets/Scenes/BasketballDemo.unity` and enter Play Mode.

## Editor tools

After the patch compiles, use `Tools > NBA2K12 Showcase` to apply URP TV-style post-processing and quality settings, apply a Lakers/Celtics-inspired color palette through the existing uniform renderer API, or audit the active scene. The color palette is not an official uniform texture or logo.

## Quality priorities

1. Confirm the upstream 5-on-5 scene runs on the target machine.
2. Improve broadcast camera framing, lighting, court presentation and crowd composition.
3. Add original or properly licensed uniform designs, materials, team data and roster support.
4. Import only game assets the user is authorized to use; keep commercial-game files out of public commits unless redistribution is authorized.
5. Validate movement, passing, shooting, collision, animation and performance in Play Mode.

Do not claim a visual or gameplay improvement is complete until the relevant Unity scene has been inspected and tested.

See [ASSET_IMPORT_STATUS.md](ASSET_IMPORT_STATUS.md) for verified and missing assets and redistribution notes.
