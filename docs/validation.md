# Validation log — 12 September 2026

## Verified independently of Unity

- Official Ferraris WMS layer exists and advertises EPSG:31370. KBR/NGI lookup identifies Winksele on sheet 93, Cortenberghe.
- Real 2048×2048 Ferraris crop downloaded and visually inspected.
- Real DHMV II GeoTIFF downloaded; CRS, bounds, nodata absence and pixel-centre/terrain-vertex alignment checked.
- DEM elevation: approximately 29.50–61.71m TAW.
- Four Python GIS tests pass, including 1,000 runtime-grid comparisons against PyProj (<1cm tolerance).
- Actual `AreaData.cs` executed under .NET 9: 1,089 coordinate round trips including area edges/corners, origin, UV orientation, bounds rejection and finite elevation pass. The first run exposed a Newton boundary-clamping bug; fixed and rerun successfully.
- Export: 13 road alignments, 57 historical building locations, 12 fields, 8 orchard/enclosure polygons, 257 instanced tree positions. Alignment overlay inspected against the original raster.
- OpenCV experiment: 885 red components retained separately; parish numbers cause false positives. Manually reviewed tracing drives the world.

## Unity and the built desktop player

- Unity 6000.6.0f1 imports and compiles the project. Input System and XR package versions match the editor's bundled PackageManager manifest; earlier package versions used removed editor APIs.
- Two Unity EditMode tests passed: map resources/coordinates and real terrain collider, roads, buildings and instanced vegetation.
- macOS development player built successfully at `builds/Winksele1775.app`.
- The built-player journey passes through actual Input System mouse/keyboard events: toolbar zoom, a quick drag delivered entirely within one frame, wheel zoom, selecting through the camera's map raycast after pan/zoom, corresponding world spawn, W-key walking supported by terrain, and Escape back to map.
- The quick-drag regression initially failed because Unity merged the mouse-down position into the endpoint. Preserving mouse event history fixes that failure; the regression now passes.
- Native UI interaction also entered the world from a map click. The application was visually inspected running on this Mac.
- Captured map, zoomed map, street-level world, top-down world and returned-map images. The top-down world was compared with the Ferraris/vector overlay: roads, village buildings and orchard clusters visibly correlate. Building models remain illustrative.

Reproduce with `bash scripts/unity.sh tests`, `bash scripts/unity.sh desktop` and `bash scripts/smoke-desktop.sh`. Local evidence is written to `artifacts/unity-tests.xml`, `artifacts/player-smoke.log`, `artifacts/journey.json` and `artifacts/01-map.png` through `05-return.png`. The journey JSON explicitly records `hardwareVRVerified: false`.

## Standalone Quest build

- Installed Unity's Android Build Support, SDK/NDK and OpenJDK through Unity Hub.
- `bash scripts/unity.sh quest` completed successfully and produced `builds/Winksele1775.apk` (about 54 MB).
- ARM64 / IL2CPP / Vulkan / OpenXR / Meta Quest support / Oculus Touch profile / single-pass instanced rendering are configured.
- The packaged manifest includes the VR category and required VR head tracking; ARM64 Unity and OpenXR libraries are present. Minimum Android SDK is 29; this build targets SDK 36.
- Selecting Android before the batch build fixed an OpenXR validator exception caused by validating against the desktop target. The final build reports only an optional OpenXR input-polling latency recommendation.
- No physical headset is connected to ADB, so installation, headset/controller behavior and on-device frame rate remain unverified.

Builds and map-containing screenshots remain local; they are excluded from Git along with downloaded rasters and editor caches.

## Hardware acceptance still required

A physical Quest 3 is needed to verify headset/controller tracking, map ray interaction, thumbstick motion, comfort, standalone rendering and sustained 72 FPS. No headset performance claim is made before that test. An APK alone does not satisfy this acceptance criterion.
