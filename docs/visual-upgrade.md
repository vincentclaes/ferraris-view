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

The current aligned-world Android APK was rebuilt on 13 September: 79,192,963 bytes, with verified signature, current offline JSON content and no stale numbered build copies. See [Quest package receipt](quest-validation.md) for the SHA-256 and checks. No device was attached, so installation and hardware validation remain pending.

The earlier Quest 72–73 FPS sample belongs to the simpler scene. This pass needs its own on-device performance and visual check. The scene now contains richer geometry and surfaces, while the livestock and rural architecture remain procedural approximations; it should not be described as finished AAA or film-quality graphics.
