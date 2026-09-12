"""Reviewable Ferraris vectors -> GeoJSON (WGS84) and Unity metric geometry.

CV candidates are retained separately: red parish numbers resemble buildings.
The Winksele MVP uses manually corrected symbols, not unreviewed CV detections.
"""
import argparse
import json
import math
from pathlib import Path
import shutil

import cv2
import geopandas as gpd
import numpy as np
from PIL import Image, ImageDraw
from shapely.geometry import LineString, Polygon, Point
from shapely import affinity
from shapely.ops import triangulate
from generate_area import ROOT


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
    for i,b in enumerate(trace['buildings']):
        px,py,w,h,angle,*archetype=b
        # Rotation in image space is clockwise because image Y points south.
        rect=affinity.rotate(Polygon([(-w/2,-h/2),(w/2,-h/2),(w/2,h/2),(-w/2,h/2)]),-angle)
        x,z=local([px,py]); rect=affinity.translate(affinity.scale(rect,xfact=scale,yfact=scale,origin=(0,0)),x,z)
        kind=archetype[0] if archetype else ['house','barn','farmhouse'][i%3]
        record('buildings',rect,archetype=kind)
        footprints.append(rect.buffer(3))
        world['buildings'].append(dict(x=x,z=z,width=w*scale,depth=h*scale,yaw=angle,kind=kind))
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
    for b in world['buildings']:
        x=(b['x']/size+.5)*1024;y=(.5-b['z']/size)*1024
        draw.ellipse((x-5,y-5,x+5,y+5),outline='#ffed00',width=2)
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
    dest=ROOT/'unity/FerrarisVR/Assets/Resources/Winksele'
    dest.mkdir(parents=True,exist_ok=True)
    for filename in ['area.json','world.json','ferraris.png','landcover.png']: shutil.copy(out/filename,dest/filename)
    print(f'Exported {len(world["roads"])} roads, {len(world["buildings"])} buildings, {len(world["patches"])} land parcels, {len(world["trees"])} instanced trees; {len(candidates)} unreviewed CV candidates')


if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--name',default='winksele')
    export(**vars(parser.parse_args()))
