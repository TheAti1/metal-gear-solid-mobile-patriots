"""Check stable IDs, protected tokens and the embedded/importable catalogues."""
import hashlib
import json
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]


def tokens(text):
    return sorted(re.findall(r'\{[^{}]+\}|%(?:\d+\$)?[-+#0 .\d]*[sdifuoxX]', text))


def main():
    english = json.loads((ROOT / 'launcher/launcher.en.json').read_text(encoding='utf-8-sig'))
    expected = {'ui_' + hashlib.sha256(k.encode()).hexdigest()[:20]: v for k, v in english.items()}
    for component in ('launcher', 'game'):
        folder = ROOT / 'localization' / component
        template = json.loads((folder / f'{component}.en.json').read_text(encoding='utf-8-sig'))
        rows = template['entries']
        assert len({e['id'] for e in rows}) == len(rows), 'duplicate IDs'
        assert all(e['required_tokens'] == tokens(e['source']) for e in rows), 'token metadata mismatch'
        if component == 'launcher':
            assert {e['id']: e['source'] for e in rows} == expected, 'launcher catalogue drift'
        for path in folder.glob('*.json'):
            doc = json.loads(path.read_text(encoding='utf-8-sig'))
            assert doc['format'] == template['format'] and doc['template_version'] == 1
            canonical = {e['id']: e for e in rows}
            for entry in doc['entries']:
                original = canonical[entry['id']]
                assert {k: v for k, v in entry.items() if k != 'text'} == {k: v for k, v in original.items() if k != 'text'}, path
                if entry['text']:
                    assert tokens(entry['text']) == entry['required_tokens'], path
        print(f'{component}: {len(rows)} entries, stable IDs and tokens PASS')


if __name__ == '__main__':
    main()
