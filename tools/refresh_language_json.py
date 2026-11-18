"""Upgrade a translated language JSON to the current template without retranslating.

Preserves translated text by stable ID and identical source; new entries remain
blank for translation. Run once for each launcher/game component as needed.
"""
import argparse
import copy
import json
from pathlib import Path


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--input', type=Path, required=True)
    parser.add_argument('--template', type=Path, help='Defaults to the current matching catalogue in this repository')
    parser.add_argument('--out', type=Path, required=True)
    args = parser.parse_args()
    previous = json.loads(args.input.read_text(encoding='utf-8-sig'))
    component = {'mgs-mobile-launcher-language-v1': 'launcher', 'mgs-mobile-game-language-v1': 'game'}.get(previous.get('format'))
    if not component:
        parser.error('Unsupported JSON format; use game_pack_from_dat.py for legacy DAT translations')
    template = args.template or Path(__file__).resolve().parents[1] / f'localization/{component}/{component}.en.json'
    current = json.loads(template.read_text(encoding='utf-8-sig'))
    if current['format'] != previous['format']:
        parser.error('Launcher and game templates cannot be mixed')
    output = copy.deepcopy(current)
    for field in ('language', 'native_name', 'culture', 'author'):
        if field in previous:
            output[field] = previous[field]
    rows = previous['entries']
    old = {entry['id']: entry for entry in rows}
    if len(old) != len(rows):
        parser.error('Duplicate IDs in translated JSON')
    canonical = {entry['id']: entry for entry in output['entries']}
    for key, entry in old.items():
        if key not in canonical or {k: v for k, v in entry.items() if k != 'text'} != {k: v for k, v in canonical[key].items() if k != 'text'}:
            parser.error(f'Source/protected metadata changed for {key}; review manually before upgrading')
    kept = 0
    for entry in output['entries']:
        if entry['id'] in old:
            entry['text'] = old[entry['id']].get('text', '')
            kept += bool(entry['text'])
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(output, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(f'Preserved {kept} translated entries; {len(canonical.keys() - old.keys())} new entries; {len(canonical)} total.')


if __name__ == '__main__':
    main()
