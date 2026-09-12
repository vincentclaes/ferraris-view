# Toenland — browser edition

Working brand: **Toenland**, with **Wandel door het Vlaanderen van toen.** The name joins time and landscape in accessible Dutch. No custom domain has been purchased and no trademark clearance is claimed.

Live: **https://toenland.vercel.app**. Deployment `dpl_5xS1uGhJUbZSXmtoeUXwvRU9CUqs` was marked `READY` by Vercel on 12 September 2026. Vercel assigned the first default deployment to **production** automatically; later default deployments use preview. The deployed application corresponds to commit `76c64dc`; subsequent documentation changes record this receipt.

## Build and hosting

`bash scripts/unity.sh web` uses Unity 6000.6.0f1 WebGL Build Support, IL2CPP/WebAssembly, WebGL 2 and the custom `Assets/WebGLTemplates/Toenland` template. It is a release build of the existing runtime, not a rewritten approximation. Desktop mouse/keyboard controls and all discovery content are shared with the native app. WebXR is not added; immersive Quest remains the Android build.

Church textures are capped at 2048 px and other landscape textures at 1024 px on WebGL (sky stays 2048). Native texture settings remain unchanged. This reduces the complete gzip deployment from approximately 117 to 68 MiB. The canvas renders at 1440×1000 with a preserved aspect ratio, including full-screen mode, so the existing controls fit reliably.

`web/vercel.json` sets the required gzip encoding and MIME types for `.wasm.gz`, `.js.gz` and `.data.gz`. The local standard-library server uses equivalent headers. `bash scripts/deploy-web.sh` deploys `builds/web` as a preview to Vercel project `toenland`, scope `vincentclaes-projects`. It does not upload the repository, original models, APKs or local evidence. No Vercel build process or application backend is needed.

The cover is rendered directly from the project with `Ferraris.Editor.BuildProject.WebCover`; it contains no map raster or gameplay overlays. `web/cover.jpg` is copied into the output during a web build.

## Map consultation

The [official historical-cartography service](https://www.vlaanderen.be/datavindplaats/catalogus/raadpleegdienst-voor-historische-cartografie) permits public access. The web runtime requests the current area's 2048×2048 Ferraris crop from that service using EPSG:31370 and invariant-culture bounds. CORS access and the actual image response were verified on 12 September 2026. Attribution and direct KBR/service links appear in the website and the experience.

The native cached raster is temporarily moved outside Resources while building, restored in `finally`, and checked against the packed-asset report. The website therefore does not redistribute that raster from Vercel. Unlike the native edition, initial map loading needs internet. A failed map request gives a Dutch reload instruction. Bundled object explanations, addresses, stories and sounds remain local after the initial application download.

## Validation — 12 September 2026

- Unity WebGL release build succeeded; packed-asset check excludes the cached Ferraris raster. Existing Unity EditMode suite passes all 11 tests.
- Chrome at 1440×1100: branded start screen → user-initiated loading → live Ferraris map → map click → rendered church/landscape → WASD movement → I/object selection → Dutch church summary → Escape close → Escape return to map. Screenshots confirm actual rendered states; the visible position changed by approximately 2.6 m during a one-second W press. No application errors in the successful flow.
- Help and source disclosures show their content. Full-screen control enters full screen. Blocking the Unity loader produces a Dutch error and an enabled **Opnieuw laden** action.
- Touch viewport 390×844: no horizontal overflow; desktop-play guidance is visible; the unsupported mouse/keyboard launch button is hidden; instructions and sources remain available.
- The embedded Codex browser loaded the map but its automation connection timed out on the 3D transition. Validation continued in installed Chrome via the existing Playwright runtime. This is not a claim of compatibility with every embedded browser.
- Local screenshots `/tmp/toenland-start.png`, `/tmp/toenland-map.png`, `/tmp/toenland-world.png`, `/tmp/toenland-object.png`, `/tmp/toenland-return.png`, `/tmp/toenland-mobile-viewport.png`, and `/tmp/toenland-load-error.png`; console capture `/tmp/toenland-console.json`. The mobile viewport capture preserves touch-media emulation; full-page capture temporarily changed the pointer-media rendering. Temporary test scripts are kept outside the repository.

The full experience requires a desktop browser with WebGL 2, a mouse and keyboard. Mobile touch navigation, immersive browser VR, Safari/Firefox testing and physical-headset validation of this web edition are not included. Preview readiness is verified through Vercel CLI; browser interaction testing is performed locally on the exact compiled files.
