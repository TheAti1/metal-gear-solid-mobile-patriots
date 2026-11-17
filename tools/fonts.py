"""Lossless reader/editor for the fonts in this game's Marmalade 4.x groups."""
from pathlib import Path
from dataclasses import dataclass
import struct,sys,re,json
from PIL import Image
ROOT=Path(__file__).resolve().parent
def iw_hash_string(text):
    value=5381
    for byte in text.lower().encode('utf-8'):value=(33*value+byte)&0xffffffff
    return value

def _find_header(data):
    for at in range(16,min(64,len(data)-9)):
        width,height,stride=struct.unpack_from('<HHH',data,at+3)
        if width==2*stride and 0<width<=2048 and 0<height<=2048 and stride*height<=len(data):return at,width,height,stride
    return None

def _read_palette(data,at):
    result=[]
    for i in range(16):
        word=struct.unpack_from('<H',data,at+i*2)[0]
        result.append(tuple(((word>>bit)&15)*17 for bit in (8,4,0,12)))
    return result

def u32(b,p):return struct.unpack_from('<I',b,p)[0]

def find_map(b,start,end):
    found=[]
    for p in range(start,end):
        if p+4>len(b):continue
        n=u32(b,p)
        if not 1<=n<=128 or p+8*n+8>len(b):continue
        ranges=[struct.unpack_from('<HHI',b,p+4+8*i) for i in range(n)]
        count=u32(b,p+4+8*n)
        if not 2<=count<=1024 or sum(x[1] for x in ranges)+1<count:continue
        if any(not (1<=a<count and 1<=c<count and a+c<=count and 32<=cp<=0x10ffff) for a,c,cp in ranges):continue
        if any(a[2]+a[1]>z[2] for a,z in zip(ranges,ranges[1:])):continue
        m=p+8+8*n
        if m+count*8>len(b):continue
        metrics=[struct.unpack_from('<B2sHHB',b,m+i*8) for i in range(count)]
        if any(not(0<w<128 and x<32768 and y<32768 and t<8) for w,_,x,y,t in metrics):continue
        found.append((p,ranges,count,m,metrics))
    assert len(found)==1,('Font character map candidates',start,end,[(x[0],x[2]) for x in found])
    return found[0]

def parse_font(b,start):
    name,group=struct.unpack_from('<II',b,start);body=start+8;flags=u32(b,body+4)
    assert flags in (0,4,0x100,0x104),(hex(name),hex(flags))
    if flags&0x100:
        hdr=_find_header(b[body:]);assert hdr
        rel,w,h,pitch=hdr;pixel=body+rel+13;pal=pixel+pitch*h
        for candidate in (pal,pal+2,pal+4):
            vals=[struct.unpack_from('<H',b,candidate+2*i)[0] for i in range(16)]
            if vals[0]>>12==0 and max(x>>12 for x in vals)==15:pal=candidate;break
        mp,ranges,count,met,metrics=find_map(b,pal+32,pal+128)
        pix=bytearray(b[pixel:pixel+pitch*h]);pixels=[(v>>shift)&15 for v in pix for shift in (0,4)]
        mode='PAL4';palette=_read_palette(b,pal)
        end=met+count*8
        assert b[end]==0
        kerning_count=u32(b,end+1);assert kerning_count<=count*count
        end+=5+kerning_count*6
        # Height follows the one-byte trailing texture-reference tag.
        height=struct.unpack_from('<H',b,pal+33)[0]
        assert 5<=height<=64
    else:
        mp,ranges,count,met,metrics=find_map(b,body+8,body+48)
        page=met+count*8
        assert b[page]==1,(hex(name),'Expected 1-bit font image')
        bits=u32(b,page+14);assert bits in (4096,8192,16384,32768,65536)
        pixel=page+19;pix=bytearray(b[pixel:pixel+bits//8]);pixels=[(v>>k)&1 for v in pix for k in range(8)]
        w=1<<(max(x+ww for ww,_,x,y,t in metrics)-1).bit_length();h=bits//w
        assert w*h==bits
        pitch=w//8;mode='MONO1';palette=[(0,0,0,0),(255,255,255,255)]
        height=struct.unpack_from('<H',b,body+9)[0];assert 5<=height<=64
        end=pixel+len(pix)
        kerning_count=u32(b,end);assert kerning_count<=count*count
        end+=4+kerning_count*6
    cmap={cp+j:first+j for first,n,cp in ranges for j in range(n)}
    assert set(cmap.values())==set(range(1,count))
    page_count_at=mp-14 if flags&0x100 else None
    if page_count_at is not None:assert struct.unpack_from('<H',b,page_count_at)[0]==count,'Cached page glyph count differs from glyph vector.'
    return dict(start=start,end=end,name_hash=name,group_hash=group,body=body,flags=flags,map_offset=mp,ranges=ranges,count=count,metrics_offset=met,metrics=metrics,pixel_offset=pixel,pixel_length=len(pix),width=w,height=h,line_height=height,mode=mode,palette=palette,indices=pixels,cmap=cmap,coordinate_mode='uv4096' if flags&0x100 else 'pixels',page_glyph_count_offset=page_count_at)

def font_records(b):
    marker=struct.pack('<I',iw_hash_string('CIwGxFont'));hits=[m.start() for m in re.finditer(re.escape(marker),b)]
    if not hits:return []
    assert len(hits)==1
    p=hits[0];count=u32(b,p+4);omitted,sized=b[p+8:p+10];assert (omitted,sized)==(1,0)
    p+=10;fonts=[]
    for _ in range(count):
        f=parse_font(b,p);fonts.append(f);p=f['end']
    return fonts

def index_image(f):
    im=Image.new('L',(f['width'],f['height']));im.putdata(f['indices']);return im

def rgba_image(f):
    im=Image.new('RGBA',(f['width'],f['height']));im.putdata([f['palette'][i] for i in f['indices']]);return im

def glyph_image(f,cp):
    w,_,x,y,t=f['metrics'][f['cmap'][cp]]
    if f['coordinate_mode']=='uv4096':x=x*f['width']//4096;y=y*f['height']//4096
    return index_image(f).crop((x,y,x+w,y+f['line_height']))

if __name__=='__main__':
    import argparse
    package=ROOT.parent if ROOT.name=='tools' else ROOT
    parser=argparse.ArgumentParser(description='Extract Marmalade font records, atlases and Unicode metrics.')
    parser.add_argument('--input',type=Path,default=package/'original_assets/groups' if (package/'original_assets/groups').exists() else ROOT/'extracted/all.dz')
    parser.add_argument('--out',type=Path,default=package/'font_extraction')
    args=parser.parse_args()
    sys.stdout.reconfigure(encoding='utf-8');root=args.input;out=args.out;out.mkdir(parents=True,exist_ok=True)
    rows=[]
    for p in root.rglob('*.group.bin'):
        for i,f in enumerate(font_records(p.read_bytes())):
            label=f'{p.stem}_{f["name_hash"]:08x}';print(label,'offset',f['start'],f['end'],'flags',hex(f['flags']),'chars',f['count'],'atlas',(f['width'],f['height']),'height',f['line_height'])
            rgba_image(f).save(out/(label+'.png'));(out/(label+'.bin')).write_bytes(p.read_bytes()[f['start']:f['end']])
            detail=out/label;detail.mkdir(exist_ok=True)
            rgba_image(f).save(detail/'atlas.png');rgba_image(f).save(detail/'atlas.tga')
            (detail/'native.font.bin').write_bytes(p.read_bytes()[f['start']:f['end']])
            glyph_dir=detail/'glyphs';glyph_dir.mkdir(exist_ok=True);glyphs=[]
            for cp,idx in sorted(f['cmap'].items()):
                im=glyph_image(f,cp);rgba=Image.new('RGBA',im.size);rgba.putdata([f['palette'][v] for v in im.getdata()]);rgba.save(glyph_dir/f'U+{cp:04X}.png')
                metric=f['metrics'][idx]
                glyphs.append(dict(codepoint=f'U+{cp:04X}',character=chr(cp),glyph_index=idx,width=metric[0],preserved_word_hex=metric[1].hex(),u=metric[2],v=metric[3],style_byte=metric[4]))
            (detail/'glyphs.json').write_text(json.dumps(glyphs,ensure_ascii=False,indent=2),encoding='utf-8')
            summary={k:v for k,v in f.items() if k not in ('indices','palette','metrics','cmap')};summary['group']=str(p.relative_to(root));summary['glyphs']=[dict(codepoint=cp,character=chr(cp),glyph_index=idx,width=f['metrics'][idx][0],u=f['metrics'][idx][2],v=f['metrics'][idx][3]) for cp,idx in f['cmap'].items()];rows.append(summary)
    (out/'font_manifest.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
