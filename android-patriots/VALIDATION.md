# Android 1.0.0 validation

- NDK 29 release builds for arm64-v8a, armeabi-v7a and x86_64.
- APK v2/v3 signature verification; ZIP CRC; 16 KB ZIP and ELF LOAD alignment.
- Android 15/API 35 emulator: installation, Turkish native title/menu, virtual direction-pad confirmation, new-game transition and the opening 3D room.
- The GLES1 overlay now tracks renderer state and matrices: it restores texture bindings, color/depth writes, capabilities and transforms without incomplete GLES1 driver queries or limited hardware matrix stacks.
- Pure Java update-policy tests cover Android/PC channel isolation, version ordering, trusted release URLs and SHA256 syntax.
- Game saves are excluded from bundled assets and are not overwritten during asset refresh. The game runs separately from the launcher process.

Physical ARM phones, every mission and every controller have not been tested. Performance depends on the phone and OpenGL driver. This is an initial Android build; emulator validation does not certify all devices.
