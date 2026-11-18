# v5.1.0 validation

Performed on Windows x64, 2026-11-18.

| Check | Result |
|---|---|
| WPF launcher build, .NET Framework x64 | Passed |
| Native C++ build, LLVM-MinGW | Passed |
| Template IDs, immutable fields and protected tokens | 294 launcher / 1,936 game-reference-runtime records passed |
| Isolated JSON importer | Launcher-only and game component imports passed |
| Combined ZIP | Both components installed and existing translations preserved |
| UTF-8 DAT round trip | Translated text read back from compiled tables |
| Invalid import | Duplicate IDs, changed source, protected records, lost tokens and path traversal rejected |
| Rejected import preservation | Existing game-table hash unchanged |
| Update/cache | Reimported UI updated immediately and game tables preserved |
| English fallback | Missing component and corrupt installed UI catalogue passed |
| Native prompts | Keyboard, Xbox, PlayStation, live rebind, explicit `{pc:...}` tokens and Loading passed in en/tr/test locale |
| Real native boot | Test locale showed NOVO JOGO / OPÇÕES, Carregando... and translated native Discord status telemetry |
| Existing DAT converter | Recovered 7 nonempty translated entries from test tables with no lost tokens |
| UI render | English and Turkish pages, including Languages, rendered and reviewed |
| Existing services | Native config round trip, isolated saves and SDL initialization passed |
| Clean package self-test | Required game files and selected built-in tables passed |

The deliberately partial `pt-qa` fixture is an isolated test, not a Portuguese release. It is excluded from the distributed language folders. This change checks native presence telemetry; the existing Discord IPC implementation is retained. Full Brazilian Portuguese text, small-font coverage and layout review await the actual contributor files.
