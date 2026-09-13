# #7 — Alle objecten en landschapselementen onderzoeken

## Gebruik

“Onderzoek objecten” of **I** activeert onderzoeken. Op de kaart blijft slepen/zoomen werken en opent een klik type-informatie in plaats van meteen de wereld binnen te gaan. In de wereld werkt klikken met de muis; op Quest werken de rechter richtstraal en trekker. Een gouden ring markeert het gekozen element. Lucht heeft geen grondring.

Het paneel begint met de Nederlandse naam en **Kort uitgelegd** (maximaal twee zinnen). **Meer weten** opent een lijst met onderdelen; elk onderdeel opent apart. **Samenvatting**, **Kies ander object**, **Plaatsnamen** en **Sluiten** houden de bediening expliciet. Sluiten beëindigt onderzoeken en geeft normaal wandelen terug. De volledige legenda-inventaris is vanuit de verdieping in 25 pagina's door te bladeren.

## Volledige legendecontrole

De [officiële NGI Topoferrarisviewer](https://topoferraris.ngi.be/) bevat 150 unieke symboolafbeeldingsreferenties in zijn 1777-legende. `pipeline/fetch_legend_inventory.py` leest de bijbehorende labels en koppelt de categorieën aan daadwerkelijke modeltypen. De 143 Nederlandse referenties zijn aangevuld met zeven extra, in het Franse deel aanwezige referenties (zachte helling, rotsen, eiland, getij, zandstrand, landduinen en kustduinen), vertaald naar het Nederlands. Afkortingen hebben een Nederlandse verklaring.

`Assets/Resources/Discovery/legend.json` bevat alle 150 IDs, labels, bron-URLs, typekoppelingen en expliciete status. De afbeeldingen zelf worden niet herverdeeld. Niet gemodelleerd betekent **niet** dat het verschijnsel afwezig was op de historische kaart; aanwezigheid is daarvoor niet vastgesteld.

## Dekking van de echte scène

| Render-/gegevenssoort | Informatie en koppeling | Selectie |
| --- | --- | --- |
| 52 gewone gebouwen | Gebouwen, `leg1777-1_61`; functie onbekend. Geen verdeling in huizen, schuren en hoeves zonder bronbewijs. | Gecontroleerde kaartcontouren en 3D-meshcolliders voor muren/daken; binnenplaatsen blijven open. Deur- en raamdetails horen bij het gebouw. |
| Kerk | Kerk, `leg1777-2_38`; lokale erfgoedbron, expliciete latere verbouwingen. | Kaartvoetafdruk en model-envelop inclusief toren. |
| 13 wegen | Onverharde weg, `leg1777-1_18`; subtypen niet apart bewezen. | Segmentbreedtes op kaart en wereldgrond. |
| 4 kale + 4 begroeide akkers | Open akkerland, `leg1777-1_13`; gedeelde uitleg, geen bewezen gewas. | Alle driehoeken van de percelen. Halmen horen bij de akker. |
| 4 graslanden | Weiland, `leg1777-1_20`; geen bewezen vee- of maaibeheer. | Alle perceelvlakken en hun begroeiing. |
| 8 boomgaarden | Boomgaard, `leg1777-1_9`. | Alle perceelvlakken op kaart en wereldgrond. |
| 259 bomen | Vrijstaande boom als categorieverwijzing, `leg1777-1_43`; individuele plaatsing illustratief. | Elke stam en kroon in de wereld; kroon-envelop gebruikt de werkelijke nabije/verre meshes en plaatsingsmatrices. Op de kaart wordt de nagekeken boomgaardcategorie gebruikt, niet een fictief individueel boomregister. |
| Runderen en schapen | Eigen dieruitleg zonder verzonnen individueel legendesymbool. | Elke geplaatste 3D-dier-envelop. |
| Ongeclassificeerde grond, losse grasplukjes, reliëf, verre grond | Onbekend landgebruik, moderne DHMV II-hoogte en illustratieve verlenging. | Grondraycast, ook op de verre achtergrond. Geen kaartcategorie erbij verzonnen. |
| Lucht, licht, wolken en horizon | Illustratieve sfeer, geen historische weerswaarneming. | Een wereldstraal die geen oppervlak raakt. |

Sinds de kaartcorrectie zijn dit 12 aanwezige typekeys met 11 gedeelde inhoudssets. De uitleg voor oude huis/schuur/hoevevarianten, tonnen, houtstapels en hekken blijft beschikbaar in de inhoudsbundel, maar deze varianten en erfvoorwerpen worden momenteel niet in de kaartgetrouwe scène geplaatst. Water, bospercelen, hagen, industrie, molens, grenzen enzovoort staan in de volledige legenda-inventaris, maar zijn niet als eigen geverifieerde 3D-typen aanwezig. Decoratieve dieren bestaan uitsluitend in de wereld; er worden geen fictieve symbolen voor toegevoegd aan de historische kaart.

## Selectie en kosten

Gewone gebouwen gebruiken hun echte muur-/dakmesh als collider. Terrein gebruikt eveneens exacte meshhits; kerk, bomen en dieren gebruiken semantische selectievakken. Dat maakt instanced bomen en samengevoegde geveldetails selecteerbaar zonder honderden fysieke colliders of nieuwe draw calls. De vakken zijn een selectiemarge, geen precieze botanische of bouwkundige meting; open plekken binnen een kroon kunnen eveneens het omvattende object selecteren. Per klik wordt het dichtstbijzijnde geraakte type gekozen. Kaartselectie gebruikt de oorspronkelijke bouwvoetafdrukken, wegbreedtes en perceeldriehoeken.

## Historische duiding

Tekst is in eigen woorden geschreven vanuit algemene historische kennis. Locatiespecifieke uitspraken worden onderscheiden van gebruikslogica, kaartinterpretatie en illustratie. Onbekende namen, bewoners, gewassen, materialen en precieze functies worden niet aangevuld met verzonnen feiten. Bronnen staan per type onder **Hoe weten we dit?**; de kerk verwijst naar Onroerend Erfgoed en plaatsnamen naar #6. De CAG-werktuigencollectie levert algemene context, geen bewijs voor gebruik op één erf in 1775.

## Validatie — 12 september 2026

De nieuwe inhoudstest faalde eerst op de ontbrekende implementatie (`artifacts/issue-7-red.xml`). Na implementatie slagen alle 11 Unity EditMode-tests, 4 GIS-tests en 1.089 coördinaatrondgangen (maximale fout 0 m). De desktop- en Quest/Android-build slagen beide.

De echte desktopspeler doorloopt 222 geslaagde controles voor de geïntegreerde issues #2–#7. Daarbij worden alle 17 typen, herhaalde exemplaren, alle verdiepingsonderdelen, alle 25 legendepagina's, paneelafsluiting en terugkeer naar de wereld gecontroleerd. Dezelfde world-space canvasconfiguratie en straalbediening als op Quest worden in de speler getest, inclusief selecteren, verdieping en sluiten. Inhoud en adressen komen uit gebundelde resources; er zijn geen runtime-netwerkverzoeken.

Lokale bewijzen: `artifacts/issues-2-7-validation.json`, `artifacts/player-smoke.log`, `artifacts/unity-tests.xml`, `artifacts/unity-quest.log`, `artifacts/13-object-detail.png`, `artifacts/14-world-space-ui.png` en `artifacts/issues-2-7-journey.webm`. De opname is een stille visuele opname met 2 beelden per seconde, geen prestatiemeting.

De geïntegreerde versie is nog niet op de fysieke Quest gevalideerd: tijdens afronding wordt de headset niet meer door ADB gevonden. Werkelijke leesbaarheid, controllerbediening, geluidsbeleving en framerate op de headset blijven een expliciete acceptatiestap. De APK staat gereed in `builds/Winksele1775.apk`.
