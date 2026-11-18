# Metal Gear Solid Mobile · Patriots PC v5.1.0

## Community language packs

The launcher now has a **LANGUAGES / DİLLER** page. Import a launcher JSON, game JSON, or a ZIP containing both. New languages appear in the selector. Each component can be updated separately; untranslated text falls back to English.

- **294 launcher texts**, separated from **1,936 game/reference/runtime records**.
- Stable IDs, editable `text` fields, UTF-8 game-table compilation and protected dynamic-button tokens.
- Keyboard/gamepad prompts, live rebinding, shared saves and Discord room/status activity remain available in imported languages.
- Translation template export, component progress and optional per-language bitmap font groups.
- Original tables/fonts and a converter for already translated DATs in the Translation Kit.

**English and Turkish remain the completed bundled languages. Brazilian Portuguese files are blank contributor templates.** Send the completed launcher/game JSONs back for review before Portuguese is bundled as an official third language.

## Downloads

- **`MGS_Mobile_Patriots_PC_v5.1.zip`** — complete Windows x64 package. Extract the full folder and run `Metal Gear Solid Mobile.exe`.
- **`MGS_Mobile_Patriots_PC_v5.1_Guncelleme.zip`** — update for the existing v5.0 package. Close the game and launcher, extract over their folder, and replace matching files. This update contains no personal save or preference files.
- **`MGS_Mobile_Translation_Kit_v5.1.zip`** — separate `launcher/` and `game/` JSONs, original tables, font references, tools and guides. Translating/importing JSON requires no development tools; advanced DAT/font conversion uses Python.
- Matching `.sha256` files verify each archive.

**For the Brazilian Portuguese translator:** start with [pt-BR.launcher.json](https://github.com/TheAti1/metal-gear-solid-mobile-patriots/blob/main/localization/launcher/pt-BR.launcher.json), edit only `text`, and preserve placeholders. The [game JSON](https://github.com/TheAti1/metal-gear-solid-mobile-patriots/blob/main/localization/game/pt-BR.game.json) is separate. [Translation guide](https://github.com/TheAti1/metal-gear-solid-mobile-patriots/blob/main/docs/TRANSLATING.md) · [Türkçe rehber](https://github.com/TheAti1/metal-gear-solid-mobile-patriots/blob/main/docs/TRANSLATING.tr.md).

## Türkçe

**DİLLER** sekmesinden launcher ve oyun çevirilerini ayrı ayrı veya birlikte içe aktarabilirsiniz. JSON'larda yalnızca `text` alanları çevrilir; tuş işaretleri korunur. Eksik metinler İngilizce açılır. Oyun kayıtları, kontroller ve Discord desteği korunur. Portekizce şablon hazırdır; çeviri tamamlanıp incelendikten sonra üçüncü resmî dil olarak eklenecek.

## Validation

Launcher build and isolated importer checks passed, including separate/ZIP components, UTF-8 DAT rebuilding, source/ID/token validation, English fallback, cache reload and rejected-import preservation. Native prompt checks passed in Turkish, English and an isolated test locale. A real native boot showed translated menu text, loading and Discord telemetry. The synthetic test locale and personal saves are excluded from the distribution.

The runtime executes the original Symbian/N-Gage ARM game through Unicorn, based on mgsm; this update does not change that architecture.
