"""Fetch official georeferenced Ferraris + DHMV, export a local metric Unity area.

Raster rows start NORTH. Unity +X=east, +Z=north, origin is projected WGS84
centre. All PROJ calls use always_xy=True (longitude, latitude). WMS 1.1.1
avoids the EPSG:4326 axis-order ambiguity; all downloads request EPSG:31370.
"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import urllib.parse
import urllib.request
import xml.etree.ElementTree as ET

import numpy as np
from PIL import Image
from pyproj import Transformer
import rasterio
from rasterio.transform import from_bounds

ROOT = Path(__file__).resolve().parents[1]
WMS = 'https://geo.api.vlaanderen.be/histcart/wms'
WCS = 'https://geo.api.vlaanderen.be/DHMV/wcs'
FORWARD = Transformer.from_crs(4326, 31370, always_xy=True)
INVERSE = Transformer.from_crs(31370, 4326, always_xy=True)


def fetch(url, dest, refresh=False):
    dest = Path(dest)
    if dest.exists() and not refresh:
        return dest
    dest.parent.mkdir(parents=True, exist_ok=True)
    print('Download', url, flush=True)
    req = urllib.request.Request(url, headers={'User-Agent': 'FerrarisVR-local-research/0.1'})
    with urllib.request.urlopen(req, timeout=120) as response:
        data = response.read()
    temp = dest.with_suffix(dest.suffix + '.part')
    temp.write_bytes(data)
    temp.replace(dest)
    return dest


def url(base, **params):
    return base + '?' + urllib.parse.urlencode(params)


def geo_to_local(lon, lat, origin):
    x, y = FORWARD.transform(lon, lat)
    return x-origin[0], y-origin[1]


def local_to_geo(x, z, origin):
    return INVERSE.transform(x+origin[0], z+origin[1])


def generate(lat=50.89795, lon=4.64385, size=1000, name='winksele', pixels=2048, refresh=False):
    if not (49.4 <= lat <= 51.6 and 2.4 <= lon <= 6.5 and 100 <= size <= 4000):
        raise ValueError('Area must be in Belgium and 100–4000 metres wide')
    if not name.replace('-', '').isalnum():
        raise ValueError('Area name must be alphanumeric')
    origin = FORWARD.transform(lon, lat)
    bounds = [origin[0]-size/2, origin[1]-size/2, origin[0]+size/2, origin[1]+size/2]
    out = ROOT / 'data' / name
    raw = ROOT / 'data/raw' / name
    out.mkdir(parents=True, exist_ok=True)
    key = hashlib.sha256(json.dumps([lat,lon,size,pixels]).encode()).hexdigest()[:12]
    cap = fetch(url(WMS, service='WMS', request='GetCapabilities'), raw/'histcart-capabilities.xml', refresh)
    tree = ET.parse(cap)
    ns = {'w': 'http://www.opengis.net/wms'}
    assert 'EPSG:31370' in [e.text for e in tree.findall('.//w:CRS',ns)]
    assert 'ferraris' in [e.text for e in tree.findall('.//w:Name',ns)]
    map_url = url(WMS, SERVICE='WMS', VERSION='1.1.1', REQUEST='GetMap', LAYERS='ferraris', STYLES='', SRS='EPSG:31370', BBOX=','.join(map(str,bounds)), WIDTH=pixels, HEIGHT=pixels, FORMAT='image/png')
    png = fetch(map_url, raw/f'ferraris-{key}.png', refresh)
    image = Image.open(png).convert('RGB')
    assert image.size == (pixels,pixels) and np.asarray(image).std() > 10, 'Empty Ferraris response'
    image.save(out/'ferraris.png')
    with rasterio.open(out/'ferraris.tif','w',driver='GTiff',width=pixels,height=pixels,count=3,dtype='uint8',crs='EPSG:31370',transform=from_bounds(*bounds,pixels,pixels),compress='deflate') as ds:
        ds.write(np.asarray(image).transpose(2,0,1))
    # 257 sample vertices including area edges; extend request by half a cell
    # so WCS pixel centres coincide with terrain vertices, rather than shift 2m.
    n = 257
    halfcell = size/(n-1)/2
    dem_bounds = [bounds[0]-halfcell,bounds[1]-halfcell,bounds[2]+halfcell,bounds[3]+halfcell]
    dem_url = url(WCS,SERVICE='WCS',VERSION='1.0.0',REQUEST='GetCoverage',COVERAGE='DHMVII_DTM_1m',CRS='EPSG:31370',RESPONSE_CRS='EPSG:31370',BBOX=','.join(map(str,dem_bounds)),WIDTH=n,HEIGHT=n,FORMAT='GeoTIFF')
    dem = fetch(dem_url,raw/f'dhmv-{key}.tif',refresh)
    with rasterio.open(dem) as ds:
        assert ds.crs.to_epsg() == 31370, ds.crs
        assert ds.shape == (n,n), ds.shape
        assert np.allclose(ds.bounds,dem_bounds,atol=.02), ds.bounds
        heights = ds.read(1,masked=True)
        assert not np.ma.getmaskarray(heights).any(), 'DEM contains nodata; do not silently fill'
        heights = np.asarray(heights,dtype=float)
        assert np.isfinite(heights).all() and -20 < heights.min() < heights.max() < 700
    shutil.copy(dem,out/'elevation.tif')
    np.save(out/'elevation.npy',heights)
    # A PROJ-generated 17x17 geodetic lookup grid avoids native PROJ on Android.
    # Runtime bilinear interpolation is tested against PROJ across the whole crop.
    grid = [dict(zip(('lon','lat'),local_to_geo(x,z,origin))) for z in np.linspace(-size/2,size/2,17) for x in np.linspace(-size/2,size/2,17)]
    area = dict(name=name,crs='EPSG:31370',lat=lat,lon=lon,originE=origin[0],originN=origin[1],size=size,bounds=bounds,heightResolution=n,heightBase=float(np.floor(heights.min())-1),heightRange=float(np.ceil(heights.max())-np.floor(heights.min())+2),heights=heights[::-1].ravel().tolist(),geoResolution=17,geoGrid=grid)
    (out/'area.json').write_text(json.dumps(area,separators=(',',':')))
    sources = dict(ferraris=dict(url=map_url,provider='Digitaal Vlaanderen / KBR',layer='ferraris',license='Copyright protected; metadata requires contacting owner for reuse. No open redistribution license asserted.'),elevation=dict(url=dem_url,provider='Digitaal Vlaanderen',layer='DHMVII_DTM_1m',license='Open Data License Flanders',verticalDatum='TAW metres; contemporary 2013–2015 terrain, not 1775 topography'),crs='EPSG:31370',bounds=bounds,sha256={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in [out/'ferraris.png',out/'elevation.tif']})
    (out/'sources.json').write_text(json.dumps(sources,indent=2)+'\n')
    print(f'Generated {name}: {pixels}² Ferraris; {n}² DEM {heights.min():.2f}–{heights.max():.2f}m TAW',flush=True)
    return out


if __name__ == '__main__':
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--lat',type=float,default=50.89795)
    p.add_argument('--lon',type=float,default=4.64385)
    p.add_argument('--size',type=float,default=1000)
    p.add_argument('--name',default='winksele')
    p.add_argument('--pixels',type=int,choices=[1024,2048,4096],default=2048)
    p.add_argument('--refresh',action='store_true')
    generate(**vars(p.parse_args()))
