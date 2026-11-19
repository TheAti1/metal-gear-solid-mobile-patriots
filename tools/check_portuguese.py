"""Validate the bundled Brazilian Portuguese translation, including Android UI."""
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def main():
    documents = []
    for component in ('launcher', 'game'):
        path = ROOT / 'localization' / component / f'pt-BR.{component}.json'
        doc = json.loads(path.read_text(encoding='utf-8'))
        assert doc['language'].lower() == 'pt-br' and doc['culture'] == 'pt-BR'
        assert 'Sanju Vinho e Fumaça' in doc['author'] and 'steamcommunity.com/id/SanjiVinsmokeBRASIL/' in doc['author']
        active = [e for e in doc['entries'] if e['editable'] and e['category'] in ('launcher', 'game_localization', 'runtime')]
        assert all(e['text'].strip() and '\ufffd' not in e['text'] and '\0' not in e['text'] for e in active)
        assert len(active) == (299 if component == 'launcher' else 1862)
        documents.append(doc)
    strings = {e['source']: e['text'] for doc in documents for e in doc['entries'] if e['text']}
    strings.update(json.loads((ROOT / 'localization/android/pt-BR.android.json').read_text(encoding='utf-8'))['strings'])
    used = set()
    for name in ('HomeActivity', 'Updater'):
        java = (ROOT / f'android-patriots/java/com/patriots/shared/{name}.java').read_text(encoding='utf-8')
        for raw in re.findall(r'\bt\("(?:[^"\\]|\\.)*","((?:[^"\\]|\\.)*)"\)', java):
            used.add(json.loads('"' + raw + '"'))
    assert all(source in strings and strings[source] for source in used), sorted(used - strings.keys())
    print(f'Portuguese: 299 launcher + 1862 active game/runtime entries; {len(used)} Android messages PASS')


if __name__ == '__main__':
    main()
