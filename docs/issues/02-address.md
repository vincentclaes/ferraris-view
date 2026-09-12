# #2 — Dichtstbijzijnde huidige adres

De app bundelt een momentopname van publieke adrespunten, zonder bewonersgegevens, uit het Adressenregister van Digitaal Vlaanderen. De bron bevat straatnaam, huisnummer, gemeente en adrespositie. Alleen officieel toegekende adressen met status `InGebruik` zijn opgenomen; busnummers worden samengevoegd. De selectie omvat een ruime buffer rond het speelgebied zodat de rand geen fout dichtstbijzijnd adres oplevert.

Vernieuwen: `.venv/bin/python pipeline/fetch_addresses.py`. De ophaaldatum staat in de data en in de app. De lokale berekening heeft geen netwerk nodig, blokkeert niet op een server en herberekent maximaal viermaal per seconde. Na een verplaatsing verschijnt binnen 250 ms het opnieuw berekende adres; buiten het speelgebied wordt het vorige resultaat verwijderd.

## Hoe weten we dit?

- [Adressenregister, officiële dataset en licentie](https://www.vlaanderen.be/datavindplaats/catalogus/adressenregister-adressen).
- [OGC API van Digitaal Vlaanderen](https://geo.api.vlaanderen.be/Adressenregister/ogc/features/v1/collections/Adres).
- [Modellicentie gratis hergebruik](https://www.vlaanderen.be/digitaal-vlaanderen/onze-diensten-en-platformen/open-data/voorwaarden-voor-het-hergebruik-van-overheidsinformatie/modellicentie-gratis-hergebruik): bronvermelding Digitaal Vlaanderen, datum in de momentopname.

Het getoonde adres is een hedendaags referentiepunt. De afstand is hemelsbreed vanaf de bezoeker tot het adrespunt, niet tot een perceelgrens. Het toont geen bewezen overeenkomst met een gebouw uit 1775. De Ferrariskaart heeft historische meet- en georeferentiefouten. De momentopname is geen live gemeentelijke registratie.

## Verificatie

Eerste test faalde op ontbrekende adresfunctie. Daarna 5/5 Unity EditMode-tests geslaagd. Desktopbuild en echte map → lopen → kaart-smoke geslaagd; `artifacts/03-world.png` toont Dalenstraat 6 en de afstand bij de geselecteerde plek. De gedeelde canvas heeft een world-space Quest-weergave; fysieke leesbaarheid en prestaties van de nieuwe build zijn nog niet gemeten doordat ADB `unauthorized` meldt.
