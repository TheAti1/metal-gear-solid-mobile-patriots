# Android 1.0.1 validation

- NDK 29 release builds for arm64-v8a, armeabi-v7a and x86_64.
- APK v2/v3 signature verification; ZIP CRC; 16 KB ZIP and ELF LOAD alignment.
- Android 15/API 35 emulator: installation, Turkish native title/menu, virtual direction-pad confirmation, new-game transition and the opening 3D room.
- The GLES1 overlay now tracks renderer state and matrices: it restores texture bindings, color/depth writes, capabilities and transforms without incomplete GLES1 driver queries or limited hardware matrix stacks.
- Pure Java update-policy tests cover Android/PC channel isolation, version ordering, trusted release URLs and SHA256 syntax.
- Game saves are excluded from bundled assets and are not overwritten during asset refresh. The game runs separately from the launcher process.

Physical ARM phones, every mission and every controller have not been tested. Performance depends on the phone and OpenGL driver. This is an initial Android build; emulator validation does not certify all devices.

Shared updater integration: an older signed Mobile test build downloaded the live GitHub APK in HTTP ranges, verified it and completed Android permission/installer confirmation. The actual savedata.i3d SHA256 remained aa3cded8a5ca612f3b46007b8cf0eefceaa359c3f2c66a5269ce1c4fb6e964c1; the English language preference survived. Touch transferred over 40 MB through the same ranged client and retained its real SAVE.data through in-place APK installs.
