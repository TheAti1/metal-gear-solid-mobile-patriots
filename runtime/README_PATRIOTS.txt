PATRIOTS MGS MOBILE 5.0 — MODIFIED NATIVE RUNTIME SOURCE
Upstream: https://github.com/LeithSilver/mgsm
Base commit: 4e6d91ceadfb898322f535f82f24a394197d892b (v0.1)
Original code: GPL-2.0-or-later; full license remains in LICENSE.
Patriots changes: prompts.cpp/h, prompt_rules.inc, patriots_state.cpp/h,
room_names.inc, input notifications, Lua ReturnField, INI options, Windows build.
No original proprietary game assets are part of this source archive.

Windows x64 build used for this release:
1. Python 3, CMake and Ninja must be on PATH.
2. py -3 -m pip install unicorn==2.1.4
3. Download and extract LLVM-MinGW 20261104 UCRT x86_64.
   Exact URL and SHA256: patriots-build/compiler_asset.json
4. py -3 patriots-build/build_native.py --toolchain C:\path\llvm-mingw-20261104-ucrt-x86_64 --out C:\path\output
   Use --ninja C:\path\ninja.exe if Ninja is not on PATH.
5. Output contains mgs_mobile.exe, SDL3.dll and unicorn.dll. Copy into Oyun/.
   Preserve game/, mgs_mobile.ini, save files and the shipped VC runtime DLLs.

Launcher sources are separately provided in Belgeler/Kaynaklar, GPL-3.0-or-later.
Launcher build: powershell -ExecutionPolicy Bypass -File Belgeler/Kaynaklar/build.ps1
Discord uses the documented local IPC protocol; no account token is used.

5.0 adds startup language selection (language=tr/en), read-only locale overlays in
Oyun/languages/{tr,en}/data, bilingual key names, English prompt templates and room names.
Launcher English/Turkish selection and catalogue are in Belgeler/Kaynaklar.
