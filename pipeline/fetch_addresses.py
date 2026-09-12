"""Bundle public address points (no occupants) from Digitaal Vlaanderen for offline use."""
import datetime
import json
from pathlib import Path
from urllib.request import urlopen
from pyproj import Transformer

ROOT = Path(__file__).resolve().parents[1]
URL = 'https://geo.api.vlaanderen.be/Adressenregister/ogc/features/v1/collections/Adres/items?bbox=4.631,50.891,4.657,50.905&limit=1000&f=application%2Fgeo%2Bjson'

def main():
    area = json.loads((ROOT / 'data/winksele/area.json').read_text())
    project = Transformer.from_crs(4326, 31370, always_xy=True)
    addresses, seen, url = [], set(), URL
    while url:
        with urlopen(url, timeout=60) as response:
            page = json.load(response)
        for feature in page['features']:
            p = feature['properties']
            if p['AdresStatus'] != 'InGebruik' or not p['OfficieelToegekend']:
                continue
            # Apartments share a civic house number; keep one position per address.
            key = (p['Straatnaam'], p['Huisnummer'], p['Gemeentenaam'])
            if key in seen:
                continue
            seen.add(key)
            lon, lat = feature['geometry']['coordinates'][:2]
            e, n = project.transform(lon, lat)
            addresses.append(dict(id=p['Id'], street=p['Straatnaam'], number=p['Huisnummer'] or '', locality=p['Gemeentenaam'], x=round(e-area['originE'], 3), z=round(n-area['originN'], 3)))
        url = next((link['href'] for link in page.get('links', []) if link['rel'] == 'next'), None)
    payload = dict(date=datetime.date.today().isoformat(), source=URL, addresses=sorted(addresses, key=lambda a:(a['street'], a['number'])))
    target=ROOT/'unity/FerrarisVR/Assets/Resources/Discovery/addresses.json'
    target.parent.mkdir(parents=True,exist_ok=True)
    target.write_text(json.dumps(payload, ensure_ascii=False, indent=2)+'\n')
    print(f'Bundled {len(addresses)} current addresses: {target}')

if __name__ == '__main__':
    main()
