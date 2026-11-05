# Finish Audit — 2026-11-05

This project was designated for completion in Muse after work in ChatGPT/Codex
stopped with the repository unfinished (last commit 2026-10-13, `960f8ac`).
This audit records what was verified, what was fixed, and what still requires
a Windows PC with Unity. Per `docs/HIGH_FIDELITY_ACCEPTANCE_GATE.md`, the
project remains **in development**: nothing here claims the gate is passed.

## What this repository is (verified)

- An overlay/patch repository, not a standalone Unity project. There is no
  `ProjectSettings/`, scene, or `Packages/` at the root.
- 16 C# files, 4,666 lines, under `patches/Assets/NBA2K12Showcase/`
  (6 runtime, 10 editor), plus PowerShell installer/validator in `tools/`.
- The game itself is the pinned `NeuralCourt` submodule
  (`84961525d7783df7b6e449391195924c91d315eb`, which is also upstream HEAD as
  of 2026-11-05). Its `ProjectSettings/ProjectVersion.txt` confirms Unity
  **6000.5.3f1**, matching the README.
- Editor menu items 0–20 are all present.

## Verified on 2026-11-05 (Linux VM, no Unity)

1. **C# syntax + source contracts** — the repository's own checker
   (`tools/CSharpSyntaxCheck`, .NET 8.0.427) was run locally:
   16/16 files PASS, all contracts PASS, 0 errors. GitHub Actions run #79 for
   `960f8ac` also shows Status Success. This validates syntax and selected
   source contracts only — it does not compile against Unity assemblies.
2. **Upstream API cross-check** — every upstream member the overlay calls was
   checked against the pinned NeuralCourt source and found present, including:
   `BasketballScoreTracker.Scored / TeamZeroScore / TeamOneScore / ResetScore`,
   `BasketballPossessionManager.Owner / Passer / BallState / RequestPass /
   ResetPossession`, `BasketballMatchController.PossessionManager /
   SelectPlayer / ActivePlayer / ActivePlayerIndex`, and
   `BasketballPlayerAppearance.Apply()`.
3. **Reflection contract** — the runtime menu sets the appearance profile's
   private fields by name because upstream
   `BasketballTeamAppearanceProfile.Configure(...)` is `#if UNITY_EDITOR`
   only. All five reflected field names (`teamId`, `displayName`,
   `jerseyColor`, `shortsColor`, `numberColor`) and the reflected
   `jerseyNumber` (on `BasketballTeamMember`) and `teamProfiles` (on
   `BasketballPlayerAppearance`) fields exist with matching types in the
   pinned upstream source.
4. **Installer dry-run** — the installer's exact transformations were applied
   to a pristine clone of the pinned upstream: the overlay copies cleanly
   (18 files), both upstream gamepad source patches apply with their safety
   guards satisfied, and `Packages/manifest.json` gains glTFast 6.20.0.
   The new `tools/Install-ShowcasePatch.sh` (Linux/macOS mirror of the
   PowerShell installer) reproduces byte-identical patched files and is
   idempotent on a second run, with timestamped backups.
5. **Visual baseline inspected** — the upstream screenshot
   `NeuralCourt/Assets/Screenshots/NeuralCourt_Basketball_5v5.png` was viewed
   directly: synthetic athletes in plain red/blue kits in an empty box gym.
   This is the honest starting point the acceptance gate describes, far from
   the 2K-fidelity target.

## Fixed in this pass

- **2013 Heat five corrected in all three roster definitions**
  (`ShowcaseRuntimeMatchupMenu.cs`, `Editor/ShowcaseEraMatchupTools.cs`,
  `Editor/ShowcaseGameSetupTools.cs`). The Heat "starting five" previously
  listed Ray Allen (SG, #34) as the fifth starter with Chris Bosh at PF and
  no center — while Ray Allen was also the 2008 Celtics' starting SG in the
  same matchup. Ray Allen was Miami's sixth man in 2012–13; the Heat's
  most-used starting lineup (Basketball-Reference 2012–13 lineup data,
  687:18 minutes, the team's #1 five-man unit) was Chalmers / Wade / James /
  **Udonis Haslem (PF, #40)** / **Chris Bosh (C, #1)**. The rosters now use
  that five everywhere, and three new source contracts in
  `tools/CSharpSyntaxCheck/Program.cs` lock it in.
- **`ShowcasePreGameIntro.cs` marked LEGACY / UNUSED.** No editor tool, scene,
  or build setting creates or references it; its Lakers are not one of the
  three supported teams. The supported flow is the runtime matchup menu
  (menu item 20). The file is kept for reference, with the header saying so.
- **`tools/Install-ShowcasePatch.sh` added** — a tested Linux/macOS mirror of
  the Windows installer (see verification #4).

## Still unfinished (unchanged, honestly)

- No realistic player models, face textures, fitted uniform UVs/logos, or
  NBA 2K12 motion clips are in this repository. `PrivateAssets/` is empty by
  design (local imports are git-ignored). The next asset milestone in
  `ASSET_IMPORT_STATUS.md` still stands: one real player export with mesh,
  UVs, weighted skeleton, and textures, integrated and deformation-checked
  in Play Mode.
- No Play Mode test has ever been run. HUD/clock/possession behaviour,
  controller behaviour, and FPS with 10 athletes plus neural inference are
  all unverified until the PC steps below are done.
- Unity cannot run on the Muse Linux VM: no Unity Editor is installed, the
  editor requires a Unity account licence activation, and the project's
  neural inference targets a desktop GPU. This was checked directly
  (filesystem search found no Unity; the .NET checker and installer dry-run
  above are the strongest verification available without the editor).

## Finishing routes added later on 2026-11-05 ("finish the game" pass)

Two routes now exist to get a real Unity compile without a local install,
plus a Linux/macOS validator:

- `.github/workflows/unity-activation.yml` — manual, one-time. Generates the
  Unity 6000.5.3f1 activation file (`.alf`) as a downloadable artifact.
- `.github/workflows/unity-compile.yml` — manual. Checks out the pinned
  submodule, installs the overlay with `tools/Install-ShowcasePatch.sh`,
  compiles every script in the real Unity 6000.5.3f1 editor via GameCI, and
  runs the upstream EditMode tests.
- `tools/Test-UnityProject.sh` — Linux/macOS mirror of the batch-mode
  validator (guard stages tested on a machine with no Unity installed).

The single remaining owner step for the CI route: run the activation
workflow, submit the `.alf` at https://licence.unity3d.com/manual, and save
the returned `.ulf` text as the repository secret `UNITY_LICENSE`. Then run
the compile workflow. A green run proves real Unity compilation + upstream
tests; Play Mode visuals still require a real machine (route below).

## Exact PC steps that remain (Windows, ~15 minutes plus download time)

1. Install Unity Hub and the exact editor **6000.5.3f1**.
2. Clone fresh and install the overlay:
   `git clone --recurse-submodules https://github.com/lucianbrn/nba2k12-showcase.git`
   then `powershell -ExecutionPolicy Bypass -File .\tools\Install-ShowcasePatch.ps1`
3. (Optional compile gate) `.\tools\Test-UnityProject.ps1` — batch-mode
   import/compile; a clean log is required before Play Mode.
4. Open `NeuralCourt` in Unity Hub, open `Assets/Scenes/BasketballDemo.unity`,
   run **Tools > NBA2K12 Showcase > 16 — One-Click Core Setup**, then
   **20 — Create Playable Matchup Menu**, then **14 — Run Full Game
   Readiness Audit** and save the generated report.
5. Press Play from `NBA2K12Menu`. Test: pick each of the three matchups,
   movement/sprint, pass (including LT + A camera-relative pass), shoot,
   scoring, shot-clock violation, quarter/overtime transitions, pause/restart,
   and a wired Xbox/PlayStation controller. Confirm the Console shows no
   errors and record average FPS.
6. Report the readiness-audit file and any Console errors back — those two
   artifacts are what move this project from "syntax-verified overlay" to
   "verified Play Mode build."
