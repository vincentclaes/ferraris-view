# Ferraris VR — Winksele, circa 1775

A local Unity 6 / OpenXR prototype: navigate the real Ferraris map, click a location, explore corresponding historical roads, rural buildings, fields and trees on real Flemish terrain, then return to the map.

The complete map-to-world journey runs on desktop and has been confirmed on a physical Quest 3. See `docs/validation.md` for the test/build evidence and measured limits.

The Dutch discovery features added in issues #2–#7 include current nearby addresses, home-address search with historical land use, a five-stop day story, spatial landscape sounds with captions, local place-name stories, and selectable object explanations. These additions pass the desktop journey and Android build; their integrated physical-headset acceptance is still pending.

## Requirements and setup

- Unity **6000.6.0f1** (Apple Silicon), activated through Unity Hub. Android Build Support, SDK/NDK and OpenJDK for Quest builds.
- Python 3.11+ with the pinned GIS dependencies; `uv` recommended. .NET 9 for running the shared C# coordinate checks outside Unity.
- Internet for the first data/package download; application data are bundled for offline runtime use.

From the repository root:

```bash
uv venv .venv
uv pip install --python .venv/bin/python -r pipeline/requirements.txt
.venv/bin/python pipeline/generate_area.py --lat 50.89795 --lon 4.64385 --size 1000
.venv/bin/python pipeline/export_world.py
.venv/bin/python pipeline/fetch_visual_assets.py
bash scripts/test.sh
bash scripts/unity.sh configure
```

The download pipeline validates service CRS, raster bounds, nonempty imagery and elevation coverage. It caches raw inputs and records source URLs/checksums. `--refresh` redownloads. No cloud backend is needed.

## Desktop

Open `unity/FerrarisVR` in Unity Hub, or run `bash scripts/unity.sh open`. The initial import creates `Assets/Scenes/FerrarisMapScene.unity`; open it and press Play. The scene bootstraps the map and world from `Assets/Resources/Winksele`.

```bash
bash scripts/unity.sh tests
bash scripts/unity.sh desktop
open builds/Winksele1775.app
bash scripts/smoke-desktop.sh
```

Set `UNITY_EDITOR` to override the installed editor executable. Close the editor before running batch builds/tests on the same project.

- Map: drag or arrow keys to pan; scroll, +/− or buttons to zoom; click or Enter at the map centre to enter the world. R resets Winksele; V toggles extracted roads/building footprints/vegetation.
- World: WASD, mouse look, Shift to walk faster, Escape or Return to Ferraris to return. Click the world to recapture the mouse after focus loss.
- A map click inside a building spawns at a nearby free point within 25m. The selected coordinate remains available separately.
- Debug overlay: mode, latitude/longitude, Unity X/Z, terrain TAW elevation, FPS.
- Menus: **Tab** releases the mouse and highlights the next button; **Shift+Tab** goes back; **Enter/Space** activates. Click the landscape to resume mouse look. **Escape** closes a panel; **M** returns to the map.
- Discovery shortcuts: **H** address search, **J** day story, **L** sound, **N** place names, **I** object inspection. While typing an address, these letters stay in the address. Every panel action can also be clicked or reached with Tab, including sources, story progress and volume. In the browser, **F2** returns focus to the website full-screen button.

## Quest 3

```bash
bash scripts/unity.sh quest-configure
bash scripts/unity.sh quest  # also verifies packaged data and OpenXR libraries
# Developer mode and USB debugging must be enabled on your headset:
bash scripts/quest-device.sh devices
bash scripts/quest-device.sh install
bash scripts/quest-device.sh run
bash scripts/quest-device.sh logs
```

The build script selects Android ARM64, IL2CPP, Vulkan, OpenXR, Meta Quest support, Oculus Touch profile and single-pass instanced rendering. The runtime uses the Input System for head/controller poses and controls. Desktop remains available without an XR runtime.

The device script finds ADB inside the Unity installation; set `ADB` to override it. With multiple devices, pass the Quest serial as the second argument. Installation does not proceed when the device is absent or unauthorized. See [headset acceptance steps](docs/quest-validation.md).

- Map: right controller ray + trigger selects; left stick pans; right stick up/down zooms; B returns to map.
- World: left stick moves relative to gaze. A light push moves slowly; speed increases progressively to **6 m/s at full tilt**. Right stick snaps by 30 degrees; B returns to map.
- Discovery: use the right ray and trigger on the world-space menu; left **X** opens address search. **B** closes an open panel before returning to the map. Object inspection enables the ray in the landscape.
- Quest target: 72 FPS. The original simple scene measured 72–73 FPS in a short Quest 3 session. The new graphics pass adds physically based surfaces, a detailed church with LODs, short-range shadows, instanced foliage and distance-limited ground cover; it requires a new hardware performance check. See [graphics upgrade and asset sources](docs/visual-upgrade.md).

## Website — Land van Weleer

**[Land van Weleer — Wandel door het Vlaanderen van toen](https://land-van-weleer.vercel.app).** The browser edition runs the same map, landscape and Dutch discovery features on a computer with mouse and keyboard. It has a start screen, loading progress, full-screen control, instructions and sources. Mobile visitors get readable information and a desktop-play notice. Immersive Quest VR remains the native Android app.

```bash
# Unity Hub: add WebGL Build Support for the installed editor.
bash scripts/unity.sh web
python3 scripts/serve-web.py
# Open http://localhost:8765 to test; then deploy a preview:
bash scripts/deploy-web.sh
```

Set `VERCEL_CLI` to the CLI executable if it is not on PATH. Deployment targets the `land-van-weleer` project in `vincentclaes-projects`; production deployment requires explicitly passing `--prod`. Unity compiles locally; Vercel serves only the generated `builds/web` directory. See [web build and validation](docs/web.md).

Vercel automatically assigned the first default deployment to production on 12 September 2026. Subsequent default deployments are previews; the live site uses the domain above.

The browser fetches the historical map directly from the public Digitaal Vlaanderen WMS with KBR attribution. The local raster is excluded from the hosted build and restored after building; an internet connection is needed for the map. Native builds keep their bundled offline map. The generated website is about 68 MiB; the first visit downloads the 3D assets after pressing **Stap binnen in 1775**.

## Data and architecture

`pipeline/generate_area.py` downloads/crops official Ferraris WMS and DHMV WCS data. `pipeline/export_world.py` creates GeoJSON, metric Unity data, landcover and road textures, red-symbol candidates and alignment overlay. `data/winksele/tracing.json` supplies roads and land parcels; `buildings-reviewed.json` supplies the reviewed building contours and legend categories.

Unity `AreaData.cs` owns coordinate/height conversion; `FerrarisApp.cs` owns mode/input/navigation; `HistoricalWorld.cs` constructs the terrain, architecture and farm animals; `WorldVegetation.cs` batches trees and nearby ground cover. `BuildProject.cs` creates the scene and desktop/Quest build configuration. `JourneySmoke.cs` exercises the real built player and captures map/world/top-down screenshots.

Discovery content is bundled in `Assets/Resources/Discovery`. Implementation, source distinctions and acceptance evidence for each issue are in [docs/issues](docs/issues); [object coverage](docs/issues/07-object-discovery.md) includes the complete 150-symbol legend inventory and the current rendered types.

See [source, license and CRS documentation](docs/data-sources.md). Raw rasters, generated Unity resources, editor caches and builds are excluded from Git. Re-run both pipeline commands after cloning. The Ferraris service metadata does not grant an open redistribution license: review rights before sharing map-containing builds.

## Another Belgian area

```bash
.venv/bin/python pipeline/generate_area.py --name another-area --lat LAT --lon LON --size 1000
```

Create reviewed `data/another-area/tracing.json` and `buildings-reviewed.json` files matching the new crop. The latter needs the raster SHA-256, preview size, unique building IDs, pixel contours, evidence and supported legend categories (`building` or `church`); copy the Winksele schema, not its coordinates or historical identifications. Then run `pipeline/export_world.py --name another-area`. Export replaces the active Unity resource area. Downloading is area-parameterized; semantic tracing is currently manual and is not automatically transferable. WMS coverage is the Flemish part of Belgium; validate availability before choosing an area outside Flanders.

## Limitations and next steps

Rural houses and animals are authored approximations. The church uses the detailed Maria-Hemelvaartkerk asset from the church task; its 1786 sacristy is hidden, but the remaining modern-exterior interpretation is not a verified 1775 reconstruction. Reviewed building polygons follow readable painted symbols, with uncertain brush edges and perspective; road widths remain approximate. See [map review and legend categories](docs/period-buildings.md). The historical mosaic can contain local georeferencing distortion. Terrain is modern. Vegetation here represents orchards, not dense forest. Automated extraction still needs human review. Animal anatomy, richer farmyard dressing and species-specific vegetation remain areas for visual refinement. The graphics are a richer real-time reconstruction, not a claim of film-quality photorealism.
