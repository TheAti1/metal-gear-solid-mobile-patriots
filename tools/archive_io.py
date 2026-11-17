"""Read/rebuild this game's DTRZ archives with legacy DEFLATE framing.

Only codec 8 is accepted. Physical boundaries come from ordered offsets;
the old SDK convention stores decoded length in both directory size fields.
"""
import struct
import zlib

GZIP_PREFIX=bytes.fromhex('1f8b080000000000000b')

def directory(data):
    assert data[:4]==b'DTRZ','DTRZ arşivi bekleniyor.'
    count,folders=struct.unpack_from('<HH',data,4);p=9;names=[];dirs=['']
    for target,n in [(names,count),(dirs,folders-1)]:
        for _ in range(n):
            end=data.index(0,p);target.append(data[p:end].decode('utf-8'));p=end+1
    attrs=[struct.unpack_from('<HHH',data,p+6*i) for i in range(count)];p+=6*count
    version,chunks=struct.unpack_from('<HH',data,p);assert version==1;p+=4
    assert chunks==count,'Bu araç paylaşımlı sözlük içermeyen font paketini bekler.'
    rows={}
    for name,(folder,chunk,_) in zip(names,attrs):
        assert chunk<chunks
        rel='/'.join(x for x in [dirs[folder].replace('\\','/'),name] if x)
        pos=p+16*chunk;offset,stored,decoded,flags=struct.unpack_from('<IIII',data,pos)
        assert flags==8,(rel,'Desteklenmeyen sıkıştırma',flags)
        rows[rel]=dict(record_offset=pos,offset=offset,decoded_length=decoded)
    offsets=sorted({row['offset'] for row in rows.values()}|{len(data)})
    next_offset=dict(zip(offsets,offsets[1:]))
    for row in rows.values():row['end']=next_offset.get(row['offset'],len(data))
    return rows

def decode(data,row):
    if not row['decoded_length']:return b''
    stream=data[row['offset']:row['end']]
    assert stream.startswith(GZIP_PREFIX)
    coder=zlib.decompressobj(-15);raw=coder.decompress(stream[10:])+coder.flush()
    assert coder.eof and not coder.unused_data
    assert len(raw)==row['decoded_length']
    return raw

def encode(raw):
    if not raw:return b''
    coder=zlib.compressobj(9,zlib.DEFLATED,-15)
    return GZIP_PREFIX+coder.compress(raw)+coder.flush()

def patch_archive(data,replacements):
    if not replacements:return data
    rows=directory(data);assert set(replacements)<=set(rows),'Arşivde bulunmayan dosya.'
    out=bytearray(data[:min(row['offset'] for row in rows.values())])
    for rel,row in sorted(rows.items(),key=lambda item:item[1]['offset']):
        if rel in replacements:
            raw=replacements[rel];payload=encode(raw);size=len(raw)
        else:
            size=row['decoded_length'];payload=data[row['offset']:row['end']] if size else b''
        offset=len(out);out.extend(payload)
        struct.pack_into('<IIII',out,row['record_offset'],offset,size,size,8)
    result=bytes(out);check=directory(result)
    for rel,raw in replacements.items():assert decode(result,check[rel])==raw
    return result
