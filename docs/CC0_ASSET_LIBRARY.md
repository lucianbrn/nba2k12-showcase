# Third-Party CC0 Geometry Added by the Showcase Tooling

The Unity Editor asset downloader is implemented in `patches/Assets/NBA2K12Showcase/Editor/ShowcaseCC0AssetLibraryTools.cs`. It retrieves each pack's manifest from the provider and downloads models only after the manifest marks the pack and each item as CC0 1.0. It saves the source JSON manifest and a local license note alongside the downloaded files.

## Pack A — Basketball Club and Street Courts

- Pack page: https://3dassets.dev/packs/basketball-club-and-street-courts
- Manifest: https://3dassets.dev/api/v1/packs/basketball-club-and-street-courts
- Licence: CC0 1.0 Universal, no attribution required according to the provider's manifest.
- Current manifest: 31 static GLB models, about 12.5 MB total. Use the current manifest as the source of truth because pack contents can change.
- Includes a marked outdoor court slab, anchored hoop with net, portable hoop, basketball, scorer table, scoreboard, ball storage, fencing and other practice props.
- The models have no ready-made gameplay colliders, neural animation, NBA branding or player likenesses. The outdoor court is not a pro NBA arena.

The tool downloads the entire pack to `Assets/NBA2K12Showcase/ExternalCC0/BasketballClubAndStreetCourts`.

The basketball asset in this pack is approximately 240–246 mm in diameter. Menu item 12 uses it as the render-only child of the existing ball object; the original Rigidbody, SphereCollider and possession logic remain authoritative.

## Pack B — Community Sports Hall and Changing Rooms

- Pack page: https://3dassets.dev/packs/community-sports-hall-and-changing-rooms
- Manifest: https://3dassets.dev/api/v1/packs/community-sports-hall-and-changing-rooms
- Starter scene: https://cdn.3dassets.dev/assets/35129/v1/model.glb
- Licence: CC0 1.0 Universal according to the provider's published pack metadata.
- Current pack listing: 49 GLB models, about 4.4 MB total; the starter scene includes the community hall layout.
- Represents a municipal multi-use sports centre with a sprung floor, retractable hoops, walls, roof trusses and other sports equipment.

The tool downloads the starter GLB to `Assets/NBA2K12Showcase/ExternalCC0/CommunitySportsHall`. Menu item 13 opens it in a separate scene for inspection. It is a reference/practice gym environment, **not an NBA arena**, and does not replace the existing full-court basketball simulation.

## Usage and verification

1. Run the installer so `com.unity.cloud.gltfast` 6.20.0 is added to the Unity manifest (with a backup).
2. Open the Unity `NeuralCourt` project and wait for Package Manager to finish.
3. Use menu item 10 or 11 to download and import the desired CC0 content.
4. Check the imported mesh in the Project window and review the source manifest written beside it.
5. In Play Mode, test actual scale, material appearance, collider alignment and performance before using a model in a match.

CC0 content is kept separate from NBA 2K12 proprietary files. The downloaded models are not the NBA 2K12 game assets and must not be described as such.
