# High-Fidelity Acceptance Gate

"Done" means convincingly comparable in presentation intent to a modern NBA 2K/NBA Live game, not merely that Unity compiles. Do not mark the project complete until all categories below have verified evidence.

## Visual bar — non-negotiable

- **Players:** full-sized realistic, fully skinned athletes with detailed faces, hair, body shape, skin material, shoes, and distinct silhouettes. No Roblox-like primitives, blank mannequin bodies, or untextured placeholder models.
- **Uniforms:** fitted 3D jerseys and shorts with correct UV maps, cloth response, piping/trim, readable numbers and names, team-appropriate colors, and home/away variants. Jersey colors on a tank top are not sufficient.
- **Motion:** believable running, cuts, pivots, dribbling, gathers, shot forms, passes, rebounds, landings, and defensive contact. No visible T-poses, severe foot sliding, mesh tearing, or hand/ball separation in core gameplay.
- **Court and arena:** full-size hardwood and markings, realistic hoop/backboard/rim/net, crowd/stands, arena lighting, restrained floor reflections, and detailed backgrounds. A blank floor or a few primitive boxes does not pass.
- **Camera and broadcast:** stable 2K-style wide gameplay view with active possession and nearby spacing visible; no clipping or uncontrolled jitter. Add alternate broadcast/free camera only after gameplay framing works.
- **Presentation:** readable scoreboard, game/shot clock, team colors and score; consistent materials/shadows; no editor gizmos in the game view.
- **Performance:** 10 on-court athletes plus ball/arena simulation run stably on the target PC. Record actual average FPS and any GPU/inference limitations.

## Gameplay bar

- A 5-on-5 match loads from a clean project.
- Movement, sprint, passing, shooting, possession changes, scoring, and restarts all work.
- Camera input, selected-player control, ball targeting, and UI work together.
- No errors/exceptions in the Unity Console during a test match.
- No duplicate startup objects, broken references, or repeated asset imports.

## Asset and legal bar

- Authentic game assets come from user-authorized sources; no source game files are committed publicly without redistribution rights.
- Every imported model has a documented source, format, license/permission state, skeleton, and material mapping.
- The project explicitly distinguishes NBA 2K12 assets, licensed third-party models, and original substitutes.

## Verification record

A claim passes only after the relevant scene is opened and visually inspected in Unity Play Mode. Source code existing, files importing, successful compilation, or a deployment reporting READY do not satisfy this gate.

Until that evidence exists, label the project **in development**.
