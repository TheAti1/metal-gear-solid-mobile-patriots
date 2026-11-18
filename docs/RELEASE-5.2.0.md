# Metal Gear Solid Mobile · Patriots PC v5.2.0

## Sharp UI with high-resolution gameplay — #1

**Display → Quality → Crisp Interface** is enabled by default. Bitmap menus, dialogue and HUD draws use nearest atlas sampling independently of 3D texture filtering. Their draw scope suppresses MSAA coverage smoothing and restores both texture units and the original AA state afterwards. World rendering keeps its resolution, filtering and AA settings.

For sharp UI and high-resolution gameplay together, keep **Crisp Interface on** and **Original Pixel Resolution off**. The latter intentionally lowers the entire game scene to the phone's resolution; it is a separate classic/compatibility option. High Quality and Balanced presets enable Crisp Interface.

## Optional inventory scrolling inversion — #2

**Controls → Invert weapon / equipment menu Up and Down** reverses vertical navigation only while the inventory state is active. It applies after physical input is mapped to phone actions, so keyboard, D-pad and left stick use the same option. Movement, pause, Codec and front-end menus retain their controls. The setting defaults off, is saved, and can be changed while playing. Both new settings are also available in the native F1 overlay.

## Updated translator files

The launcher JSON now contains **299 texts**, adding **5 entries**. All previous IDs and source strings are preserved. The game JSON remains **1,936 records**. Existing 5.1 translations still import and use English for the new settings until updated.

`tools/refresh_language_json.py` upgrades an already translated JSON to the current full template, preserving completed text and contributor metadata. A non-importable `changes-5.2.json` lists the five additions. Do not overwrite completed translations with the blank template.

English and Turkish remain the completed bundled languages; Brazilian Portuguese is a contributor template pending the actual reviewed translation.

## Downloads

- **`MGS_Mobile_Patriots_PC_v5.2.zip`** — complete Windows x64 package.
- **`MGS_Mobile_Patriots_PC_v5.2_Guncelleme.zip`** — update for v5.0/v5.1. Close the launcher/game, extract into its existing folder and replace matching files. No personal save, preference or native INI is included. Back up any edited translation templates before replacing them.
- **`MGS_Mobile_Translation_Kit_v5.2.zip`** — refreshed separate launcher/game JSONs, original tables/fonts, upgrade/conversion tools and guides.
- Matching `.sha256` files for integrity verification.

[Launcher JSON](https://github.com/TheAti1/metal-gear-solid-mobile-patriots/blob/main/localization/launcher/pt-BR.launcher.json) · [Game JSON](https://github.com/TheAti1/metal-gear-solid-mobile-patriots/blob/main/localization/game/pt-BR.game.json) · [Translation guide](https://github.com/TheAti1/metal-gear-solid-mobile-patriots/blob/main/docs/TRANSLATING.md)

## Türkçe

**Görüntü → Kalite → Keskin arayüz** açıkken yazı ve menüler, 3B sahnenin yumuşatmasından bağımsız çizilir. Yüksek çözünürlüklü oynanış için **özgün piksel çözünürlüğünü kapalı** tutun. **Kontroller** altında yalnızca silah/ekipman menüsünü etkileyen yukarı-aşağı tersleme eklendi; oyun açıkken de değiştirilebilir.

Çeviri JSON'u **299 launcher metnine** güncellendi. Önceki 294 metnin kimlikleri korunur; 5 yeni alan eklendi. Oyun JSON'u değişmedi. Tamamlanmış çevirileri `refresh_language_json.py` ile yeni şablona taşıyabilirsiniz.
