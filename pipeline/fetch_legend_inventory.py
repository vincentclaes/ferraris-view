"""Inventory the NGI 1777 legend's public labels; do not redistribute its images."""
import json,re
from pathlib import Path
from urllib.request import urlopen
ROOT=Path(__file__).resolve().parents[1]
BASE='https://topoferraris.ngi.be/'
MAPPED={'leg1777-1_18':['road'],'leg1777-1_13':['soil','crop'],'leg1777-1_9':['orchard'],'leg1777-1_20':['grass'],'leg1777-1_43':['tree'],'leg1777-1_61':['house','barn','farmhouse'],'leg1777-2_38':['church']}
EXTRA={'Pente douce':'Zachte helling','Rochers':'Rotsen','Isle':'Eiland','Marée':'Getij','Plage de sable':'Zandstrand','Dunes intérieures':'Landduinen','Dunes côtières':'Kustduinen'}
def decode(s):return re.sub(r'\\u([a-fA-F0-9]{4})|\\x([a-fA-F0-9]{2})',lambda m:chr(int(m[1] or m[2],16)),s)
def main():
    html=urlopen(BASE,timeout=30).read().decode()
    asset=re.search(r'<script[^>]+src="(main-[^"]+\.js)"',html)[1]
    js=urlopen(BASE+asset,timeout=30).read().decode()
    start=js.index('selectors:[["tt-ferraris1777-legend"]]');part=js[start:js.index('styles:',start)]
    seen=set();entries=[]
    for path,label in re.findall(r'"src","(\./assets/legend/images_1777/[^\"]+)","alt","([^\"]+)"',part):
        key=Path(path).stem
        if key in seen:continue
        seen.add(key);label=decode(label);label=EXTRA.get(label,label)
        label={'Abb.':'Abdij (Abb.)','A S: M:':'Aan Zijne/Hare Majesteit (A S: M:)','Bs':'Bos (Bs)','Chaau / Chau':'Kasteel (Chaau / Chau)','Clle':'Kapel (Clle)','Cse':'Pachthoeve (Cse)',"Couv't":"Klooster (Couv’t)",'Hau / Hau':'Gehucht (Hau)','Min / Mlin':'Molen (Min / Mlin)','MGNE':'Berg / heuvel (MGNE)','Riv:':'Rivier (Riv:)','Rui:':'Beek (Rui:)','S / Ste':'Sint (S / Ste)'}.get(label,label)
        kinds=MAPPED.get(key,[])
        status='Categorie gebruikt; precieze plaats en subtype blijven kaartinterpretatie.' if kinds else 'Niet als afzonderlijk type gemodelleerd; aanwezigheid op de kaart niet vastgesteld.'
        if kinds==['tree']:status='Alleen categorieverwijzing; individuele 3D-boomposities zijn illustratief.'
        entries.append(dict(id=key,label=label,source=BASE+path[2:],types=kinds,status=status))
    assert len(entries)==150, 'Upstream legend changed: review before updating the inventory.'
    target=ROOT/'unity/FerrarisVR/Assets/Resources/Discovery/legend.json'
    target.write_text(json.dumps(dict(source=BASE,sourceBundle=asset,entries=entries),ensure_ascii=False,indent=2)+'\n')
    print(f'{len(entries)} distinct historical legend references inventoried')
if __name__=='__main__':main()
