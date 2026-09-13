# Graphics and movement upgrade — September 2026

## Movement

The Quest left stick preserves analog magnitude and applies a gentle nonlinear response. Full tilt reaches 6 m/s (previous maximum 1.8 m/s); partial pushes retain precise slow movement. After the Input System deadzone, 25%, 50% and 100% inputs yield 0.75, 2.12 and 6 m/s. Diagonal input cannot exceed the same maximum. A Unity test covers these cases.

## Visible changes

- The detailed Maria-Hemelvaartkerk from the task “Maak 3D-model van Maria-Hemelvaart” replaces the church placeholder. Scanned stone, slate, doors, modeled window openings, tower details and three LODs are included. It is scaled to the traced Ferraris footprint. The separately modeled sacristy and its collider are hidden because the asset documentation dates that addition to 1786. Other present-day details remain interpreted rather than proven for 1775.
- Ordinary buildings now follow reviewed polygon contours with low masonry walls, small timber-framed openings and pitched roofs. The previous house/barn/farmhouse variants and their yard props are no longer assigned to map symbols. See [map-aligned building forms](period-buildings.md) for the current source boundary and validation.
- Roads use scanned dry soil with distinct worn wheel tracks. Their placement continues to follow the reviewed Ferraris alignments.
- The terrain uses scanned grass/soil detail and normal maps over the real DHMV elevations. A visual horizon extends beyond the playable kilometre; it does not extend the movement boundary or claim additional mapped historical detail.
- A photographic HDR sky, warm directional light and short-range real-time shadows replace the flat backdrop.
- Orchard trees have branching trunks, alpha-cutout photographed leaves, gentle wind and reduced distant crowns. Trees remain GPU-instanced; grass uses a scanned clump and spatially culled batches. Grain ears populate mapped crop patches.
- Small groups of grazing sheep and cattle populate pasture polygons. Their placement and appearance are illustrative, not evidence of the exact livestock present in 1775.

- Five [daily-life tableaux](tableaux.md) add nine animated, stylised residents and working props, selectable Dutch explanations and direct visits from the story panel. They remain illustrative, not photorealistic or verified local period characters.

## Sources and reproducibility

Powered by [Poly Haven](https://polyhaven.com). The scanned materials, grass mesh, foliage images and sky are supplied under [CC0](https://polyhaven.com/license). `data/visual-sources.json` records source URLs, original asset names, sizes and MD5 checksums; `pipeline/fetch_visual_assets.py` verifies or restores the downloaded files. The material set includes Leafy Grass, Brown Mud Dry, Medieval Red Brick, Worn Plaster Wall, Wood Planks Grey, Grey Roof Tiles, Tree Bark 03, Tree Small 02 leaf textures, Grass Medium 02 and Kloppenheim 06 Pure Sky.

The church's imported Unity models/materials are in `Assets/WinkseleChurch`; its runtime prefab is `Resources/Visuals/Church.prefab`. The original authoring files under `models/maria-hemelvaartkerk` are unchanged. The imported asset's README and texture manifest retain its architectural sources and limitations.

Unlike the Ferraris raster, these CC0 art assets may be versioned with the application. Individual imported texture files remain at source resolution; Android overrides limit most materials to 1024px ASTC and the church stone to 2048px. The HDR sky uses a 2048px Android override.

## Validation and limits

Three Unity tests pass, including analog speed and world construction with the church LOD and animals. Desktop imports/builds and the real built-player map-to-world journey pass. Rendered street, landmark, pasture and top-down views are captured in `artifacts/03-world.png`, `06-church.png`, `07-pasture.png` and `04-world-topdown.png`. Visual inspection caught and corrected inward-facing roof triangles, incorrect facade UVs and unsuitable whole-atlas leaf sampling. The final scanned grass instance uses 1,542 vertices instead of importing all five variants together (7,031 vertices).

The latest completed Android package and its exact build/hash are recorded in the [Quest package receipt](quest-validation.md). No device was attached for the enhanced-world builds, so installation and hardware validation remain pending.

The earlier Quest 72–73 FPS sample belongs to the simpler scene. This pass needs its own on-device performance and visual check. The scene now contains richer geometry and surfaces, while the livestock and rural architecture remain procedural approximations; it should not be described as finished AAA or film-quality graphics.

## Terrain roads — 13 September 2026

The former 1,442 separate five-band road segments restarted texture coordinates, left joins at bends and sat 13 cm above terrain. Roads now blend scanned earth and illustrative wheel wear directly into the terrain shader. The export creates north-up `roads.png` from exact point-to-segment distances on the same 13 road centre lines and their existing widths. Its red channel is coverage with a 35 cm edge transition; green is wheel wear. A 2048² linear mask has approximately 49 cm pixels over this kilometre, with ASTC 4×4 on Quest. This changes appearance, not vector coordinates, selection logic or the approximate historical-width claim. Wear patterns are illustrative.

This removes 14,420 overlaid road triangles and their separate renderer. It adds one road-mask texture lookup to the terrain material; no Quest frame-rate improvement is claimed without hardware measurement. Build reports must include `roads.png`, and the website still excludes the cached Ferraris raster.

A road-edge inspection also exposed a gap between the 257-sample terrain border and the independently sampled distant grid. Four expanding horizon rings now share every terrain edge sample. The horizon remains illustrative and does not extend the playable crop or claim extra historical coverage. Unity ray tests compare both sides of 1,020 boundary positions. Run the built player with `-ferraris-smoke -road-study -evidence-dir /tmp/road-study` for the same three road views at walking height and overhead.

Validation: 6 GIS tests, 1,089 standalone C# coordinate checks and 19 Unity tests pass. The GIS test checks widths, turns, crossings, orientation, exact regenerated pixels and every exported centreline point. Native visual evidence is in `/tmp/ferraris-terrain-roads/`. Final build and publication receipts are recorded in [web](web.md) and [Quest](quest-validation.md).

The final desktop and WebGL journeys pass (340 built-player checks; Chrome road selection/walking/map return plus all story stops). Native before/after inspection confirms the northern horizon gap is closed. Production `dpl_FrABjrYmP2Uyqv35aCB7Pqv9NhCp` is READY at https://land-van-weleer.vercel.app; the signed Quest APK passes its package gate, but no headset is attached. [PR #23](https://github.com/vincentclaes/ferraris-view/pull/23), including the scene and building stack, was merged into the default branch `codex/winksele-vr` at `e8bdb1b` on 13 September. A desktop build was rejected because its cached asset report omitted the mask; recreating the generated macOS player caches restored the packed-asset proof. Desktop and WebGL builds now recreate their affected generated platform caches, as Android already does for staging and compiled output. Source files are retained.

## Field vegetation — 13 September 2026

Grain has varied heights and lean, two bent leaves per stem, tapered stems and smaller seed heads. The existing instanced foliage shader adds height-weighted wind, keeping roots fixed. Seed-row shading fades out at subpixel size to limit distant shimmer. Sixteen stems remain in each clump, with the same 65 m chunk culling range; the mesh changes from 736 to 928 triangles per clump. This is illustrative vegetation, not evidence of the exact crop or cultivar grown in each Ferraris parcel.

Plant jitter now spans the full placement cell. Vegetation and livestock clearance use the existing point-to-segment distance helper, replacing square exclusions around road vertices. Road vectors and widths remain unchanged; the clearance is never narrower than half the road width. A regression test covers sparse and diagonal lines, the second segment, rounded endpoints, wide roads and repeated vertices.

Run `bash scripts/smoke-desktop.sh -field-study` after a desktop build to capture walking-height and close views in all four crop patches, twice per viewpoint to inspect wind. This study is excluded from Android and WebGL. The inspected final views are in `/tmp/ferraris-fields-final/`; the mesh contains 1,088 vertices and 928 triangles. The close-up check led to slimmer ears with seed rows. The art remains stylised, and Quest frame time is still unmeasured.

All 32 Unity EditMode tests pass (3.22 seconds), along with 6 GIS tests and 1,089 coordinate checks. A cheap segment-bounds rejection reduced the suite from 14.07 seconds with unfiltered distance calculations; this is a desktop test timing, not a Quest performance measurement.

The full desktop journey passes all 356 checks. The signed ARM64 Quest package built from `ac7c2fc` passes offline-content, road-mask and all-19-voice gates; see the [current receipt](quest-validation.md). APK metadata confirms both desktop-only studies are excluded. No headset is connected.
