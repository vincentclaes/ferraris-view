# Rural building forms and map evidence

## Regional reference

The [1762 boerenburgerhuis at Dalenstraat 2, Winksele](https://inventaris.onroerenderfgoed.be/erfgoedobjecten/41898) provides a local pre-1775 analogue: brick and sandstone, one-and-a-half storeys, rectangular openings and shouldered side gables. Its recorded later plinth/window alterations are not treated as original evidence. The [1661 house at Dalenstraat 4](https://inventaris.onroerenderfgoed.be/erfgoedobjecten/41897) provides a second local masonry reference. These are analogues, not identifications of every Ferraris footprint.

The rural renderer now preserves the traced rectangle when aligning its roof ridge with the long side. Low plastered buildings, taller masonry forms and agricultural doors have different proportions. The existing roof scan is tinted warm brown; it is an illustrative surface, not a verified eighteenth-century tile reconstruction. The church asset is unchanged. Further fidelity work remains necessary.

## Legend constraint

The user-supplied [De Coene et al., Ferraris, the legend (2012)](https://backoffice.biblio.ugent.be/download/2116980/6770945), PDF pages 8–9 and legend page 14, distinguishes ordinary buildings from churches, chapels and other dedicated symbols. Additional written labels can specify a function. A plain building symbol alone does not justify assigning house, barn or farmhouse. Parish numbers are administrative annotations, not buildings.

The previous export cycles ordinary buildings through three functions. That is not evidence and is being replaced. The 57 legacy rectangles are approximate selections, not exact contours; they still require a complete raster review. Passing geometry tests proves that rendering preserves the supplied geometry, not that the supplied geometry is faithful to the map.

## Verification

Three new Unity geometry cases cover both long-axis orientations, rotated footprint/collider preservation, upward-facing roof slopes, and a pick envelope that includes the roof. Together with the existing suite: 16 tests passed on 13 September 2026. Four Python GIS tests and 1,089 C# coordinate round trips also pass. Desktop/browser visual validation of the new forms is pending the map review. This work has not been deployed.
