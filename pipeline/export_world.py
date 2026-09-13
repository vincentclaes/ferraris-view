"""Reviewable Ferraris vectors -> GeoJSON (WGS84) and Unity metric geometry.

CV candidates are retained separately: red parish numbers resemble buildings.
The Winksele MVP uses manually corrected symbols, not unreviewed CV detections.
"""
import argparse
import json
import math
import hashlib
from pathlib import Path
import shutil

import cv2
import geopandas as gpd
import numpy as np
from PIL import Image, ImageDraw
from shapely.geometry import LineString, Polygon, Point
from shapely import affinity
from shapely.ops import triangulate, split
from shapely.geometry.polygon import orient
from generate_area import ROOT


def road_mask(roads, size, resolution=2048):
    """North-up pixel centres: red road coverage, green illustrative wheel wear.

    Exact point-to-segment distances preserve vector width through bends and
    crossings. Only each segment's bounding window is evaluated.
    """
    coverage=np.zeros((resolution,resolution),dtype=np.float32)
    wear=np.zeros_like(coverage); pixel=size/resolution; feather=.35
    for road in roads:
        radius=road['width']/2
        for p,q in zip(road['points'],road['points'][1:]):
            a=np.array([p['x'],p['z']]); b=np.array([q['x'],q['z']]); d=b-a
            if np.dot(d,d)<1e-10: continue
            lo=np.minimum(a,b)-radius-feather; hi=np.maximum(a,b)+radius+feather
            x0=max(0,int(np.floor((lo[0]+size/2)/pixel))); x1=min(resolution,int(np.ceil((hi[0]+size/2)/pixel)))
            y0=max(0,int(np.floor((size/2-hi[1])/pixel))); y1=min(resolution,int(np.ceil((size/2-lo[1])/pixel)))
            if x0>=x1 or y0>=y1: continue
            x=(np.arange(x0,x1)+.5)*pixel-size/2
            z=size/2-(np.arange(y0,y1)+.5)*pixel
            dx=x[None,:]-a[0]; dz=z[:,None]-a[1]
            t=np.clip((dx*d[0]+dz*d[1])/np.dot(d,d),0,1)
            distance=np.hypot(dx-t*d[0],dz-t*d[1])
            blend=np.clip((radius+feather-distance)/(2*feather),0,1)
            blend=blend*blend*(3-2*blend)
            tracks=np.exp(-((distance-road['width']*.2)/(road['width']*.075))**4)*blend
            np.maximum(coverage[y0:y1,x0:x1],blend,out=coverage[y0:y1,x0:x1])
            np.maximum(wear[y0:y1,x0:x1],tracks,out=wear[y0:y1,x0:x1])
    return np.round(np.stack((coverage,wear,np.zeros_like(coverage)),axis=-1)*255).astype(np.uint8)


def export(name='winksele'):
    out = ROOT/'data'/name
    area = json.loads((out/'area.json').read_text())
    trace = json.loads((out/'tracing.json').read_text())
    assert trace['origin'] == [area['lat'],area['lon']] and trace['size']==area['size'], 'Tracing is for a different area; create a matching tracing.json'
    size=area['size']; scale=size/trace['imageSize']
    def local(p): return (p[0]*scale-size/2,size/2-p[1]*scale)
    groups={k:[] for k in ['roads','buildings','forest','fields']}
    world={'roads':[],'buildings':[],'patches':[],'trees':[]}
    def points(coords): return [dict(x=round(x,4),z=round(z,4)) for x,z in coords]
    def record(kind,geom,**props):
        assert geom.is_valid and not geom.is_empty, (kind,geom)
        groups[kind].append(dict(geometry=affinity.translate(geom,area['originE'],area['originN']),source='Ferraris KBR sheet 93 via histcart WMS',method='manual raster interpretation',**props))
    roads=[]
    for i,coords in enumerate(trace['roads']):
        line=LineString([local(p) for p in coords]); roads.append(line.buffer(4))
        record('roads',line,width=5.0)
        samples=[line.interpolate(d).coords[0] for d in np.linspace(0,line.length,max(2,math.ceil(line.length/4)+1))]
        world['roads'].append(dict(points=points(samples),width=5))
    footprints=[]
    review=json.loads((out/'buildings-reviewed.json').read_text())
    assert review['imageSize']==trace['imageSize']
    assert hashlib.sha256((out/'ferraris.png').read_bytes()).hexdigest()==review['rasterSha256'], 'Review belongs to another raster'
    assert len({b['id'] for b in review['buildings']})==len(review['buildings'])
    for b in review['buildings']:
        assert b['kind'] in ('building','church') and b['evidence']
        poly=orient(Polygon([local(p) for p in b['footprint']]),sign=1)
        assert poly.is_valid and poly.area>0
        rect=Polygon(cv2.boxPoints(cv2.minAreaRect(np.array(poly.exterior.coords[:-1],dtype=np.float32))))
        corners=list(rect.exterior.coords)
        edges=[(np.array(corners[i+1])-corners[i]) for i in range(4)]
        axis=max(edges,key=np.linalg.norm); width=float(np.linalg.norm(axis)); axis/=width
        if axis[0]<0: axis=-axis
        depth=float(rect.area/width); x,z=rect.centroid.coords[0]
        yaw=math.degrees(math.atan2(-axis[1],axis[0]))
        normal=np.array([-axis[1],axis[0]])
        ridge=LineString([np.array([x,z])-axis*size,np.array([x,z])+axis*size])
        roof=[]
        for half in split(poly,ridge).geoms:
            triangles=[t for t in triangulate(half) if half.covers(t)]
            assert abs(sum(t.area for t in triangles)-half.area)<1e-5, 'Incomplete roof triangulation'
            for t in triangles: roof.extend(list(t.exterior.coords)[:3])
        # Add ridge intersections to walls too, so gables meet the pitched roof.
        outline=[]; ring=list(poly.exterior.coords)
        for a,c in zip(ring,ring[1:]):
            outline.append(a)
            da=np.dot(np.array(a)-[x,z],normal); dc=np.dot(np.array(c)-[x,z],normal)
            if da*dc < -1e-8: outline.append(tuple(np.array(a)+(np.array(c)-a)*da/(da-dc)))
        record('buildings',poly,archetype=b['kind'],feature_id=b['id'],legend=b['legend'],evidence=b['evidence'])
        footprints.append(poly.buffer(3))
        world['buildings'].append(dict(id=b['id'],kind=b['kind'],legend=b['legend'],x=x,z=z,width=width,depth=depth,yaw=yaw,footprint=points(outline),roof=points(roof)))
    for kind in ['fields','vegetation']:
        for i,coords in enumerate(trace[kind]):
            poly=Polygon([local(p) for p in coords])
            record('forest' if kind=='vegetation' else 'fields',poly,landuse='orchard' if kind=='vegetation' else ['grass','soil','crop'][i%3])
            verts=[]
            # Delaunay triangles filtered by polygon containment respect concavities.
            for t in triangulate(poly):
                if poly.covers(t): verts.extend(list(t.exterior.coords)[:3])
            world['patches'].append(dict(points=points(verts),kind='orchard' if kind=='vegetation' else ['grass','soil','crop'][i%3]))
            if kind=='vegetation':
                rng=np.random.default_rng(1775+i)
                minx,minz,maxx,maxz=poly.bounds
                for z in np.arange(minz,maxz,13):
                    for x in np.arange(minx,maxx,13):
                        xj=x+rng.uniform(-3,3); zj=z+rng.uniform(-3,3); p=Point(xj,zj)
                        if poly.contains(p) and not any(g.contains(p) for g in roads+footprints):
                            world['trees'].append(dict(x=xj,z=zj,scale=float(rng.uniform(.8,1.3))))
    for kind,records in groups.items():
        gpd.GeoDataFrame(records,crs=31370).to_crs(4326).to_file(out/f'{name}_{kind}.geojson',driver='GeoJSON')
    (out/'world.json').write_text(json.dumps(world,separators=(',',':')))
    # A cheap extraction experiment, saved separately for review against manual vectors.
    rgb=np.asarray(Image.open(out/'ferraris.png').convert('RGB'))
    hsv=cv2.cvtColor(rgb,cv2.COLOR_RGB2HSV)
    mask=(((hsv[:,:,0]<12)|(hsv[:,:,0]>165))&(hsv[:,:,1]>100)&(hsv[:,:,2]<215)).astype('uint8')*255
    mask=cv2.morphologyEx(mask,cv2.MORPH_CLOSE,np.ones((3,3),np.uint8))
    count,labels,stats,centroids=cv2.connectedComponentsWithStats(mask)
    candidates=[dict(pixel=centroids[i].tolist(),areaPixels=int(stats[i,4])) for i in range(1,count) if 30<=stats[i,4]<=1500]
    (out/'building_candidates.json').write_text(json.dumps(candidates,indent=2))
    Image.fromarray(mask).save(out/'red-mask.png')
    preview=Image.open(out/'ferraris.png').convert('RGB').resize((1024,1024)); draw=ImageDraw.Draw(preview)
    for road in trace['roads']: draw.line([tuple(p) for p in road],fill='#00c8ff',width=3)
    for i,b in enumerate(review['buildings']):
        contour=[tuple(p) for p in b['footprint']]
        draw.line(contour+[contour[0]],fill='#ffed00',width=1)
        draw.text(contour[0],str(i+1),fill='#00ffff',stroke_width=1,stroke_fill='black')
    for poly in trace['vegetation']: draw.line([tuple(p) for p in poly+[poly[0]]],fill='#09ef77',width=2)
    preview.save(out/'alignment.png')
    # Landcover is a single mobile-friendly texture, georeferenced exactly like
    # Ferraris. Fields follow traced boundaries instead of floating flat meshes.
    land=Image.new('RGB',(1024,1024),(116,129, 70));paint=ImageDraw.Draw(land)
    palette=[(139,148,84),(134,109,72),(166,156,88)]
    for i,poly in enumerate(trace['fields']):paint.polygon([tuple(p) for p in poly],fill=palette[i%3])
    for poly in trace['vegetation']:paint.polygon([tuple(p) for p in poly],fill=(92,118,61))
    pixels=np.asarray(land).astype(np.int16)
    noise=np.random.default_rng(1775).integers(-5,6,(1024,1024,1))
    Image.fromarray(np.clip(pixels+noise,0,255).astype('uint8')).save(out/'landcover.png')
    Image.fromarray(road_mask(world['roads'],size)).save(out/'roads.png')
    dest=ROOT/'unity/FerrarisVR/Assets/Resources/Winksele'
    dest.mkdir(parents=True,exist_ok=True)
    for filename in ['area.json','world.json','ferraris.png','landcover.png','roads.png']: shutil.copy(out/filename,dest/filename)
    print(f'Exported {len(world["roads"])} roads, {len(world["buildings"])} buildings, {len(world["patches"])} land parcels, {len(world["trees"])} instanced trees; {len(candidates)} unreviewed CV candidates')


if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--name',default='winksele')
    export(**vars(parser.parse_args()))
