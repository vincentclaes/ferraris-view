import json
import sys
from pathlib import Path
import numpy as np
import geopandas as gpd
import rasterio
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'pipeline'))
from generate_area import ROOT, FORWARD, geo_to_local, local_to_geo


def test_origin_and_roundtrips():
    origin=FORWARD.transform(4.64385,50.89795)
    assert geo_to_local(4.64385,50.89795,origin)==(0,0)
    for x,z in [(0,0),(-500,500),(500,-500),(230,172)]:
        lon,lat=local_to_geo(x,z,origin)
        assert np.allclose(geo_to_local(lon,lat,origin),(x,z),atol=.002)


def test_raster_and_elevation_alignment():
    out=ROOT/'data/winksele'; a=json.loads((out/'area.json').read_text())
    with rasterio.open(out/'ferraris.tif') as ds:
        assert ds.crs.to_epsg()==31370
        assert np.allclose(ds.bounds,a['bounds'])
        assert ds.read().std()>10 and ds.width>=2048
    with rasterio.open(out/'elevation.tif') as ds:
        assert ds.crs.to_epsg()==31370 and ds.shape==(257,257)
        assert np.allclose(ds.xy(0,0),(a['bounds'][0],a['bounds'][3]),atol=.02)
        assert np.allclose(ds.xy(256,256),(a['bounds'][2],a['bounds'][1]),atol=.02)
        exported=np.array(a['heights']).reshape(257,257)
        assert np.allclose(exported,ds.read(1)[::-1])
        assert exported.max()-exported.min()>20


def test_runtime_geodetic_grid_against_proj():
    a=json.loads((ROOT/'data/winksele/area.json').read_text()); grid=a['geoGrid'];n=a['geoResolution'];size=a['size']
    for x,z in np.random.default_rng(1775).uniform(-size/2,size/2,(1000,2)):
        u=(x/size+.5)*(n-1);v=(z/size+.5)*(n-1);i=min(int(u),n-2);j=min(int(v),n-2);u-=i;v-=j
        def at(key):return sum(grid[(j+dy)*n+i+dx][key]*(u if dx else 1-u)*(v if dy else 1-v) for dx in (0,1) for dy in (0,1))
        actual=geo_to_local(at('lon'),at('lat'),(a['originE'],a['originN']))
        assert np.linalg.norm(np.array(actual)-[x,z])<.01


def test_features_and_bounds():
    out=ROOT/'data/winksele'
    for kind in ['roads','buildings','forest','fields']:
        g=gpd.read_file(out/f'winksele_{kind}.geojson').to_crs(31370)
        assert len(g)>0 and g.is_valid.all()
        assert (g.area>=0).all()
    w=json.loads((out/'world.json').read_text())
    assert len(w['buildings'])>=40 and len(w['roads'])>=8 and len(w['trees'])>=100
    assert any(b['kind']=='church' for b in w['buildings'])
    for b in w['buildings']:
        assert abs(b['x'])<500 and abs(b['z'])<500
    for p in w['patches']: assert len(p['points'])%3==0


def test_reviewed_building_contours_and_categories():
    from shapely.geometry import Polygon
    from shapely.ops import unary_union
    out=ROOT/'data/winksele'
    reviewed=json.loads((out/'buildings-reviewed.json').read_text())
    world=json.loads((out/'world.json').read_text())['buildings']
    geo=gpd.read_file(out/'winksele_buildings.geojson').to_crs(31370)
    area=json.loads((out/'area.json').read_text()); scale=area['size']/reviewed['imageSize']
    assert len(world)==len(reviewed['buildings'])==53
    assert {b['kind'] for b in world}=={'building','church'}
    assert sum(b['kind']=='church' for b in world)==1
    assert all('winksele-legacy-'+str(i).zfill(2) not in {b['id'] for b in world} for i in reviewed['rejectedLegacy'])
    for source,b in zip(reviewed['buildings'],world):
        assert b['id']==source['id'] and b['kind']==source['kind'] and b['legend']==source['legend']
        expected=Polygon([(x*scale-area['size']/2,area['size']/2-y*scale) for x,y in source['footprint']])
        actual=Polygon([(p['x'],p['z']) for p in b['footprint']])
        assert expected.hausdorff_distance(actual)<.001
        assert expected.symmetric_difference(actual).area<.01
        triangles=[Polygon([(p['x'],p['z']) for p in b['roof'][i:i+3]]) for i in range(0,len(b['roof']),3)]
        assert unary_union(triangles).symmetric_difference(actual).area<.01, b['id']
        vector=geo[geo.feature_id==b['id']].geometry.iloc[0]
        from shapely import affinity
        assert affinity.translate(vector,-area['originE'],-area['originN']).hausdorff_distance(expected)<.002
        assert all(np.isfinite(b[key]) for key in ['x','z','width','depth','yaw'])
    courtyard=next(b for b in world if b['id']=='winksele-legacy-43')
    outline=Polygon([(p['x'],p['z']) for p in courtyard['footprint']])
    assert outline.convex_hull.area-outline.area>20
