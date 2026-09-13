# Land van Weleer — browser edition

Brand: **Land van Weleer**, with **Wandel door het Vlaanderen van toen.**

Website: **https://land-van-weleer.vercel.app**. The existing Vercel project (`prj_Fc3TN3FPfmYxSPhqD3H72RrWEOUQ`) was renamed to `land-van-weleer` on 13 September 2026. Production deployment `dpl_3gfJAFE9e4gh3MMfgbYVh2AzMbrr` is `READY`, with `land-van-weleer.vercel.app` attached to the project and verified on this deployment. It contains the brand, browser title, favicon, mobile guidance and Unity template from `d132f0e`, merged through PR #17 as `2085456`. The former public address remains an alias to the same release. Production publication uses `--prod`; default deployments use preview.

The preceding map-alignment release was `dpl_BoUev3QQ3DXAm12yPG3NyVDTC9Zv`, containing code commit `a275a9e` merged through PR #16 as `a630661`. Its locally tested WebGL hashes remain in `artifacts/building-review/web-release-hashes.json`.

## Build and hosting

`bash scripts/unity.sh web` uses Unity 6000.6.0f1 WebGL Build Support, IL2CPP/WebAssembly, WebGL 2 and the custom `Assets/WebGLTemplates/LandVanWeleer` template. It is a release build of the existing runtime, not a rewritten approximation. Desktop mouse/keyboard controls and all discovery content are shared with the native app. WebXR is not added; immersive Quest remains the Android build.

Church textures are capped at 2048 px and other landscape textures at 1024 px on WebGL (sky stays 2048). Native texture settings remain unchanged. This reduces the complete gzip deployment from approximately 117 to 68 MiB. The canvas renders at 1440×1000 with a preserved aspect ratio, including full-screen mode, so the existing controls fit reliably.

`web/vercel.json` sets the required gzip encoding and MIME types for `.wasm.gz`, `.js.gz` and `.data.gz`. The local standard-library server uses equivalent headers. `bash scripts/deploy-web.sh` deploys `builds/web` as a preview to Vercel project `land-van-weleer`, scope `vincentclaes-projects`. It does not upload the repository, original models, APKs or local evidence. A generated `.vercelignore` excludes numbered sync copies of old pages and bundles; the final rename deployment contained eight input files. No Vercel build process or application backend is needed.

The cover is rendered directly from the project with `Ferraris.Editor.BuildProject.WebCover`; it contains no map raster or gameplay overlays. `web/cover.jpg` is copied into the output during a web build.

## Mouse and keyboard

All visitor-panel buttons accept mouse clicks and Tab/Shift+Tab navigation, with a gold focus outline and Enter/Space activation. Arrow keys also move between focused buttons. Tab releases pointer lock; clicking the landscape resumes mouse look. Panels block background movement and pointer recapture. Escape closes the current panel.

H opens address search, J the day story, L sound, N place names, I inspection and M the map. Address entry owns letter keys and spaces, so typing cannot trigger another feature. Click the address row to resume typing after navigating buttons. Map arrows pan, +/− zoom, R resets, V toggles vectors and Enter enters the centre point. WASD walks, Shift speeds up and the mouse looks around. F2 leaves the canvas for the website full-screen control; normal browser Tab then reaches help and sources.

## Map consultation

The [official historical-cartography service](https://www.vlaanderen.be/datavindplaats/catalogus/raadpleegdienst-voor-historische-cartografie) permits public access. The web runtime requests the current area's 2048×2048 Ferraris crop from that service using EPSG:31370 and invariant-culture bounds. CORS access and the actual image response were verified on 12 September 2026. Attribution and direct KBR/service links appear in the website and the experience.

The native cached raster is temporarily moved outside Resources while building, restored in `finally`, and checked against the packed-asset report. The website therefore does not redistribute that raster from Vercel. Unlike the native edition, initial map loading needs internet. A failed map request gives a Dutch reload instruction. Bundled object explanations, addresses, stories and sounds remain local after the initial application download.

## Name change validation — 13 September 2026

The renamed Unity WebGL release built successfully. Chrome/Playwright at 1440×1100 and a 390×844 touch viewport verified the exact browser title, header/footer, mobile guidance and absence of the former brand in the generated page. Help navigation, map startup and world entry/return passed without JavaScript errors. Desktop and mobile screenshots were inspected; no horizontal overflow. The Browser plugin was unavailable, so the installed Playwright/Chrome runtime was used. Temporary evidence: `/tmp/land-van-weleer-desktop.png`, `/tmp/land-van-weleer-mobile.png`, `/tmp/land-van-weleer-map.png`.

## Building review — 13 September 2026

The website build now contains 53 reviewed building polygons and source-supported categories, including the corrected church anchor. Ordinary building selection and collision preserve concave courtyards. V / **Toon lijnen** displays readable yellow building outlines for comparison with the raster. [Review and validation](period-buildings.md) records the scope and uncertainty.

The full Chrome mouse/keyboard journey passed on this update. A final build check confirmed visible outlines and the Dutch unknown-function explanation, with no JavaScript application errors. GIS tests: 5; standalone coordinate checks: 1,089; Unity EditMode: 16; desktop journey: 321. Earlier validation below remains historical context.

## Validation — 12 September 2026

- Unity WebGL release build succeeded; packed-asset check excludes the cached Ferraris raster. Unity EditMode suite passes all 13 tests, including modal focus order and repeated activation after a panel redraw.
- Chrome at 1440×1100: branded start screen → user-initiated loading → live Ferraris map → map click → rendered church/landscape → WASD movement → I/object selection → Dutch church summary → Escape close → Escape return to map. Screenshots confirm actual rendered states; the visible position changed by approximately 2.6 m during a one-second W press. No application errors in the successful flow.
- Extended mouse/keyboard browser journey: address typing with spaces and all shortcut letters, keyboard result selection, entering the chosen location, movement and pointer lock release, forward/reverse focus, outside-panel clicks, five story stops through the ending and source pages, volume/mute and repeated focused activation, sound locations, place-name sources/exploration, church details and legend pagination, return to map and F2/full-screen. Rendered screenshots were inspected; no browser application errors. Evidence: `/tmp/toenland-controls/` and `console.json`.
- Native desktop regression: all 223 journey checks pass, including address typing while the I key is held.
- Help and source disclosures show their content. Full-screen control enters full screen. Blocking the Unity loader produces a Dutch error and an enabled **Opnieuw laden** action.
- Touch viewport 390×844: no horizontal overflow; desktop-play guidance is visible; the unsupported mouse/keyboard launch button is hidden; instructions and sources remain available.
- The embedded Codex browser loaded the map but its automation connection timed out on the 3D transition. Validation continued in installed Chrome via the existing Playwright runtime. This is not a claim of compatibility with every embedded browser.
- Local screenshots `/tmp/toenland-start.png`, `/tmp/toenland-map.png`, `/tmp/toenland-world.png`, `/tmp/toenland-object.png`, `/tmp/toenland-return.png`, `/tmp/toenland-mobile-viewport.png`, and `/tmp/toenland-load-error.png`; console capture `/tmp/toenland-console.json`. The mobile viewport capture preserves touch-media emulation; full-page capture temporarily changed the pointer-media rendering. Temporary test scripts are kept outside the repository.

The full experience requires a desktop browser with WebGL 2, a mouse and keyboard. Mobile touch navigation, immersive browser VR, Safari/Firefox testing and physical-headset validation of this web edition are not included. Deployment readiness is verified through Vercel CLI; browser interaction testing is performed locally on the exact compiled files.
