# Ferraris VR — Winksele, circa 1775

A local Unity 6 / OpenXR prototype: navigate the real Ferraris map, click a location, explore corresponding historical roads, rural buildings, fields and trees on real Flemish terrain, then return to the map.

The complete map-to-world journey runs on desktop and has been confirmed on a physical Quest 3. See `docs/validation.md` for the test/build evidence and measured limits.

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

- Map: drag to pan, scroll or buttons to zoom, click to enter the world. Reset restores Winksele. Show vectors displays extracted roads/building footprints/vegetation.
- World: WASD, mouse look, Shift to walk faster, Escape or Return to Ferraris to return. Click the world to recapture the mouse after focus loss.
- A map click inside a building spawns at a nearby free point within 25m. The selected coordinate remains available separately.
- Debug overlay: mode, latitude/longitude, Unity X/Z, terrain TAW elevation, FPS.

## Quest 3

```bash
bash scripts/unity.sh quest-configure
bash scripts/unity.sh quest
# Developer mode and USB debugging must be enabled on your headset:
bash scripts/quest-device.sh devices
bash scripts/quest-device.sh install
bash scripts/quest-device.sh run
bash scripts/quest-device.sh logs
```

The build script selects Android ARM64, IL2CPP, Vulkan, OpenXR, Meta Quest support, Oculus Touch profile and single-pass instanced rendering. The runtime uses the Input System for head/controller poses and controls. Desktop remains available without an XR runtime.

The device script finds ADB inside the Unity installation; set `ADB` to override it. With multiple devices, pass the Quest serial as the second argument. Installation does not proceed when the device is absent or unauthorized. See [headset acceptance steps](docs/quest-validation.md).

- Map: right controller ray + trigger selects; left stick pans; right stick up/down zooms; B returns to map.
- World: left stick moves relative to gaze; right stick snaps by 30 degrees; B returns to map.
- Quest target: 72 FPS. A short physical Quest 3 session measured 72–73 FPS at 72 Hz after entering the world. Longer stress and comfort testing remains outstanding. The scene uses a single terrain mesh/texture, combined building/road meshes, shared instanced tree mesh, simple mobile shader, no real-time shadows, and a 2048px ASTC map.

## Data and architecture

`pipeline/generate_area.py` downloads/crops official Ferraris WMS and DHMV WCS data. `pipeline/export_world.py` creates GeoJSON, metric Unity data, landcover texture, red-symbol candidates and alignment overlay. `data/winksele/tracing.json` is the reviewed manual interpretation used for this area.

Unity `AreaData.cs` owns coordinate/height conversion; `FerrarisApp.cs` owns mode/input/navigation; `HistoricalWorld.cs` constructs geometry and tree instances. `BuildProject.cs` creates the scene and desktop/Quest build configuration. `JourneySmoke.cs` exercises the real built player and captures map/world/top-down screenshots.

See [source, license and CRS documentation](docs/data-sources.md). Raw rasters, generated Unity resources, editor caches and builds are excluded from Git. Re-run both pipeline commands after cloning. The Ferraris service metadata does not grant an open redistribution license: review rights before sharing map-containing builds.

## Another Belgian area

```bash
.venv/bin/python pipeline/generate_area.py --name another-area --lat LAT --lon LON --size 1000
```

Create a reviewed `data/another-area/tracing.json` in the same schema with matching origin/size, then run `pipeline/export_world.py --name another-area`. Export replaces the active Unity resource area. Downloading is area-parameterized; semantic tracing is currently manual and is not automatically transferable. WMS coverage is the Flemish part of Belgium; validate availability before choosing an area outside Flanders.

## Limitations and next steps

Procedural houses and church are illustrative archetypes, not architectural reconstructions. Building sizes and road widths are approximate; the historical mosaic can contain local georeferencing distortion. Terrain is modern. Vegetation here represents orchards, not dense forest. Automated extraction still needs human review. Improve historical detail and segmentation only after validating the complete map/world journey and Quest comfort/performance.
