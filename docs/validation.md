# Validation log — 12 September 2026

Latest update — 13 September 2026: the integrated navigation, Marie guide and map-aligned world build for Quest with verified packaged data, 19 speech clips and signature. The current merged-mainline APK is 80,474,320 bytes (built from `a00c1f3`). The current desktop journey passes 356 checks and all 33 Unity tests pass; the map-edge update passed 116 dedicated checks and 19 XR input replay checks. ADB finds no attached headset. See [current Quest receipt](quest-validation.md). The hardware results below describe the earlier simple scene, not the new package.

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

Map-edge update, 13 September 2026: the previous pan limit prevented dragging at 1× and centring boundary points. The new built-player regression first failed on the unzoomed drag, then passed 116 checks after allowing desktop pan to the area boundary. Actual mouse events reach all four corners at 1×, 2× and 8×; clicks select within 0.02m of the intended coordinate. Overzicht and R reset zoom and pan; arrow keys and Enter reach the northwest boundary; blank space outside the map does not enter the world. XR retains its texture-safe pan limits. The full desktop journey passes 356 checks, controller replay passes 19, and all 33 Unity EditMode tests pass. Evidence: `/tmp/ferraris-map-edges-red/`, `/tmp/ferraris-map-edges-green/`, `/tmp/ferraris-map-edges-xr/` and `artifacts/unity-tests.xml`. This update has not been tested on headset hardware.

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
- Installed and launched successfully on a physical Quest 3 on 12 September 2026. OpenXR reached `XR_SESSION_STATE_FOCUSED`; both Oculus Touch interaction profiles were recognized.
- Vincent confirmed the complete map → select → landscape → movement → B return flow works in the headset.
- The app's own VrApi records after the first world transition include 37 one-second samples (17:27:28–17:28:04 device time): 72–73 FPS against a 72 Hz target, median reported app time 2.4 ms. This is a short observed session, not a five-minute thermal or worst-case performance test. Evidence: `artifacts/quest-process.log` and `artifacts/quest-metrics.json`.
- Hardware logs exposed a stripped `SphereCollider` used by the primitive map marker and a redundant VR field-of-view assignment. The marker now references its concrete collider type so IL2CPP preserves it; field of view is assigned only in desktop mode.
- The corrected APK rebuilt successfully and its signature verifies. SHA-256: `91017f85255aff673a55599808caccf521c1e0d3459ca83ade6be6e2526b6242`. Test device: Quest 3 (`eureka`), Android build `UP1A.231005.007.A1`. Reinstallation requires renewed USB authorization after Unity restarted ADB.

Builds and map-containing screenshots remain local; they are excluded from Git along with downloaded rasters and editor caches.

## Further hardware checks

The complete user journey is confirmed on Quest 3. Longer performance sampling across the village/tree clusters, thermal behavior, headset removal/resume and extended comfort checks remain useful follow-up work; the short measurement above does not establish those results.
