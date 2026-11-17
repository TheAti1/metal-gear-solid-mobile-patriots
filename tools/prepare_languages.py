"""Create read-only bilingual text overlays from prepared local game data."""
import argparse,hashlib,json,shutil
from pathlib import Path
p=argparse.ArgumentParser(description=__doc__);p.add_argument('--english-data',type=Path,required=True);p.add_argument('--turkish-data',type=Path,required=True);p.add_argument('--package',type=Path,required=True);a=p.parse_args()
manifest={}
for lang,root in [('en',a.english_data),('tr',a.turkish_data)]:
    tables=sorted(root.glob('localisation/**/english*.dat'))
    if len(tables)!=14:raise SystemExit(f'{lang}: expected 14 English lookup tables, found {len(tables)}')
    for src in tables:
        rel=src.relative_to(root);dst=a.package/'Oyun/languages'/lang/'data'/rel;dst.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(src,dst)
        manifest[f'{lang}/{rel.as_posix()}']=hashlib.sha256(dst.read_bytes()).hexdigest()
(a.package/'Oyun/languages/manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print('Prepared 14 tables per language. Existing game files and saves were kept.')
