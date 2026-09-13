# Een dag met Marie — interaction and story design

Continuation of [issue #4](https://github.com/vincentclaes/ferraris-view/issues/4). The original issue is closed; the existing implementation is a five-stop text walk with a golden beacon. This design replaces that abstraction with a person who invites the visitor, leads, notices their pace and accepts their departure.

All quoted dialogue and controls below are Dutch. Marie, the dialogue, weather, errands, crop and personal circumstances are fictional. The museum and map evidence remain in `Hoe weten we dit?`. See [character and skill research](character-research.md).

## Experience

The visitor meets Marie near the farmyard. She wants to bring the day's produce indoors before the weather changes. She has a reason to visit the field, orchard, church surroundings and barn. The visitor accompanies her rather than ticking off unrelated information panels.

Marie notices the visitor, turns towards them and greets them once. The visitor approaches and chooses to speak. She introduces herself and asks for company. Choosing `Ik loop mee` begins the story; `Niet nu` leaves the visitor free. There is no automatic quest acceptance, camera grab, forced teleport or punishment for leaving.

Each stop has a short spoken-style scene, an optional question and a clear invitation to continue. The visitor controls reading speed. In the final art/audio pass, use the exact same Dutch text for recorded speech and subtitles, with replay and independent voice volume. Essential dialogue never depends on audio or a network connection.

## Story beats and dialogue

1. **Farmyard — the invitation.** Marie checks her basket and looks towards the field. “Dag, ik ben Marie. Ik wil de oogst binnenhalen voor het weer omslaat. Loop je een stukje mee?” The visitor can accept or decline. “Eerst naar het graan. Kom, ik wijs je de weg.”
2. **Field — shared work.** She stops at the edge, turns towards the visitor and gestures to the crop. “Zie je die halmen? Die brengen we samen binnen. Alleen zou ik hier nog lang bezig zijn.” Optional question: `Waarom zoveel handen?` Answer: “Snijden, verzamelen en dragen: het werk houdt niet op bij het afsnijden.” Transition: “Er ligt ook fruit in de boomgaard. Dat nemen we mee voor we naar huis gaan.”
3. **Orchard — care and choice.** She looks at the ground and her basket. “De gevallen vruchten leg ik apart. Wat beschadigd is, wil ik eerst gebruiken.” Optional question: `Wat bewaren we?` Answer: “We kiezen zorgvuldig. In dit verhaal neem ik het gave fruit mee en houd ik het beschadigde apart.” Transition: “Bij de kerk zou ik mijn buur treffen. Ik wil weten wie straks kan helpen.”
4. **Church surroundings — dependence on others.** Marie looks along the road, then back at the visitor. “Mijn buur is er nog niet. Dan loop ik alvast terug. Met hulp gaat het dragen straks sneller.” Optional question: `Ken je hier iedereen?` Answer: “In mijn verzonnen dag kom ik bekenden tegen bij het werk en onderweg.” A second voiced/animated neighbour is a later cast expansion, not a required invisible speaker. Transition: “Kom, we brengen de mand naar de schuur.”
5. **Barn exterior — resolution.** Marie lowers the basket. “Zo, de mand is binnen bereik. Bedankt voor je gezelschap. Nu zie je hoeveel plekken bij één werkdag horen.” Optional question: `Is het werk nu klaar?` Answer: “Voor vandaag leggen we de mand neer. Het graan moet later nog verder verwerkt worden.” `Rond de dag af` concludes the story and returns free exploration. No claim that this particular mapped building was a barn.

The weather gives the story direction, not a countdown. No crop, ownership or exact local custom is presented as established by the Ferraris map. With the current route, walking distance should determine duration; measure a playthrough before promising a five-minute story.

## Guide behaviour

Starting tuning values, to be playtested:

- Notice the visitor within 8 m; offer conversation within 3 m with an unobstructed sightline.
- Walk at 1.5 m/s. Stop when the visitor falls more than 7 m behind; resume only within 3.5 m. Separate distances prevent rapid stop/start oscillation.
- When waiting, turn towards the visitor and say once: “Ik wacht hier. Kom maar als je zover bent.” On reunion: “Daar ben je. Loop maar mee.” Do not repeat a call every frame or shout across the landscape.
- If the visitor gets ahead, continue towards the same destination; never chase or force them backwards. At the destination, wait for them to approach before advancing the scene.
- Never continue essential dialogue while the visitor is out of conversation range. Freeze movement while a panel is open or the application lacks focus.
- If a route is unavailable, stop and show “Ik vind hier geen doorgang. Je kunt later opnieuw proberen of het verhaal verlaten.” Never walk through a building or advance to an unreachable stop.
- Returning to the map pauses the story. Rejoining preserves the stop and the guide's location in the current app session.

## State and cancellation contract

| State | Player action or event | Result |
| --- | --- | --- |
| Available | Approach and speak | Greeting; explicit invitation |
| Invitation | `Ik loop mee` | First scene begins |
| Invitation | `Niet nu` or close | No story accepted |
| Speaking | Optional question | Response; same scene remains active |
| Speaking | `Ik loop mee` / continue | Guide starts towards the next stop |
| Guiding | Visitor falls behind | Waiting; progress unchanged |
| Waiting | Visitor catches up | Guiding resumes |
| Guiding | Guide arrives | Wait for visitor, then next scene |
| Any active state | `Verhaal verlaten` | Stop movement and speech immediately; close story UI; retain checkpoint |
| Paused | Approach and `Hervatten` | Resume the same checkpoint without duplicating actors or changing costume |
| Paused or completed | `Opnieuw beginnen` | Explicitly reset the day |
| Final scene | `Rond de dag af` | Completed; free exploration |

`Sluiten` closes the text panel. It does not mean cancellation. `Verhaal verlaten` is the unambiguous one-action exit and requires no confirmation. If voice is playing, cancellation stops it and clears queued callouts. Escape/B retains the repository's close-panel/map convention; the story exit must also be reachable through keyboard and controller UI.

## Implementation boundaries

Reuse `PersonsDay`, `VisitorUI`, the existing map/world lifecycle and `Discovery/day.json`. Keep progression rules separate from frame updates so waiting, cancellation and resumption are testable. Use Unity navigation against the actual collision geometry; move to reachable points outside buildings instead of their footprint centres. Keep one persistent guide instance. Bundled content remains available offline.

The first interaction slice can use an explicitly illustrative character. It is not final photorealistic art. A production character must replace that presentation only after the sample proves a stable face, period costume, body rig, speech expressions, Unity shader compatibility and target-device performance.

## Acceptance scenarios

- Approach without accepting: greeting appears, no progress begins, free walking remains possible.
- Accept: the same visible person speaks, indicates the next stop and leads along a reachable route.
- Fall behind: Marie stops; no scene skips. Catch up: she resumes without oscillation or repeated calls.
- Walk ahead: reach the stop, wait for Marie and hear the correct scene once.
- Ask a question: response fits the current scene and does not advance it.
- Leave during speaking, walking or waiting: movement and speech stop immediately; no delayed callback advances the story.
- Rejoin: same face, clothes, position and checkpoint; no duplicate guide. Reset is explicit.
- Return to map, open another panel, lose focus or encounter a blocked path: no unattended progression.
- Finish all five scenes: a clear ending; free exploration continues.
- Desktop click/keyboard and Quest ray/trigger can perform the same actions. Subtitles work with audio muted and offline. Verify text fits and source distinctions remain accessible.
- Profile a representative full scene on Quest; a desktop build is not headset performance evidence.

Status: design and research prepared. Runtime, final character acquisition and physical-headset evidence must be reported separately.
