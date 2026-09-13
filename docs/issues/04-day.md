# #4 — Een dag met Marie

Voortzetting van het oorspronkelijke tekstverhaal, op de gebouwcontouren van `codex/period-building-forms` (`8011ee9`). Eén zichtbaar, illustratief personage begeleidt de bezoeker langs vijf plekken: erf, akker, boomgaard, kerk en schuur. Marie, haar kleding, dialogen en handelingen zijn verzonnen. De gebouwenkaart bewijst geen specifieke bewoner of gebouwfunctie.

## Spelen

Open **Dag van Marie** of druk **J**. Kies op de kaart **Zoek Marie**, of loop zelf naar haar toe. Binnen drie meter en zonder muur ertussen kun je haar aanklikken, aanwijzen met de controller of via het menu aanspreken. **Ik loop mee** accepteert haar uitnodiging. **Niet nu** laat je vrij verkennen.

Marie vertelt in het Nederlands met een gebundelde Vlaamse computerstem. Elke stop heeft een korte scène, een optionele vraag, het antwoord en een reden om verder te lopen. **Nog eens vertellen** herhaalt de tekst; **Stem: aan/uit** regelt de stem apart van de omgevingsgeluiden. Alle tekst blijft leesbaar zonder geluid of internet.

Marie loopt met 1,5 m/s over een bereikbare route buiten de gebouwcontouren. Bij meer dan zeven meter achterstand wacht ze; binnen 3,5 meter loopt ze verder. Wie vooruitloopt, mag vooruit blijven. Op de bestemming begint de volgende scène pas wanneer de bezoeker dichtbij is. De kijkrichting wordt niet overgenomen.

**Sluiten** sluit het gesprek. **Verhaal verlaten** stopt beweging en stem onmiddellijk, sluit de verhaalinterface en bewaart de fase, stop en positie. Opnieuw aanspreken of **J** maakt hervatten mogelijk. Terugkeren naar de kaart pauzeert de dag. **Opnieuw beginnen** reset expliciet. Voortgang wordt alleen in de huidige appsessie bewaard.

Een open paneel of focusverlies stopt het lopen. Een onbereikbare route meldt het probleem; Marie loopt niet door een gebouw om de scène toch af te maken. De route gebruikt de daadwerkelijke terrein- en gebouwcolliders, met bestemmingen buiten de beoordeelde gebouwpolygonen.

## Historische en visuele grenzen

**Hoe weten we dit?** scheidt kaartinterpretatie, gedocumenteerde kerkgeschiedenis, algemene werktuigenkennis en illustratie. Marie draagt tijdens de hele dag dezelfde muts, halsdoek, bruine jas, blauwgrijze rok, schort en schoenen. Dit is een eenvoudige vormstudie, geen fotorealistisch of historisch gevalideerd eindmodel. De lichaamsgebaren en mondbeweging zijn eenvoudig; er is geen nauwkeurige lipsynchronisatie.

Het [verhaalontwerp](../interactive-story.md) bevat de scènes, spelregels en acceptatiegevallen. Het [onderzoek naar personages en skills](../character-research.md) vergelijkt Character Creator 5, ActorCore, MetaHuman en MPFB, inclusief de beperkingen voor deze Unity-versie, macOS, Quest en kleding uit circa 1775. Definitieve kleding vraagt aanvullende controle tegen lokale bronnen.

- [Ferrariskaart, KBR/NGI blad 93](https://uurl.kbr.be/1028485).
- [Maria-Hemelvaartkerk, Onroerend Erfgoed object 41893](https://inventaris.onroerenderfgoed.be/erfgoedobjecten/41893).
- [Demasure & Woestenborghs, CAG, Collecties Bulskampveld (2016), p. 23–24](https://cagnet.be/files/original/52489/2016-Rapport_registratie_en_waardering_collectie_Bulskampveld_def.pdf).

## Verificatie

24/24 EditMode-tests zijn geslaagd, inclusief de vijf verbonden buitenlocaties, stoppen en hervatten in elke actieve fase, wachten, vooruitlopen en alle gesproken scènes. De oorspronkelijke fout in objectselectie kwam door een lokaal gegenereerd wereldbestand van een andere codeversie; na het samenbrengen van code en kaartgegevens zijn de regressietests groen.

De desktopspeltest controleert benaderen zonder accepteren, selecteren via een straal, accepteren via de knop, hoorbare stem, vragen, echt lopen, wachten, direct annuleren, dezelfde Marie hervatten, kaartpauze, alle vijf scènes en het einde. De controllerinterface wordt in de desktopspeler als wereldcanvas getest. Beelden en logbestanden staan lokaal in `artifacts`; fysieke Quest-validatie en gebruikerstests op natuurlijkheid blijven apart te doen.
