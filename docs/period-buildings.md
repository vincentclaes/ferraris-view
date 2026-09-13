# Building contours and historical evidence

## Map review

The [user-supplied Ferraris legend study](https://backoffice.biblio.ugent.be/download/2116980/6770945), PDF pages 8–9, 14 and 16, distinguishes ordinary buildings from religious and other dedicated symbols. A plain red building symbol does not establish house, barn or farmhouse use. Parish numbers are annotations, not buildings.

`data/winksele/buildings-reviewed.json` is the authoritative review: 52 ordinary building symbols and one church. Ten legacy detections on numbers/vegetation were rejected, two rectangles were merged into one connected T-shaped symbol, and seven missing symbols were added. Each record has a stable ID, contour, legend category and evidence note. The file records rejected IDs, source raster hash and review method. `tracing.json` still supplies roads and parcels; its old building rectangles are no longer exported.

The saved contours were checked against the raster, including higher-resolution WMS crops of the central T-shaped symbol and church. Painted brush edges, small symbols and perspective leave interpretation uncertainty: these are reviewed map outlines, not surveyed 1775 cadastral plans. The church outline follows its nave symbol, not the cemetery enclosure.

## Runtime

The export preserves polygons in GeoJSON, Unity data and the numbered `data/winksele/alignment.png` overlay. Ordinary building walls, roof triangles, collision, map selection, land-use lookup and spawn exclusion use those contours. Concave courtyards stay open. Minimum bounding rectangles determine only the illustrative roof axis; they do not replace wall contours. No ordinary building is assigned a house/barn/farmhouse function by its list index.

The church model is centred and oriented on the reviewed symbol, with its visible plan envelope fitted to the symbol bounds. Its detailed footprint and elevation remain illustrative because the source is pictographic. The later sacristy is hidden. This does not establish the entire model as an exact 1775 reconstruction.

## Architectural interpretation

The [1762 building at Dalenstraat 2](https://inventaris.onroerenderfgoed.be/erfgoedobjecten/41898) and [1661 building at Dalenstraat 4](https://inventaris.onroerenderfgoed.be/erfgoedobjecten/41897) provide local pre-1775 masonry analogues. They do not identify the use, facade or material of every map symbol. Ordinary buildings use low masonry walls, small timber-framed openings and pitched roofs; height, roof shape, openings and warm-tinted roof textures are reconstruction choices. Five illustrative [inhabited tableaux](tableaux.md) now accompany the story walk. Unique photorealistic period buildings and characters remain further work.

## Verification

GIS tests compare every reviewed polygon with runtime footprints, roof coverage and projected GeoJSON, reject the false legacy IDs, and check allowed categories. Unity tests select all ordinary building roofs, check the L-shaped courtyard is neither selectable as a building nor blocked by a collider, and retain rural roof-axis regressions. The desktop journey checks spawn safety and selection for all 52 ordinary buildings in addition to the visitor features.

Validated on 13 September 2026: 5 GIS tests, 1,089 standalone C# coordinate round trips, 16 Unity EditMode tests, and 321 built-player journey checks passed. The unattended journey now isolates injected input devices from physical mouse/focus events after two focus-dependent test attempts failed. Actual browser input remains covered separately.

Chrome/Metal completed the full mouse/keyboard journey on the new polygon build (34 captured states), then the final WebGL build was inspected for visible polygon overlays and the Dutch “Gebouw — functie onbekend” explanation. No JavaScript application errors were recorded; Chrome emitted WebGL drawBuffers warnings while still rendering the inspected scenes. Local evidence: `artifacts/journey.json`, `artifacts/player-smoke.log`, `artifacts/unity-tests.xml`, `/tmp/toenland-controls/`, `/tmp/toenland-contours.png`, `/tmp/toenland-building-category.png`. Quest hardware validation is not claimed; no headset was attached during this pass. Deployment receipt: [web edition](web.md).
