"""Fetch a small, pinned CC0 material set from Poly Haven. Powered by Poly Haven.
No scanned Ferraris imagery is included here. Existing files are checksum-verified.
"""
from concurrent.futures import ThreadPoolExecutor
import hashlib, json, urllib.request
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'unity/FerrarisVR/Assets/Resources/Visuals/Textures'
HEADERS={'User-Agent':'FerrarisView/0.2 (local historical VR prototype; Powered by Poly Haven)'}
ASSETS={'grass':'leafy_grass','soil':'brown_mud_dry','brick':'medieval_red_brick','plaster':'worn_plaster_wall','wood':'wood_planks_grey','roof':'grey_roof_tiles','bark':'tree_bark_03'}

def request(url):
    return urllib.request.urlopen(urllib.request.Request(url,headers=HEADERS),timeout=90).read()

def main():
    OUT.mkdir(parents=True,exist_ok=True)
    manifest=ROOT/'data/visual-sources.json'
    if manifest.exists(): records=json.loads(manifest.read_text())['files']
    else:
        records=[]
        for alias,asset in ASSETS.items():
            info=json.loads(request('https://api.polyhaven.com/files/'+asset))
            for channel,key in [('diff','Diffuse'),('normal','nor_gl'),('rough','Rough')]:
                entry=info[key]['1k']['jpg']; records.append(dict(asset=asset,file=f'{alias}_{channel}.jpg',**entry))
        for asset,key,ext,name in [('kloppenheim_06_puresky','hdri','hdr','sky.hdr'),('tree_small_02','leaves_diff','png','leaves_diff.png'),('tree_small_02','leaves_alpha','png','leaves_alpha.png')]:
            info=json.loads(request('https://api.polyhaven.com/files/'+asset)); records.append(dict(asset=asset,file=name,**info[key]['2k' if key=='hdri' else '1k'][ext]))
        manifest.write_text(json.dumps({'license':'CC0','license_url':'https://polyhaven.com/license','api_credit':'Powered by Poly Haven','files':records},indent=2)+'\n')
    def fetch(r):
        dest=OUT/r['file']
        if not dest.exists() or hashlib.md5(dest.read_bytes()).hexdigest()!=r['md5']:dest.write_bytes(request(r['url']))
        assert hashlib.md5(dest.read_bytes()).hexdigest()==r['md5'],dest
        print(dest.name,flush=True)
    with ThreadPoolExecutor(max_workers=4) as pool:list(pool.map(fetch,records))
if __name__=='__main__':main()
