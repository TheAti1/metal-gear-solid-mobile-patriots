"""Regression checks for carrying existing translations into a newer catalogue."""
import copy
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]


class UpgradeTests(unittest.TestCase):
    def test_existing_translation_and_game_component(self):
        for component in ('launcher', 'game'):
            template = ROOT / f'localization/{component}/{component}.en.json'
            doc = json.loads(template.read_text(encoding='utf-8-sig'))
            old = copy.deepcopy(doc)
            old.update(language='pt-BR', native_name='Português (Brasil)', culture='pt-BR', author='Test translator')
            if component == 'launcher':
                old['entries'] = old['entries'][:294]
            row = next(e for e in old['entries'] if e['editable'] and not e['required_tokens'])
            row['text'] = 'Tradução: çãõ / Türkçe ıİşğ'
            with tempfile.TemporaryDirectory() as folder:
                source, output = Path(folder) / 'old.json', Path(folder) / 'new.json'
                source.write_text(json.dumps(old, ensure_ascii=False), encoding='utf-8')
                result = subprocess.run([sys.executable, str(ROOT / 'tools/refresh_language_json.py'), '--input', str(source), '--template', str(template), '--out', str(output)], capture_output=True)
                self.assertEqual(result.returncode, 0, result.stderr)
                updated = json.loads(output.read_text(encoding='utf-8'))
                self.assertEqual(updated['author'], old['author'])
                self.assertEqual(updated['language'], 'pt-BR')
                self.assertEqual(len(updated['entries']), len(doc['entries']))
                by_id = {e['id']: e for e in updated['entries']}
                for entry in old['entries']:
                    self.assertEqual(by_id[entry['id']], entry)
                for entry in updated['entries']:
                    if entry['id'] not in {e['id'] for e in old['entries']}:
                        self.assertEqual(entry['text'], '')

    def test_modified_source_is_not_silently_lost(self):
        doc = json.loads((ROOT / 'localization/launcher/pt-BR.launcher.json').read_text(encoding='utf-8-sig'))
        doc['entries'][0]['source'] = 'Changed source'
        with tempfile.TemporaryDirectory() as folder:
            source, output = Path(folder) / 'old.json', Path(folder) / 'new.json'
            source.write_text(json.dumps(doc), encoding='utf-8')
            result = subprocess.run([sys.executable, str(ROOT / 'tools/refresh_language_json.py'), '--input', str(source), '--out', str(output)], capture_output=True)
            self.assertNotEqual(result.returncode, 0)
            self.assertFalse(output.exists())


if __name__ == '__main__':
    unittest.main()
