# Land van Weleer — browser edition

Brand: **Land van Weleer**, with **Wandel door het Vlaanderen van toen.**

Website: **https://land-van-weleer.vercel.app**. Latest production release: `dpl_CLorvzRqBXGB9LwyZrD9BcCLW95c`, READY on 13 September 2026. Runtime commit `3cdb7a3` merges remote mainline `032fa89` (PRs #19 and #22) into `codex/terrain-roads`: the new navigation and voiced Marie guide run alongside mapped buildings, terrain roads and five animated tableaux. Marie uses the tableaux's actual locations. The exact browser title remains Land van Weleer. The project is `land-van-weleer` (`prj_Fc3TN3FPfmYxSPhqD3H72RrWEOUQ`). Locally tested build hashes: `artifacts/remote-merge-web-hashes.json`. The full world-detail stack was merged through [PR #23](https://github.com/vincentclaes/ferraris-view/pull/23) into `codex/winksele-vr` at `e8bdb1b`.

Validation: 6 GIS tests, 1,089 coordinate checks, 31 Unity tests and 356 built-player journey checks pass. The journey covers every guide destination, voice, questions, waiting, cancellation/resume, all five animated tableaux, map position/heading preservation and world-space ray controls. The merge exposed clipped source text; increasing its panel height fixed both source pages, verified in the repeated journey.

The flow under browser test was start → map → world → mouse look/walking → current-location map → address search → Marie invitation/question/sources → leave story. Chrome/Playwright checks passed at 1440×1000 and 1024×768; mobile information passed at 390×844. Page identity, rendering, interaction screenshots, full screen, keyboard focus, loading errors and recovery after denied pointer lock were checked. No application errors occurred. The first scripted story attempt stayed too far from Marie; the corrected run explicitly returned to map selection, used Zoek Marie, and the inspected images show the accepted dialogue and distinct question answer. Browser plugin not available; existing Playwright/Chrome used. Evidence: `/tmp/land-van-weleer-publish-qa.json`, `/tmp/land-van-weleer-publish-*.png`. These are local checks of the exact published files; no deployed URL was fetched. The same runtime is now included in a verified local Quest APK; [package receipt and headset limits](quest-validation.md). Hardware validation remains open.


## Field vegetation preview — 13 September 2026

Preview: https://land-van-weleer-glih0tc7z-vincentclaes-projects.vercel.app, deployment `dpl_CsaMQnc2KerRRerabXTH76hCXAKP`, READY. Runtime `4711476` adds detailed wind-bent grain, planting clearance along full road segments, individual ground-cover range filtering and gradual distance transitions. This preview does not replace production. Exact tested file hashes: `artifacts/ground-cover-web-hashes.json`.

Chrome/Playwright at `http://localhost:8765` verified map → western grain field → W movement/mouse look → current-location map without application errors. Inspected screenshots show the field, new grain and western map marker. Page identity, nonblank rendering, no error overlay and layouts at 1440×1000, 1024×768 and 390×844 passed. Evidence: `/tmp/land-van-weleer-range-qa.json` and `/tmp/land-van-weleer-range-*.png`. Browser plugin not available; existing Playwright and the repository's gzip-aware `scripts/serve-web.py` were used. The native source passes 33 Unity tests, 356 journey checks and three rendered ground-cover fade checks. The signed Quest package is recorded separately. No deployed URL was fetched.

The northwestern toolbar overlap exposed an edge-selection issue, resolved by the map navigation update below.

## Merged map navigation preview — 13 September 2026

[PR #24](https://github.com/vincentclaes/ferraris-view/pull/24) and [PR #25](https://github.com/vincentclaes/ferraris-view/pull/25) are merged into the repository's default branch, `codex/winksele-vr`, at `aac2e05`. There is no branch named `main`. The WebGL build uses source `4dd7d24`, whose tree matches that merge commit.

Preview: https://land-van-weleer-ddz1tw263-vincentclaes-projects.vercel.app, deployment `dpl_7KArD47Rvd9PgqxU58aLmqRPWHMA`, READY. Production remains the release recorded above; automatic approval review rejected replacing production without explicit production confirmation. Exact tested file hashes: `artifacts/map-edges-web-hashes.json`.

Desktop map panning now brings every corner to the centre, including at 1× zoom. R and Overzicht reset both zoom and pan. Chrome/Playwright verified entry at all four corners, blank-space rejection, arrow-key navigation and Enter, plus northwest entry at 1024×768, with no application errors. Screenshots of map corners and the world position marker were inspected. Evidence: `/tmp/land-van-weleer-map-edges-qa.json` and `/tmp/land-van-weleer-edges-*.png`. Native regression passes 116 map-edge checks, 356 journey checks, 19 controller replay checks and 33 Unity tests. Browser checks ran locally on the exact uploaded files; no deployed URL was fetched. The merged runtime was subsequently built and package-verified for Quest; see [current APK receipt](quest-validation.md). Hardware validation remains open.

## Build and hosting

`bash scripts/unity.sh web` uses Unity 6000.6.0f1 WebGL Build Support, IL2CPP/WebAssembly, WebGL 2 and the custom `Assets/WebGLTemplates/LandVanWeleer` template. It is a release build of the existing runtime, not a rewritten approximation. Desktop mouse/keyboard controls and all discovery content are shared with the native app. WebXR is not added; immersive Quest remains the Android build.

Church textures are capped at 2048 px and other landscape textures at 1024 px on WebGL (sky stays 2048). Native texture settings remain unchanged. This reduces the complete gzip deployment from approximately 117 to 68 MiB. The canvas follows the browser window, including full-screen mode, at one rendering pixel per CSS pixel. The native UI scales to fit a 1440×1000 reference area without clipping its panels.

`web/vercel.json` sets the required gzip encoding and MIME types for `.wasm.gz`, `.js.gz` and `.data.gz`. The local standard-library server uses equivalent headers. `bash scripts/deploy-web.sh` deploys `builds/web` as a preview to Vercel project `land-van-weleer`, scope `vincentclaes-projects`. It does not upload the repository, original models, APKs or local evidence. A generated `.vercelignore` excludes numbered sync copies of old pages and bundles; the final rename deployment contained eight input files. No Vercel build process or application backend is needed.

The cover is rendered directly from the project with `Ferraris.Editor.BuildProject.WebCover`; it contains no map raster or gameplay overlays. `web/cover.jpg` is copied into the output during a web build.

## Mouse and keyboard

The small north-up map shows the current player position and camera heading. M or a click enlarges it. The world pauses movement while the map is open; closing it preserves position and heading. Andere startplek explicitly returns to location selection. Escape closes a panel or releases the mouse, without discarding the walk.

All visitor-panel buttons accept mouse clicks and Tab/Shift+Tab navigation, with a gold focus outline and Enter/Space activation. Arrow keys also move between focused buttons. Tab releases pointer lock; clicking the landscape resumes mouse look. Panels block background movement and pointer recapture. Escape closes the current panel.

Kaart, Ontdek and Hulp are the main controls. Ontdek groups address search, the day story, place names, object inspection and sound settings. M toggles the location map; F1 opens help. H, J, L, N and I remain optional shortcuts. Address entry owns letter keys and spaces, so typing cannot trigger another feature. Click the address row to resume typing after navigating buttons. Map arrows pan, +/− zoom, R resets, V toggles vectors and Enter enters the centre point. WASD walks, Shift speeds up and the mouse looks around. F2 leaves the canvas for the website help control; normal browser Tab reaches full screen.

## Map consultation

The [official historical-cartography service](https://www.vlaanderen.be/datavindplaats/catalogus/raadpleegdienst-voor-historische-cartografie) permits public access. The web runtime requests the current area's 2048×2048 Ferraris crop from that service using EPSG:31370 and invariant-culture bounds. CORS access and the actual image response were verified on 12 September 2026. Attribution and direct KBR/service links appear in the website and the experience.

The native cached raster is temporarily moved outside Resources while building, restored in `finally`, and checked against the packed-asset report. The website therefore does not redistribute that raster from Vercel. Unlike the native edition, initial map loading needs internet. A failed map request gives a Dutch reload instruction. Bundled object explanations, addresses, stories and sounds remain local after the initial application download.

## Name change validation — 13 September 2026

The renamed Unity WebGL release built successfully. Chrome/Playwright at 1440×1100 and a 390×844 touch viewport verified the exact browser title, header/footer, mobile guidance and absence of the former brand in the generated page. Help navigation, map startup and world entry/return passed without JavaScript errors. Desktop and mobile screenshots were inspected; no horizontal overflow. The Browser plugin was unavailable, so the installed Playwright/Chrome runtime was used. Temporary evidence: `/tmp/land-van-weleer-desktop.png`, `/tmp/land-van-weleer-mobile.png`, `/tmp/land-van-weleer-map.png`.

## Building review — 13 September 2026

The website build now contains 53 reviewed building polygons and source-supported categories, including the corrected church anchor. Ordinary building selection and collision preserve concave courtyards. V displays readable yellow building outlines for comparison with the raster. [Review and validation](period-buildings.md) records the scope and uncertainty.

The full Chrome mouse/keyboard journey passed on this update. A final build check confirmed visible outlines and the Dutch unknown-function explanation, with no JavaScript application errors. GIS tests: 5; standalone coordinate checks: 1,089; Unity EditMode: 16; desktop journey: 321. Earlier validation below remains historical context.

## Web-first navigation — earlier local validation, 13 September 2026

Branch `codex/web-first-ux` builds on the local `codex/period-building-forms` branch, preserving its 53 reviewed building polygons and historical explanations. These navigation changes were subsequently integrated and published in the release recorded above.

The start page has one primary action. Kaart, Ontdek and Hulp replace the expanded toolbar. The live north-up map follows player position and camera heading; opening and closing it preserves both. Escape releases the mouse without leaving the world. Address search uses the physical keyboard and ranks an exact house number before partial matches.

Validation: 5 GIS tests, 1,089 standalone coordinate checks, 20 Unity EditMode tests and 326 native journey checks passed. WebGL release compilation and the packed-raster exclusion check passed. Chrome checks covered the actual world, walking and looking, map consultation, keyboard menus, address entry with spaces, help, full screen, a 1024×768 window, a 390×844 touch start page, Dutch loading errors and recovery after a rejected pointer-lock request. No JavaScript application errors occurred. Rendered screenshots were inspected; temporary evidence is in `/tmp/toenland-ux-*.png` and `/tmp/toenland-ux-qa.json`.

## Previous-release validation — 12 September 2026

- Unity WebGL release build succeeded; packed-asset check excludes the cached Ferraris raster. Unity EditMode suite passes all 13 tests, including modal focus order and repeated activation after a panel redraw.
- Chrome at 1440×1100: branded start screen → user-initiated loading → live Ferraris map → map click → rendered church/landscape → WASD movement → I/object selection → Dutch church summary → Escape close → Escape return to map. Screenshots confirm actual rendered states; the visible position changed by approximately 2.6 m during a one-second W press. No application errors in the successful flow.
- Extended mouse/keyboard browser journey: address typing with spaces and all shortcut letters, keyboard result selection, entering the chosen location, movement and pointer lock release, forward/reverse focus, outside-panel clicks, five story stops through the ending and source pages, volume/mute and repeated focused activation, sound locations, place-name sources/exploration, church details and legend pagination, return to map and F2/full-screen. Rendered screenshots were inspected; no browser application errors. Evidence: `/tmp/toenland-controls/` and `console.json`.
- Native desktop regression: all 223 journey checks pass, including address typing while the I key is held.
- Help and source disclosures show their content. Full-screen control enters full screen. Blocking the Unity loader produces a Dutch error and an enabled **Opnieuw laden** action.
- Touch viewport 390×844: no horizontal overflow; desktop-play guidance is visible; the unsupported mouse/keyboard launch button is hidden; instructions and sources remain available.
- The embedded Codex browser loaded the map but its automation connection timed out on the 3D transition. Validation continued in installed Chrome via the existing Playwright runtime. This is not a claim of compatibility with every embedded browser.
- Local screenshots `/tmp/toenland-start.png`, `/tmp/toenland-map.png`, `/tmp/toenland-world.png`, `/tmp/toenland-object.png`, `/tmp/toenland-return.png`, `/tmp/toenland-mobile-viewport.png`, and `/tmp/toenland-load-error.png`; console capture `/tmp/toenland-console.json`. The mobile viewport capture preserves touch-media emulation; full-page capture temporarily changed the pointer-media rendering. Temporary test scripts are kept outside the repository.

The full experience requires a desktop browser with WebGL 2, a mouse and keyboard. Mobile touch navigation, immersive browser VR, Safari/Firefox testing and physical-headset validation of this web edition are not included. Deployment readiness is verified through Vercel CLI; browser interaction testing is performed locally on the exact compiled files.
