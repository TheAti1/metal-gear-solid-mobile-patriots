"""Add Turkish Unicode glyphs while retaining every original glyph."""
from pathlib import Path
import struct,json,hashlib,sys,collections,math
from PIL import Image,ImageDraw,ImageFont
from fonts import ROOT,font_records,index_image,rgba_image,glyph_image,u32

OUT=ROOT/'font_build'
TR='ÇçĞğİıÖöŞşÜü'
NEW=[('Ğ','G'),('ğ','g'),('İ','I'),('ı','i'),('Ş','S'),('ş','s')]
REPORT=[]

def xy(f,metric):
    x,y=metric[2:4]
    if f['coordinate_mode']=='uv4096':return x*f['width']//4096,y*f['height']//4096
    return x,y

def components(im):
    s={(x,y) for y in range(im.height) for x in range(im.width) if im.getpixel((x,y))};out=[]
    while s:
        todo=[s.pop()];part=[]
        while todo:
            x,y=todo.pop();part.append((x,y))
            for a,c in [(x-1,y),(x+1,y),(x,y-1),(x,y+1)]:
                if (a,c) in s:s.remove((a,c));todo.append((a,c))
        out.append(part)
    return out

def add_glyph(f,ch,base):
    im=glyph_image(f,ord(base));bbox=im.getbbox();assert bbox
    fg=max(set(im.getdata()),key=lambda i:f['palette'][i][3])
    if ch in 'Ğğ':
        # One-pixel strokes, with a rounded breve and a clear gap to the stem.
        width=5 if bbox[2]-bbox[0]>=7 else 3
        left=(bbox[0]+bbox[2]-width)//2;top=max(0,bbox[1]-3)
        im.putpixel((left,top),fg);im.putpixel((left+width-1,top),fg)
        for x in range(left+1,left+width-1):im.putpixel((x,top+1),fg)
    elif ch=='İ':
        parts=components(glyph_image(f,ord('i'))) if ord('i') in f['cmap'] else []
        dot=min(parts,key=lambda a:min(y for x,y in a)) if parts else [(0,0)]
        height=len({y for x,y in dot});top=max(0,bbox[1]-height-1)
        center=(bbox[0]+bbox[2]-1)//2
        for y in range(top,top+height):im.putpixel((center,y),fg)
    elif ch=='ı':
        parts=components(im);body=max(parts,key=lambda a:max(y for x,y in a)-min(y for x,y in a))
        keep=set(body)
        for y in range(im.height):
            for x in range(im.width):
                if (x,y) not in keep:im.putpixel((x,y),0)
    elif ch in 'ÇçŞş':
        c='C' if ch.isupper() else 'c';ced='Ç' if ch.isupper() else 'ç'
        if ord(ced) in f['cmap']:
            ci=glyph_image(f,ord(c));cc=glyph_image(f,ord(ced));cb=ci.getbbox();assert cb
            tail=cc.crop((0,cb[3],cc.width,cc.height));tb=tail.getbbox();assert tb
            mark=tail.crop(tb);cx=(bbox[0]+bbox[2]-mark.width)//2;ty=cb[3]+tb[1]
            assert ty+mark.height<=im.height
            for y in range(mark.height):
                for x in range(mark.width):
                    v=mark.getpixel((x,y))
                    if v:im.putpixel((cx+x,ty+y),v)
        else:
            center=(bbox[0]+bbox[2]-1)//2;bottom=bbox[3]
            assert bottom+1<im.height
            im.putpixel((center+1,bottom),fg);im.putpixel((center,bottom+1),fg)
    elif ch in 'ÖöÜü':
        top=max(0,bbox[1]-2);center=(bbox[0]+bbox[2]-1)//2
        im.putpixel((max(bbox[0],center-1),top),fg);im.putpixel((min(bbox[2]-1,center+2),top),fg)
    return im

def patch_font(b,f,pairs=None,images=None):
    if pairs is None:
        if f['count']==197:pairs=NEW
        elif f['name_hash']==0x1B8EB1B7:
            pairs=[('Ç','C'),('ç','c'),('Ğ','G'),('ğ','g'),('İ','I'),('ı','i'),('Ö','O'),('ö','o'),('Ş','S'),('ş','s'),('Ü','U'),('ü','u')]
        elif f['name_hash']==0xA2F1C022:pairs=[('Ğ','G'),('İ','I'),('Ş','S')]
        else:raise ValueError('Unknown alphabet font')
    if images is None:images=[add_glyph(f,ch,base) for ch,base in pairs]
    old=index_image(f);new_height=f['height']
    bottom=max(xy(f,m)[1]+f['line_height'] for m in f['metrics'])
    row_y=bottom
    repack=row_y+f['line_height']>new_height
    atlas=Image.new('L',(f['width'],new_height));atlas.paste(old,(0,0))
    metrics=list(f['metrics']);positions=[];x=4
    for (ch,base),im in zip(pairs,images):
        assert x+im.width+1<f['width']
        if not repack:atlas.paste(im,(x,row_y))
        m=f['metrics'][f['cmap'][ord(base)]]
        if f['coordinate_mode']=='uv4096':
            assert f['height']==new_height
            u=x*4096//f['width'];v=row_y*4096//new_height
        else:u,v=x,row_y
        metrics.append((m[0],m[1],u,v,m[4]));positions.append((ch,x,row_y));x+=im.width+4
    if repack:
        # Identical empty-space/duplicate glyph images share a rectangle. This
        # fits every original glyph plus Turkish into the ORIGINAL atlas size.
        assert f['mode']=='MONO1'
        all_images=[]
        for m in f['metrics']:
            xx,yy=xy(f,m);all_images.append(old.crop((xx,yy,xx+m[0],yy+f['line_height'])))
        all_images+=images;unique={};lookup=[]
        for im in all_images:
            key=(im.size,im.tobytes());unique.setdefault(key,im);lookup.append(key)
        bins=[[] for _ in range(new_height//f['line_height'])];used=[0]*len(bins);coords={}
        for key,im in sorted(unique.items(),key=lambda x:-x[1].width):
            candidates=[i for i,n in enumerate(used) if n+im.width<=f['width']]
            if not candidates:
                # Locale additions may need a larger page; original glyphs and
                # their line height are retained, including every pixel.
                previous=len(bins);new_height*=2
                assert new_height<=1024,'Font atlas exceeds supported size'
                bins.extend([] for _ in range(new_height//f['line_height']-previous))
                used.extend(0 for _ in range(len(bins)-previous))
                candidates=list(range(previous,len(bins)))
            row=min(candidates,key=lambda i:f['width']-(used[i]+im.width));xx=used[row];yy=row*f['line_height'];coords[key]=(xx,yy);used[row]+=im.width+1
        atlas=Image.new('L',(f['width'],new_height))
        for key,im in unique.items():atlas.paste(im,coords[key])
        metrics=[(m[0],m[1],*coords[key],m[4]) for m,key in zip(metrics,lookup)]
        positions=[(ch,*coords[lookup[f['count']+i]]) for i,(ch,_) in enumerate(pairs)]
    for cp in f['cmap']:
        m=metrics[f['cmap'][cp]];xx,yy=xy(f,m)
        assert atlas.crop((xx,yy,xx+m[0],yy+f['line_height'])).tobytes()==glyph_image(f,cp).tobytes()
    pix=list(atlas.getdata());encoded=bytearray()
    if f['mode']=='MONO1':
        assert set(pix)<={0,1}
        for i in range(0,len(pix),8):encoded.append(sum(pix[i+k]<<k for k in range(8)))
    else:
        for i in range(0,len(pix),2):encoded.append(pix[i]|pix[i+1]<<4)
    cmap=dict(f['cmap'])
    for i,(ch,base) in enumerate(pairs):cmap[ord(ch)]=f['count']+i
    ranges=[]
    for cp,idx in sorted(cmap.items()):
        if ranges and cp==ranges[-1][2]+ranges[-1][1] and idx==ranges[-1][0]+ranges[-1][1]:ranges[-1][1]+=1
        else:ranges.append([idx,1,cp])
    mapping=struct.pack('<I',len(ranges))+b''.join(struct.pack('<HHI',*r) for r in ranges)+struct.pack('<I',len(metrics))+b''.join(struct.pack('<B2sHHB',*m) for m in metrics)
    edits=[(f['map_offset'],f['metrics_offset']+f['count']*8,mapping),(f['pixel_offset'],f['pixel_offset']+f['pixel_length'],bytes(encoded))]
    if f['flags']&0x100:
        page_count=f['map_offset']-14
        assert struct.unpack_from('<H',b,page_count)[0]==f['count']
        edits.append((page_count,page_count+2,struct.pack('<H',len(metrics))))
    if new_height!=f['height']:
        # The 1-bit bitmap's serialised bit count controls the payload length.
        page=f['pixel_offset']-19
        assert u32(b,page+14)==f['width']*f['height']
        edits.append((page+14,page+18,struct.pack('<I',f['width']*new_height)))
    piece=bytearray(b[f['start']:f['end']])
    for a,z,data in sorted(edits,reverse=True):piece[a-f['start']:z-f['start']]=data
    if f['flags']==0:struct.pack_into('<I',piece,f['body']-f['start']+4,4)
    # Verify the serialisation with an independent fresh parse below.
    return bytes(piece),dict(name_hash=f'{f["name_hash"]:08x}',old_count=f['count'],new_count=len(metrics),unicode_characters=len(cmap),unicode_ranges=len(ranges),old_atlas=[f['width'],f['height']],new_atlas=[f['width'],new_height],atlas_repacked=repack,new_glyph_positions=positions,added_characters=''.join(ch for ch,base in pairs),all_original_glyph_pixels_preserved=True,cached_page_glyph_count=len(metrics) if f['flags']&0x100 else None)


def patch_group(b):
    edits=[];reports=[]
    for f in font_records(b):
        if all(ord(ch) in f['cmap'] for ch in 'CGISU'):
            data,report=patch_font(b,f);edits.append((f['start'],f['end'],data));reports.append(report)
    if not edits:return b,[]
    patched=bytearray(b)
    for a,z,data in sorted(edits,reverse=True):patched[a:z]=data
    delta=len(patched)-len(b);p=6
    while p+4<=len(b):
        h=u32(b,p)
        if not h:break
        size=u32(b,p+4);payload=p+8;end=payload+size-4
        if any(payload<=a<end for a,z,data in edits):struct.pack_into('<I',patched,p+4,size+delta)
        p=end
    old=font_records(b);new=font_records(patched);assert len(old)==len(new)
    for a,c in zip(old,new):
        if c['cmap']!=a['cmap']:
            needed=TR if ord('a') in a['cmap'] else 'ÇĞİÖŞÜ'
            assert all(ord(ch) in c['cmap'] for ch in needed)
            for cp in a['cmap'].keys()&c['cmap'].keys():
                assert glyph_image(a,cp).tobytes()==glyph_image(c,cp).tobytes(),('Original glyph changed',cp)
            assert c['count']==a['count']+len(c['cmap'].keys()-a['cmap'].keys())
    return bytes(patched),reports

def render_text(f,text):
    width=sum(f['metrics'][f['cmap'].get(ord(c),0)][0]+1 for c in text)+8
    im=Image.new('RGBA',(width,f['line_height']+8),(12,16,19,255));x=4
    for c in text:
        if ord(c) not in f['cmap']:x+=f['metrics'][0][0]+1;continue
        g=glyph_image(f,ord(c));rgba=Image.new('RGBA',g.size);rgba.putdata([f['palette'][v] for v in g.getdata()]);im.alpha_composite(rgba,(x,4));x+=g.width+1
    return im

if __name__=='__main__':
    import argparse
    package=ROOT.parent if ROOT.name=='tools' else ROOT
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--input',type=Path,default=package/'original_assets/groups' if (package/'original_assets/groups').exists() else ROOT/'extracted/all.dz')
    parser.add_argument('--out',type=Path,default=ROOT/'font_build')
    args=parser.parse_args();OUT=args.out;OUT.mkdir(parents=True,exist_ok=True)
    sys.stdout.reconfigure(encoding='utf-8');r=args.input;summaries=[];previews=[]
    for p in r.rglob('*.group.bin'):
        b=p.read_bytes();patched,reports=patch_group(b)
        if not reports:continue
        rel=p.relative_to(r);dst=OUT/'groups'/rel;dst.parent.mkdir(parents=True,exist_ok=True);dst.write_bytes(patched)
        for f in font_records(patched):
            if not all(ord(ch) in f['cmap'] for ch in 'ÇĞİÖŞÜ'):continue
            nm=f'{p.stem}_{f["name_hash"]:08x}';d=OUT/'fonts'/nm;d.mkdir(parents=True,exist_ok=True)
            rgba_image(f).save(d/'atlas.png');rgba_image(f).save(d/'atlas.tga');(d/'native.font.bin').write_bytes(patched[f['start']:f['end']])
            glyphs=[]
            for cp,idx in f['cmap'].items():
                gi=glyph_image(f,cp);g=Image.new('RGBA',gi.size);g.putdata([f['palette'][v] for v in gi.getdata()]);gd=d/'glyphs';gd.mkdir(exist_ok=True);g.save(gd/f'U+{cp:04X}.png');glyphs.append(dict(codepoint=f'U+{cp:04X}',character=chr(cp),glyph_index=idx,width=gi.width,coordinate=list(xy(f,f['metrics'][idx]))))
            (d/'glyphs.json').write_text(json.dumps(glyphs,ensure_ascii=False,indent=2),encoding='utf-8')
            lower=ord('a') in f['cmap']
            previews.append((nm,render_text(f,'Çç Ğğ Iı İi Öö Şş Üü' if lower else 'Ç Ğ I İ Ö Ş Ü')))
            previews.append((nm+' / sample',render_text(f,'Pijamalı hasta yağız şoföre çabucak güvendi.' if lower else 'PIJAMALI HASTA YAĞIZ ŞOFÖRE ÇABUCAK GÜVENDİ.')))
        summaries.append(dict(group=str(rel).replace('\\','/'),original_sha256=hashlib.sha256(b).hexdigest(),patched_sha256=hashlib.sha256(patched).hexdigest(),font_changes=reports))
    maxw=max(im.width for _,im in previews);canvas=Image.new('RGB',(maxw*3+30,sum(im.height*3+34 for _,im in previews)+20),(12,16,19));draw=ImageDraw.Draw(canvas);y=8
    for label,im in previews:
        draw.text((10,y),label,fill=(175,195,205));y+=22;im=im.resize((im.width*3,im.height*3),Image.Resampling.NEAREST);canvas.paste(im,(10,y),im);y+=im.height+12
    canvas.save(OUT/'turkish_font_preview.png');(OUT/'verification.json').write_text(json.dumps(summaries,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps(summaries,ensure_ascii=False,indent=2))
