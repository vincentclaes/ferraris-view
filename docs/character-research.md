# Characters and narrative tools for issue #4

Research checked 13 September 2026. Target: the existing Unity 6000.6.0f1 project, desktop/browser and native Quest 3, with bundled Dutch story content.

## Character recommendation

Use one authored, rigged person with a fixed costume, facial expressions and recorded Dutch speech. Build natural behaviour through attention, turn-taking, gestures, walking and waiting. Keep the story deterministic and available offline. A conversational AI service does not supply a period character model, navigation or reliable historical evidence. The current slice has a generated illustrative Marie; it does not claim to be one of the high-fidelity assets below.

| Candidate | Useful capability | Fit and constraint |
| --- | --- | --- |
| [Character Creator 5](https://www.reallusion.com/character-creator/digital-human.html) with [Unity Auto Setup](https://www.reallusion.com/auto-setup/unity/download.html) | Detailed human authoring, facial performance and Unity import tools | Preferred production candidate if Windows authoring is available. The current download page lists Windows and Unity 2022.3–6000.3; this project uses 6000.6 on macOS. Verify an exported sample in this exact project before buying assets or adopting the plugin. |
| [ActorCore ActorSCAN / ActorBUILD](https://www.reallusion.com/content/characterspec/) | Rigged people with facial animation; useful motion library | A candidate for animation and secondary people. A modern scanned outfit does not become suitable for 1775 through recolouring. Check whether a specific asset can actually be re-clothed. No period-ready asset has been verified here. |
| [MetaHuman](https://www.metahuman.com/license) | High-detail digital human pipeline | Epic explicitly permits other engines. This is not evidence of a ready Unity shader/face pipeline or standalone Quest performance. More integration work than the preferred Unity export route; retain as a visual benchmark. |
| [MakeHuman / MPFB](https://static.makehumancommunity.org/mpfb.html) and Blender | Editable human base and custom garments | Practical open-source authoring route for this Mac. Core graphic assets are CC0 according to the [project licence](https://static.makehumancommunity.org/about/license.html); check community assets separately. Requires artist work for convincing faces, clothes, skinning and speech. |

These are researched candidates, not imported or performance-validated assets. No purchase or account registration was made. Check the exact asset's game redistribution terms before acquisition; tooling and content licences are separate.

CC supports [character optimisation and LOD generation](https://www.reallusion.com/character-creator/lod-characters.html). For the first hero, propose 25–40k triangles nearby, 10–15k at medium distance and 3–5k far away, baked cloth, hair cards, 1–2k textures and few materials. These are starting budgets, not measured limits. Profile the complete landscape on Quest at its existing 72 FPS target. Never assume a cinematic 8k/subdivision asset fits mobile VR.

## Face and costume continuity

The user confirmed that “same clothes” means both period coherence and a persistent identity: clothing must suit the character and represent 1775 as closely as possible. Every character has their own face and outfit; do not make all villagers identical.

Marie has one approved head/body, hair, costume, material palette and rig. Reuse that same asset during the greeting, route, pause, resumption and ending. Derive all LODs and platform variants from that master. Do not generate a different face or outfit for each line, image or scene. Record the approved asset version and compare front, side and back views across variants.

Proposed Marie costume: plain linen cap and neck cloth, fitted working jacket, ankle-length skirt, apron, stockings and worn closed shoes. A muted brown jacket, blue-grey skirt and cream apron give her a readable silhouette against the fields. Shape, fabric, colour, age and wear are illustrative art decisions pending local costume review, not documented facts about a named Winksele resident.

Useful historical research boundaries:

- The [British Museum print after Theobald Michau, circa 1775–1790](https://www.britishmuseum.org/collection/object/P_1877-0811-543), is comparative rural imagery. Publication date does not prove that the earlier artist depicted clothing current in Winksele in 1775. The catalogue was visible in search; its full page refused access.
- The Rijksmuseum's [Boerin bij een waslijn](https://www.rijksmuseum.nl/nl/collectie/object/Boerin-bij-een-waslijn--0cbb58238cd3b92c72f851b528cbdda9) has the broad date 1770–1834. It cannot establish an exact 1775 outfit.
- Avoid using a later Brabant folk costume as an eighteenth-century uniform. [Erfgoed Brabant](https://erfgoedbrabantverhalen.nl/erfgoedinstelling/museum-de-muts/) dates the poffer's development to the nineteenth century.
- Ferraris land use and a church record do not establish fabric, face, hairstyle, personal possessions or an individual's biography. Keep these labelled as illustrative.

## Skills found

No installed skill in this session specifically covers historical game narrative. The following public candidates were inspected; none was installed globally or executed as a script.

| Candidate | Use here | Assessment |
| --- | --- | --- |
| [FMG-Studio game-designer](https://github.com/FMG-Studio/codex_skills/blob/main/skills/game-designer/SKILL.md) | Translate the encounter into player actions, states, recovery and playtest observations | Best small general design reference. Separates a design proposal from a proven playable experience. |
| [Vibeship narrative-design](https://github.com/vibeforge1111/vibeship-spawner-skills/blob/main/game-dev/narrative-design/skill.yaml) | Causal story beats, short spoken dialogue, environmental clues and situational callouts | Relevant specialist material, but distributed as a YAML skill rather than a standalone Codex SKILL.md. Treat its persona claims and example code as third-party material, not verified expertise. |
| [Yuki001 game-design-review](https://github.com/Yuki001/game-dev-skills/blob/main/skills/game-design-review/SKILL.md) | Review the encounter and identify a small playtest | Candidate for a later design review; no installation or execution validation performed. |

The design below uses causal scene connections, voluntary participation and explicit recovery. No new agent framework is needed for this single story.

## Runtime authoring tools

[Yarn Spinner](https://docs.yarnspinner.dev/yarn-spinner-for-unity/creating-commands-functions) is a strong candidate when the project grows to several dialogue trees: Unity commands can pause a script until movement or another task finishes. [ink](https://www.inklestudios.com/ink/) is another established branching-narrative language with Unity integration. Neither tool provides a human model or a pathfinder.

For this first guided story, extend the existing bundled `Discovery/day.json` and `PersonsDay` workflow. Adopting a narrative package now would add migration and integration work without resolving the missing physical guide.

## Bundled prototype voice

The playable slice uses Piper's Flemish `nl_BE-nathalie-medium` voice. Its [model card](https://huggingface.co/rhasspy/piper-voices/blob/1162a9173d0ce503555aed757976b7a9912eae4c/nl/nl_BE/nathalie/medium/MODEL_CARD) identifies the source dataset as CC0. `pipeline/generate_story_voice.py` uses Piper 1.8.0 as a local authoring tool; the game contains WAV clips, not the engine or model. The pinned model revision, SHA-256 and exact transcript of all 19 clips are in [story-voice.json](story-voice.json). The voice is explicitly labelled a computer voice. No user microphone or cloud conversation is involved.
