# Data provenance and coordinates

The primary historical source is the KBR Ferraris Cabinet Map. The official NGI/KBR sheet lookup returns **93, Cortenberghe**, for Winksele: [KBR sheet](https://uurl.kbr.be/1028485), [assembleertabel](https://common.ngi.be/FerrarisKBR/index.jsp?l=nl), [KBR project](https://www.kbr.be/nl/projecten/kaart-van-ferraris/). The lookup was verified using `POST https://common.ngi.be/FerrarisKBR/post`, body `place=Winksele`.

## Ferraris raster

- Provider: Digitaal Vlaanderen, scanned KBR exemplar; WMS layer `ferraris`.
- Service: <https://geo.api.vlaanderen.be/histcart/wms>.
- [Official catalogue](https://www.vlaanderen.be/datavindplaats/catalogus/raadpleegdienst-voor-historische-cartografie).
- [Dataset metadata](https://metadata.vlaanderen.be/srv/dut/catalog.search#/metadata/2d7382ea-d25c-4fe5-9196-b7ebf2dbe352).
- The capabilities describe **scanned and georeferenced** map sheets, approximately 1:11,520. This is an existing georeferenced mosaic, not a raw unreferenced KBR image.
- Request CRS explicitly `EPSG:31370`; WMS version 1.1.1; 2048×2048 pixels for 1×1km (0.4883m requested pixel spacing; original map detail is not equivalent to modern survey accuracy).
- The pipeline saves a GeoTIFF with the requested bounds/CRS and a PNG for Unity. No additional hand georeferencing was applied.
- **License:** public access has no restrictions, but the retrieved metadata states that the data are copyright protected and that reuse requires contacting the information owner. An open redistribution license is **not** assumed. Downloaded maps and packed binaries remain local and excluded from Git. Check reuse rights before distributing maps or builds containing them.

## Terrain

- Provider: Digitaal Vlaanderen, **DHMV II DTM 1m**, acquired 2013–2015.
- Service: <https://geo.api.vlaanderen.be/DHMV/wcs>, coverage `DHMVII_DTM_1m`.
- [Official dataset catalogue and license](https://data.gov.be/en/datasets/9be5b169-b62f-4076-a31a-2252f26dacf8).
- License: Open Data License Flanders. Attribution: Digitaal Vlaanderen, Digitaal Hoogtemodel Vlaanderen II.
- Horizontal CRS verified from downloaded GeoTIFF: `EPSG:31370`. Vertical values: metres TAW.
- 257×257 samples over the 1km crop. WCS request extends by half a cell so raster pixel centres coincide with terrain vertices, including both edges.
- Actual crop min/max: approximately 29.50–61.71m TAW. Unity Y = TAW minus `heightBase`; no artificial vertical exaggeration.
- This modern bare-earth terrain provides approximate relief. Modern earthworks may remain; it is not a reconstruction of 1775 topography.

Exact request URLs, bounds and SHA-256 checksums are generated in `data/winksele/sources.json`. Cached raw files live in ignored `data/raw/`.

## Coordinate contract

Origin: WGS84 latitude 50.89795, longitude 4.64385 → Lambert 72 east 169352.76968973473, north 176436.7209948646.

```text
Unity X = Lambert easting − origin easting
Unity Z = Lambert northing − origin northing
Unity Y = DTM elevation − heightBase

map UV (0,0) = southwest = Unity X/Z (-500,-500)
map UV (1,1) = northeast = Unity X/Z (+500,+500)
PNG row 0 = north; exported heights row 0 = south
```

Python uses PyProj with `always_xy=True` (longitude, latitude). C# uses a 17×17 grid sampled by PyProj and bilinear interpolation. Newton inversion uses the same grid for geographic-to-world mapping. This removes a native PROJ dependency from the Android runtime. Automated tests compare 1,000 interpolated points against PROJ with a 1cm tolerance and execute 1,089 round trips using the actual C# runtime file, including boundaries. This is numerical conversion accuracy, **not** historical map accuracy.

## Semantic interpretation

`tracing.json` records pixel-space manual corrections against the north-up 1024px preview and locks the crop origin/size. Roads follow visible historical road centres. Buildings instead use `buildings-reviewed.json`: 53 reviewed polygon symbols (52 ordinary buildings and one church), with stable IDs, evidence, raster hash and rejected legacy detections. Their walls, roofs and selection use the same contours; see [building review](period-buildings.md). House, barn and farmhouse functions are not inferred from a plain red symbol. Fields follow visible agricultural areas. Vegetation polygons represent orchards/enclosures; no dense forest was identified in this crop. `winksele_forest.geojson` retains the category name requested for the pipeline, with `landuse=orchard` to avoid mislabelling the content.

OpenCV red segmentation is run reproducibly and saved as candidates and a mask. The initial run found 885 components, including parish numbers and annotation strokes. These are **not automatically accepted as buildings**. Manual corrections produce the playable MVP data. GeoJSON is exported in WGS84; Unity JSON contains local metres. A generated `alignment.png` overlays roads, building centres and vegetation on Ferraris for visual review.
