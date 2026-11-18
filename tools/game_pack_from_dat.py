"""Convert an existing translated Mobile DAT directory into an importable JSON.

Only reads game files. Entries needing PC-button tokens are left blank and listed
in a separate review file rather than guessing their intended button bindings.
"""
import argparse
import copy
import json
from pathlib import Path
import re
import struct


def read_table(path):
    data = path.read_bytes()
    if len(data) < 21 or data[:4] not in (b'GLB0', b'PRT0'):
        raise ValueError(f'Invalid DAT: {path}')
    count = struct.unpack_from('<I', data, 16)[0]
    header = 20 + count * 6
    if count > 65536 or header >= len(data):
        raise ValueError(f'Invalid DAT index: {path}')
    end = data.index(b'\0', header)
    if data[header:end].upper() != b'UTF-8':
        raise ValueError(f'Unsupported DAT encoding: {path}')
    rows = {}
    for index in range(count):
        key, offset = struct.unpack_from('<HI', data, 20 + index * 6)
        start = end + 1 + offset
        if start >= len(data) or key in rows:
            raise ValueError(f'Invalid DAT row: {path}')
        rows[key] = data[start:data.index(b'\0', start)].decode('utf-8')
    return rows


def tokens(text):
    return sorted(re.findall(r'\{[^{}]+\}|%(?:\d+\$)?[-+#0 .\d]*[sdifuoxX]', text))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--data', type=Path, required=True, help='Translated data directory containing localisation/')
    parser.add_argument('--language', required=True, help='For example pt-BR')
    parser.add_argument('--name', required=True, help='For example Português (Brasil)')
    parser.add_argument('--culture', required=True, help='For example pt-BR')
    parser.add_argument('--author', default='')
    parser.add_argument('--template', type=Path, default=Path(__file__).resolve().parents[1] / 'localization/game/game.en.json')
    parser.add_argument('--out', type=Path, required=True)
    args = parser.parse_args()
    doc = json.loads(args.template.read_text(encoding='utf-8-sig'))
    doc.update(language=args.language, native_name=args.name, culture=args.culture, author=args.author)
    tables, review = {}, []
    imported = 0
    for entry in doc['entries']:
        if not entry['editable'] or entry['category'] != 'game_localization':
            continue
        values = set()
        missing = []
        for relative in entry['files']:
            if relative not in tables:
                file = args.data / relative
                tables[relative] = read_table(file) if file.is_file() else {}
            value = tables[relative].get(entry['text_id'])
            if value is None:
                missing.append(relative)
            else:
                values.add(value)
        reason = 'missing tables or text ID' if missing else 'different translations in shared locations' if len(values) != 1 else ''
        text = next(iter(values)) if len(values) == 1 else ''
        if not reason and tokens(text) != entry['required_tokens']:
            reason = 'replace legacy phone-button references with the required PC tokens'
        if reason:
            review.append(dict(id=entry['id'], source=entry['source'], found=sorted(values), reason=reason, missing=missing))
        elif text and text != entry['source']:
            entry['text'] = text
            imported += 1
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(doc, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    report = args.out.with_suffix('.review.json')
    report.write_text(json.dumps(review, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(f'Converted {imported} translated entries; {len(review)} entries need review. Runtime labels remain in the JSON for manual translation.')


if __name__ == '__main__':
    main()
