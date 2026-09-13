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


def test_road_mask_follows_vector_width_bends_and_crossings():
    from export_world import road_mask
    from shapely.geometry import LineString, Point
    from shapely.ops import unary_union
    from PIL import Image
    roads=[{'width':4,'points':[{'x':-15,'z':-12},{'x':0,'z':8},{'x':14,'z':10}]},
           {'width':3,'points':[{'x':-15,'z':8},{'x':15,'z':-8}]}]
    mask=road_mask(roads,40,160)
    expected=unary_union([LineString([(p['x'],p['z']) for p in r['points']]).buffer(r['width']/2) for r in roads])
    # North-up rows and metre widths, including both sides of the sharp bend.
    for y in range(0,160,3):
        for x in range(0,160,3):
            point=Point((x+.5)*.25-20,20-(y+.5)*.25)
            if point.distance(expected.boundary)<.36: continue
            assert (mask[y,x,0]==255) if expected.contains(point) else (mask[y,x,0]==0)
    assert mask[...,1].max()>240 and np.all(mask[...,1]<=mask[...,0])
    assert np.array_equal(mask,road_mask(roads,40,160)), 'Deterministic export'
    out=ROOT/'data/winksele';area=json.loads((out/'area.json').read_text());world=json.loads((out/'world.json').read_text())
    actual=np.asarray(Image.open(out/'roads.png'))
    assert np.array_equal(actual,road_mask(world['roads'],area['size']))
    assert (out/'roads.png').read_bytes()==(ROOT/'unity/FerrarisVR/Assets/Resources/Winksele/roads.png').read_bytes()
    for road in world['roads']:
        for p in road['points']:
            x=int((p['x']/area['size']+.5)*actual.shape[1]);y=int((.5-p['z']/area['size'])*actual.shape[0])
            if 0<=x<actual.shape[1] and 0<=y<actual.shape[0]: assert actual[y,x,0]>240
