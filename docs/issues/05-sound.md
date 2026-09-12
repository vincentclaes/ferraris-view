# #5 — Luister naar het landschap

Drie gebundelde mono-opnamen maken plekken hoorbaar: een klok bij de kerktoren, een schaap bij een werkelijk geplaatst schaapmodel en houtwerk bij een erfgebouw. De laatste twee plaatsen en alle klanken zijn illustraties; er worden geen historische opnamen of bewezen werkplaatsen gesuggereerd.

Unity's 3D AudioSource gebruikt stereorichting en lineaire afstandsafname, met een minimumafstand van 4 m en bereik van 65 m (schaap), 80 m (hout) of 180 m (klok). Korte klanken worden herhaald met 12 seconden tussenruimte. De luisteraar volgt het hoofd/de camera. De kaart heeft geen actief omgevingsgeluid. Er is geen gemodelleerde geluidsafscherming door muren.

“Luister” biedt zachter, luider en dempen; instellingen worden lokaal bewaard. Een tekstkaart toont ook bij dempen de bron, afstand en richting. Elke bron heeft uitleg en een knop om haar plaats op de kaart te vinden. Bronnen en historische onzekerheid staan onder “Hoe weten we dit?”.

Opnamen onder CC0 (URLs, auteurs en SHA-256 in `data/audio-sources.json`):

- [Sheep Baa, AntumDeluge / mikewest](https://opengameart.org/content/sheep-baa).
- [Saw or Wood Impact, StarNinjas](https://opengameart.org/content/saw-or-wood-impact).
- [Old Church Bell, dsp9000](https://freesound.org/people/dsp9000/sounds/76405/); publieke hoge-kwaliteit-preview van dezelfde CC0-opname.

Verificatie: ontbrekende functie eerst rood; daarna 8/8 EditMode-tests (afname, richting en decodeerbare mono-opnamen). De desktopjourney controleert alle bronvolumes, dempen, stereo-plaatsing en het zichtbare paneel. Luistercomfort op de Quest moet nog fysiek gecontroleerd worden.
