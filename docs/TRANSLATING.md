# Add a language

Version 5.1 imports community translations from the launcher's **LANGUAGES** page. English and Turkish are the bundled, reviewed languages. The Brazilian Portuguese files below are **untranslated templates**, ready for a contributor; they do not claim completed Portuguese support.

## Files to send to a translator

- [Brazilian Portuguese launcher JSON](../localization/launcher/pt-BR.launcher.json): **294** entries, including settings, messages and the language importer.
- [Brazilian Portuguese game JSON](../localization/game/pt-BR.game.json): **1,936** records. This contains 1,814 editable game strings, 48 PC runtime / Discord labels, 70 legacy phone metadata records for reference, and 4 protected technical records.
- [English launcher template](../localization/launcher/launcher.en.json) and [English game template](../localization/game/game.en.json) for other languages.

The downloadable **Translation Kit** also includes the 14 original English text tables, original and Turkish-extended font groups, and font inspection/editing tools. These game assets are in the release kit, outside Git history. The complete game package keeps shared assets in `Oyun/game/`, language overlays in `Oyun/languages/`, and translator files in `Translations/launcher/` and `Translations/game/`.

## Translate

1. Set `language` to a unique tag such as `pt-BR`, `native_name` to `Português (Brasil)`, `culture` to `pt-BR`, and `author` to your credit. Installation normalizes the tag to lowercase.
2. Translate **only `text`** inside each entry. `source` is the English reference. Preserve `id`, `category`, `editable`, `text_id`, `files` and `required_tokens`. Keep technical records unchanged.
3. Preserve every placeholder, including `{pc:a}`, `{pc:c}`, `{pc:pause}`, `{0}`, `%s`, and intentional line breaks. `{pc:...}` resolves to the player's current keyboard or controller binding at runtime; do not replace it with a fixed Xbox/Nokia key.
4. Save as UTF-8 JSON. A blank `text` uses the English source. Partial packs can be tested and updated without completing every entry.

Game coverage counts the 1,862 editable game/runtime entries. The 70 old N-Gage installer/phone metadata entries are archival references and are not used by this Windows runtime. Fonts and native engine records marked `editable: false` are not translatable strings.

## Import and test

Close the running game, open **LANGUAGES → Import language pack**, and choose the launcher JSON or game JSON. They can be installed independently, in either order. Use the same language tag for both. The language appears in the header selector immediately; game text changes on the next launch.

- A launcher-only pack uses English game text until its game component is installed.
- A game-only pack uses English launcher text.
- Blank entries fall back to English. Saves and controls are shared across languages.
- Reimporting one component preserves the other component. Invalid imports leave the installed pack intact.
- Dynamic keyboard/gamepad prompts and Discord IPC remain active. Runtime room/status translations live in the game JSON's `runtime` category; untranslated names use English.

To import both together, ZIP these files **directly at the archive root**:

```text
launcher.json
game.json
fonts/                         # optional, only the four paths below
  common/fixed.group.bin
  common/loading.group.bin
  common/ui/hud.group.bin
  common/codec/codec.group.bin
```

The ZIP contains no executable scripts. Extra paths and modified protected tokens are rejected. Installed JSON lives in `Arayuz/Languages/<tag>/`; compiled DATs, runtime strings and optional fonts live in `Oyun/languages/<tag>/`. English/Turkish files are preserved.

The launcher can also export fresh JSON templates to a folder. The included full Translation Kit supplies additional font/table references. For automation:

```powershell
& '.\Metal Gear Solid Mobile.exe' --import-language '.\pt-BR.launcher.json'
& '.\Metal Gear Solid Mobile.exe' --import-language '.\pt-BR.game.json'
& '.\Metal Gear Solid Mobile.exe' --export-language-kit 'C:\Translations\MGS'
```

## Already translated the DAT files?

Use Python 3 to convert the translated `data` directory into a game JSON. The game and launcher do not require Python to import/play.

```powershell
python tools/game_pack_from_dat.py --data 'C:\TranslatedGame\data' --language pt-BR --name 'Português (Brasil)' --culture pt-BR --author 'Your name' --out pt-BR.game.json
```

If using the standalone kit, pass `--template game/game.en.json` from the kit root. Read the generated `.review.json`: conflicting/missing rows and legacy phone-button instructions needing `{pc:...}` tokens are left blank for manual review. Runtime labels remain in the game JSON for translation. Send both the finished launcher JSON and game JSON back to the maintainer; original translated DATs are also useful for comparison.

## Fonts and layout

The main original fonts already include Brazilian Portuguese accented Latin characters. Small specialized HUD/Codec fonts and long text still require in-game review. The kit's `fonts.py`, `archive_io.py` and `build_turkish_fonts.py` inspect/rebuild bitmap groups; the Turkish tool is a worked example, not a universal font generator. Include edited groups at the optional ZIP paths above. Launcher text uses WPF/Windows fonts.

Other alphabets may need additional glyphs and runtime rendering work, especially scripts requiring shaping or right-to-left layout. JSON import alone does not guarantee font coverage. Test dialogue, menu widths, accents, Loading, keyboard/gamepad switching and Discord room text before submitting a finished pack.

## Becoming a bundled language

Community imports are available now. A new official language is bundled after the completed launcher/game translation, credits, tokens, fonts and layouts have been reviewed. Brazilian Portuguese is intended as the next language once the contributor's finished files arrive. Review and bundle those actual files before adding `pt-br` to `OfficialIds` in `launcher/LanguagePacks.cs`; never label a blank template as finished support.
