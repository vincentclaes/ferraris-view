"""Check the actual Quest package for current offline data and stale build copies."""
import hashlib
import json
from pathlib import Path
import re
import sys
import zipfile

ROOT = Path(__file__).resolve().parents[1]
apk = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / 'builds/Winksele1775.apk'
with zipfile.ZipFile(apk) as package:
    names = package.namelist()
    assert len(names) == len(set(names)), 'Duplicate ZIP entries'
    copies = [n for n in names if re.search(r' \d+(?:\.|$)', n)]
    assert not copies, f'Stale numbered build copies: {len(copies)}; first: {copies[:3]}'
    libraries = [n for n in names if n.startswith('lib/') and n.endswith('.so')]
    assert libraries and all(n.startswith('lib/arm64-v8a/') for n in libraries), 'Quest must use ARM64 only'
    for library in ['libunity.so', 'libil2cpp.so', 'libUnityOpenXR.so', 'libopenxr_loader.so']:
        assert 'lib/arm64-v8a/' + library in names, f'Missing runtime: {library}'
    assert 'assets/bin/Data/UnitySubsystems/UnityOpenXR/UnitySubsystemsManifest.json' in names
    expected = {
        str(p.relative_to(ROOT)): p.read_bytes()
        for p in [ROOT/'data/winksele/world.json', ROOT/'data/winksele/area.json',
                  *sorted((ROOT/'unity/FerrarisVR/Assets/Resources/Discovery').glob('*.json'))]
        if not re.search(r' \d+\.', p.name)
    }
    found = {}
    for name in names:
        if not name.startswith('assets/bin/Data/'):
            continue
        data = package.read(name)
        for path, content in expected.items():
            if content in data:
                found[path] = name
    assert found.keys() == expected.keys(), f'Missing or outdated bundled content: {expected.keys() - found.keys()}'
    assert package.testzip() is None, 'Corrupt APK ZIP entry'
receipt = dict(apk=str(apk.resolve()), bytes=apk.stat().st_size,
               sha256=hashlib.sha256(apk.read_bytes()).hexdigest(),
               bundledContent=found, nativeLibraries=libraries,
               hardwareVerified=False)
print(json.dumps(receipt, indent=2))
