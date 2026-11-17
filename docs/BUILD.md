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
The English UI catalogue is embedded in the executable. Turkish strings are the source catalogue.

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
Discord activity is validated using IPC acknowledgements, including accepted external artwork. No account token is required by the game.

## Runtime settings

- `Language` in `Arayuz/ayarlar.json`: `tr` or `en`; the launcher's selection is authoritative.
- `language=tr/en` in `Oyun/mgs_mobile.ini`: the running game's language, written on launch.
- `dynamic_prompts=1`: keyboard / controller names in game text.
- `prompt_style=0/1/2/3`: automatic / Xbox / PlayStation / Nintendo.
- `discord_presence=1`, `discord_room=1`: presence and room visibility.

The language is fixed for a game session. Controls can be updated live through the launcher's atomic command file.
