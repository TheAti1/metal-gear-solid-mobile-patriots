# Windows build guide

## Dependencies

- Windows 10/11 x64 and .NET Framework 4.8 (WPF).
- Python 3.11+, CMake 3.16+ and Ninja.
- `unicorn==2.1.4`.
- Visual Studio 2022 C++ build tools + Windows SDK, or LLVM-MinGW 20261104 UCRT x86_64.

SDL3 headers and Windows development files are vendored in the upstream runtime source.

## Launcher

```powershell
powershell -ExecutionPolicy Bypass -File .\launcher\build.ps1
```

This writes `package/Metal Gear Solid Mobile.exe` and copies the launcher artwork.
The English UI catalogue and game import template are embedded in the executable. Turkish strings are the built-in UI source catalogue. `LanguagePacks.cs` and `LanguagePackChecks.cs` are part of the build; no external translation compiler is required to import a community pack.

To update a prepared local game package:

```powershell
powershell -ExecutionPolicy Bypass -File .\launcher\build.ps1 -ReleaseRoot 'C:\Games\MGS Mobile'
```

## Native runtime — MSVC

Run in an x64 Visual Studio developer shell:

```powershell
python -m pip install unicorn==2.1.4 ninja
cmake -S runtime/mgs_native -B build/runtime -G Ninja -DCMAKE_BUILD_TYPE=Release -DMGS_OUTPUT_DIR="$PWD/package/Oyun"
cmake --build build/runtime --parallel 4
```

## Native runtime — LLVM-MinGW

The compiler URL and SHA256 are pinned in `runtime/patriots-build/compiler_asset.json`.

```powershell
python -m pip install unicorn==2.1.4
python runtime/patriots-build/build_native.py --toolchain 'C:\Tools\llvm-mingw-20261104-ucrt-x86_64' --out package/Oyun
```

The output contains `mgs_mobile.exe`, `SDL3.dll` and `unicorn.dll`. A prepared package also carries the VC runtime DLLs required by Unicorn.

## Preparing language assets

Use the original English game data produced by the upstream installer and your prepared Turkish game data:

```powershell
python tools/prepare_languages.py --english-data 'C:\MGS-English\game\data' --turkish-data 'C:\MGS-Turkish\game\data' --package package
```

Each language supplies fourteen `english*.dat` tables below `Oyun/languages/{en,tr}/data/localisation`.
The filename stays `english.dat` because it is an internal game lookup name. The runtime's VFS selects the right overlay from the launcher preference.
Shared assets and writable saves stay in `Oyun/game/data`. Use the prepared release package for those files when rebuilding locally.

## Checks

The catalogue check needs no game data:

```powershell
& '.\package\Metal Gear Solid Mobile.exe' --test-localization
python tools/check_language_templates.py
python tools/test_language_upgrade.py
& '.\package\Metal Gear Solid Mobile.exe' --test-language-packs
$env:MGS_PATRIOTS_SELFTEST='1'
& '.\package\Oyun\mgs_mobile.exe'
Remove-Item Env:MGS_PATRIOTS_SELFTEST
```

Against a complete prepared package:

```powershell
& '.\package\Metal Gear Solid Mobile.exe' --self-test
& '.\package\Metal Gear Solid Mobile.exe' --test-services
& '.\package\Metal Gear Solid Mobile.exe' --render-bilingual
& '.\package\Metal Gear Solid Mobile.exe' --test-game
```

UI preview generation exercises the actual language selector through Turkish → English → Turkish. Game automation uses the runtime's own input and framebuffer hooks.
The language-pack check forks an isolated temporary launcher, generates synthetic English tables and tests UTF-8 compilation, separate/component ZIP imports, token/ID validation, rollback, cache reload and English fallback. It does not use or alter user game saves.
Discord activity is validated using IPC acknowledgements, including accepted external artwork. No account token is required by the game.

## Runtime settings

- `Language` in `Arayuz/ayarlar.json`: `tr`, `en`, or an installed community tag such as `pt-br`; the launcher's selection is authoritative.
- `language=<tag>` in `Oyun/mgs_mobile.ini`: the resolved game language, written on launch. A launcher-only locale resolves to `en` for the game.
- `dynamic_prompts=1`: keyboard / controller names in game text.
- `crisp_ui=1`: default; nearest sampling and no per-draw MSAA for 2D font/sprite atlases. 3D resolution and texture filters are preserved.
- `invert_inventory_scroll=0/1`: original/reversed vertical input in the inventory only; shared across physical devices and updated live.
- `prompt_style=0/1/2/3`: automatic / Xbox / PlayStation / Nintendo.
- `discord_presence=1`, `discord_room=1`: presence and room visibility.

The language is fixed for a game session. Controls can be updated live through the launcher's atomic command file.

See [TRANSLATING.md](TRANSLATING.md) for importable JSON, bitmap-font overlays and contributor tools. Community packs live outside shared assets and the built-in English/Turkish folders. Review and bundle a finished third translation before marking its ID official.

With a prepared local game, `MGS_RENDER_SELFTEST=1` runs real OpenGL sampler/MSAA restoration checks on the first guest draw. `MGS_TRACE_UI=1` logs the first UI/world draw and render-target sizes; `MGS_TRACE_INVENTORY=1` logs state changes. These optional diagnostics are not enabled in distribution defaults.
