"""Build Portuguese HUD glyphs from the original bitmap lettering, losslessly.

Input is the prepared Patriots game/data tree. Only locale-specific font group
overlays are written; shared assets and the original glyph pixels are preserved.
"""
import argparse
import json
import struct
import unicodedata
from pathlib import Path
from PIL import Image, ImageDraw
from fonts import font_records, glyph_image, u32
from build_turkish_fonts import patch_font, render_text


def accent(f, ch, base):
    im = glyph_image(f, ord(base)).copy()
    left, top, right, bottom = im.getbbox()
    fg = max(set(im.getdata()), key=lambda i: f['palette'][i][3])
    mark = unicodedata.normalize('NFD', ch)[1:]
    center = (left + right - 1) // 2
    # The existing bitmap accents use a two-row mark. If an uppercase stem
    # already touches the top, lower only this new glyph by the needed rows.
    if top < 3:
        shift = min(3 - top, im.height - bottom)
        shifted = Image.new('L', im.size)
        shifted.paste(im, (0, shift))
        im = shifted
        top += shift
    y = max(0, top - 3)
    strokes = {
        '\u0301': [(1, 0), (0, 1)],
        '\u0300': [(-1, 0), (0, 1)],
        '\u0302': [(0, 0), (-1, 1), (1, 1)],
        '\u0303': [(-1, 0), (0, 0), (1, 1), (2, 1)],
    }
    if mark not in strokes:
        raise ValueError(f'Unsupported accent: {ch!r}')
    for dx, dy in strokes[mark]:
        im.putpixel((max(0, min(im.width - 1, center + dx)), min(im.height - 1, y + dy)), fg)
    return im


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--data', type=Path, required=True)
    parser.add_argument('--game-json', type=Path, required=True)
    parser.add_argument('--out', type=Path, required=True)
    args = parser.parse_args()
    doc = json.loads(args.game_json.read_text(encoding='utf-8-sig'))
    text = ''.join(e['text'] or e['source'] for e in doc['entries'] if e['category'] == 'game_localization')
    needed = {c for c in text + text.upper() if ord(c) > 127 and unicodedata.category(c).startswith('L')}
    report = []
    previews = []
    for relative in ('common/fixed.group.bin', 'common/loading.group.bin', 'common/ui/hud.group.bin', 'common/codec/codec.group.bin'):
        original = (args.data / relative).read_bytes()
        changes = []
        for f in font_records(original):
            if ord('A') not in f['cmap']:
                continue
            pairs = [(c, unicodedata.normalize('NFD', c)[0]) for c in sorted(needed) if ord(c) not in f['cmap']]
            if not pairs:
                continue
            assert all(ord(base) in f['cmap'] for _, base in pairs)
            result, detail = patch_font(original, f, pairs, [accent(f, c, base) for c, base in pairs])
            changes.append((f['start'], f['end'], result))
            report.append(dict(group=relative, **detail))
        if not changes:
            continue
        patched = bytearray(original)
        for start, end, result in sorted(changes, reverse=True):
            patched[start:end] = result
        delta = len(patched) - len(original)
        at = 6
        while at + 4 <= len(original):
            if not u32(original, at):
                break
            size = u32(original, at + 4)
            end = at + 8 + size - 4
            if any(at + 8 <= start < end for start, _, _ in changes):
                struct.pack_into('<I', patched, at + 4, size + delta)
            at = end
        old_fonts, new_fonts = font_records(original), font_records(patched)
        assert len(old_fonts) == len(new_fonts)
        for old, new in zip(old_fonts, new_fonts):
            if ord('A') not in old['cmap']:
                continue
            assert all(ord(c) in new['cmap'] for c in needed)
            for cp in old['cmap']:
                assert glyph_image(old, cp).tobytes() == glyph_image(new, cp).tobytes(), cp
            if new['count'] != old['count']:
                previews.append((f'{relative} / {new["name_hash"]:08x}', render_text(new, 'AÇÃO MISSÃO OPÇÕES ÀÁÂÃ Ç ÉÊ Í ÓÔÕ Ú / ação missão opções àáâã ç éê í óôõ ú')))
        target = args.out / 'data' / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(patched)
    args.out.mkdir(parents=True, exist_ok=True)
    (args.out / 'font-verification.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
    if previews:
        canvas = Image.new('RGB', (max(im.width for _, im in previews) * 4 + 20, sum(im.height * 4 + 30 for _, im in previews)), '#0c1013')
        draw = ImageDraw.Draw(canvas)
        y = 0
        for label, im in previews:
            draw.text((8, y), label, fill='white')
            y += 24
            im = im.resize((im.width * 4, im.height * 4), Image.Resampling.NEAREST)
            canvas.paste(im, (8, y), im)
            y += im.height + 6
        canvas.save(args.out / 'font-preview.png')
    print(f'Portuguese font overlays: {len(report)} fonts; all original glyph pixels retained.')


if __name__ == '__main__':
    main()
