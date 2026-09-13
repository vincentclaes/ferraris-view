"""Generate bundled Flemish dialogue with Piper 1.8.0 (authoring only).

Install piper-tts==1.8.0 in a separate environment, then run this script.
The game ships WAV files; it does not ship Piper or download a voice at runtime.
"""
import hashlib
import json
from pathlib import Path
import urllib.request
import wave

from piper import PiperVoice

ROOT = Path(__file__).resolve().parents[1]
REVISION = "1162a9173d0ce503555aed757976b7a9912eae4c"
BASE = f"https://huggingface.co/rhasspy/piper-voices/resolve/{REVISION}/nl/nl_BE/nathalie/medium/"
MODEL = "nl_BE-nathalie-medium.onnx"


def generate():
    cache = ROOT / ".tools/story-voice"
    cache.mkdir(parents=True, exist_ok=True)
    for name in (MODEL, MODEL + ".json", "MODEL_CARD"):
        path = cache / name
        if not path.exists():
            urllib.request.urlretrieve(BASE + name, path)
    story = json.loads((ROOT / "unity/FerrarisVR/Assets/Resources/Discovery/day.json").read_text())
    lines = {
        "greeting": "Dag! Heb je even tijd? Kom gerust dichterbij.",
        "invitation": "Dag, ik ben Marie. Ik wil de oogst binnenhalen voor het weer omslaat. Loop je een stukje mee?",
        "wait": "Ik wacht hier. Kom maar als je zover bent.",
        "rejoin": "Daar ben je. Loop maar mee.",
    }
    for index, stop in enumerate(story["stops"]):
        for key, field in (("stop", "dialogue"), ("answer", "answer"), ("follow", "follow")):
            lines[f"{key}-{index}"] = stop[field]
    output = ROOT / "unity/FerrarisVR/Assets/Resources/Discovery/Voice/Marie"
    output.mkdir(parents=True, exist_ok=True)
    voice = PiperVoice.load(str(cache / MODEL))
    records = {}
    for key, text in lines.items():
        target = output / f"{key}.wav"
        with wave.open(str(target), "wb") as wav:
            voice.synthesize_wav(text, wav)
        with wave.open(str(target)) as wav:
            seconds = wav.getnframes() / wav.getframerate()
        records[key] = dict(text=text, seconds=round(seconds, 3), sha256=hashlib.sha256(target.read_bytes()).hexdigest())
    manifest = dict(engine="piper-tts 1.8.0 (authoring only)", voice="nl_BE-nathalie-medium",
                    model_url=BASE + MODEL, model_sha256=hashlib.sha256((cache / MODEL).read_bytes()).hexdigest(),
                    model_card=BASE + "MODEL_CARD", dataset_license="CC0", clips=records)
    (ROOT / "docs/story-voice.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n")
    print(f"Generated {len(records)} Dutch voice clips ({sum(x['seconds'] for x in records.values()):.1f} seconds)")


if __name__ == "__main__":
    generate()
