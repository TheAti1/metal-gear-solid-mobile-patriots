# Patriots Android · Mobile

Android APK v1.1.1, Android 9/API 28 or newer. The distributed universal APK includes arm64-v8a, armeabi-v7a and an x86_64 build for emulators/compatible devices. Actual performance depends on the device and OpenGL driver.

## Play

Install the APK and open the Patriots launcher. Choose Turkish/English/Brazilian Portuguese and press PLAY. Game data is bundled; no emulator setup or separate ROM installation is required. Touch fits the original view into the actual window and presents landscape scenes upright with matching touch coordinates. Mobile provides a circular virtual analog stick, separate confirm button and minimal action icons. Saves are stored separately from immutable game assets.

The launcher checks GitHub Releases on startup (can be disabled) and has a manual check button. A newer **android-vMAJOR.MINOR.PATCH** release with the correctly named APK triggers a download prompt. The app verifies the asset SHA256, app package, increasing versionCode and signing certificate before opening Android's installer. Android installation permission/confirmation still belongs to the user. Offline/check errors do not block PLAY.

**Pushing a source commit alone does not update a phone. Publish a newly built and signed APK release.** PC version tags/assets are ignored. Keep the signing key: losing/changing it prevents an in-place update and save-preserving replacement.

## Build prerequisites (Windows)

- JDK 21 (`JAVA_HOME`), Python 3.11+ + Pillow, CMake/Ninja on PATH.
- Android SDK platform 35, build-tools 35.0.0, NDK 29.0.14206865 (`ANDROID_HOME`).
- `PATRIOTS_ANDROID_WORK` defaults to `D:/Patriots_Android`. It holds caches/dependencies/signing material, never inside Git.
- A prepared Windows game package in `PATRIOTS_PC_PACKAGE` (default `package/`), providing the locally supplied game assets. No game bundle is committed here.
- SDL3-3.4.18 and unicorn-2.1.4 sources under `PATRIOTS_ANDROID_WORK/deps/` for Mobile; source URLs/checksums remain pinned in `runtime/android/build.py`.
- Mobile's Unicorn configure needs a POSIX shell/strings; default scripts use MSYS2 at `C:/msys64/usr/bin`. Build Windows-compatible shims for `strings`/`pkg-config` if needed. On Windows Unicorn's archive post-build symlink may be replaced with a regular copy; no administrator permission is needed.
- Touch needs the locally recovered `mgst_w2c.c` / matching headers (`PATRIOTS_TOUCH_GEN`), WABT wasm2c runtime (`PATRIOTS_WABT`) and FreeType 2.13.3 sources (`PATRIOTS_FREETYPE`). Defaults refer to the existing `D:/Patriots_MGS_Touch_PC` developer workspace. Recovered original game C stays local.

```powershell
$env:JAVA_HOME='C:\Program Files\Java\jdk-21.0.10'
$env:ANDROID_HOME='D:\Patriots_Android\sdk'
$env:PATRIOTS_ANDROID_WORK='D:\Patriots_Android'
$env:PATRIOTS_PC_PACKAGE='C:\Games\MGS Mobile'
$env:PATRIOTS_ANDROID_ABIS='arm64-v8a,armeabi-v7a,x86_64'
python android-patriots/build.py 1.1.1
```

The builder writes the APK and SHA256 to `outputs/Android/`, verifies APK signatures and 16 KB native-library alignment, and uses the stable private key in the work folder's `signing/`. **Back up that folder privately. Do not upload it.** The Android launcher/updater is MIT-licensed; native runtime component licenses remain as documented in the repository.

## Publish an update

1. Choose a three-part version higher than the newest published Android release. Substitute that number for `<version>` in `python android-patriots/build.py <version>` and use the same private key.
2. Create a non-draft, non-prerelease GitHub release tagged `android-v<version>` with exactly the APK's version number. It need not become the latest PC release.
3. Upload `MGS_Mobile_Patriots_Android.apk` plus its `.apk.sha256` sidecar. Keep the filename stable between updates.
4. On next launcher startup, installed older versions offer the new APK. Game assets may be refreshed but personal save files/settings are not part of the update payload.

## Checks

`UpdatePolicyTest.java` covers Android tag ordering, PC-channel exclusion, trusted asset URLs and checksum syntax. Compile/run it with `UpdatePolicy.java` using javac/java. Android device/emulator checks cover launcher, native boot, save preservation and package installation separately. APK signature/ZIP checks are build-time checks; they do not establish performance on every physical phone.

The Android GLES1 renderer shadows overlay state and matrix stacks, and the touch action button confirms menus as well as performing its gameplay action. The game runs in a separate process so engine shutdown does not terminate the launcher. Large updates use checked 4 MiB ranges with bounded connection retries.

Automatic checks also run when the launcher returns to the foreground, at most once per minute. Returning from installation permission settings resumes the pending install without showing another update offer.

Brazilian Portuguese uses the reviewed PC translation and locale-specific font overlay. See [coverage and contributor credit](../docs/PORTUGUESE.md). The Android launcher catalogue combines that JSON with localization/android/pt-BR.android.json; packaging refuses a missing Portuguese data folder.

## PC-style launcher and tablets

The native Android launcher reuses the respective PC artwork/palette. Touch uses a dark sidebar on wide windows; Mobile uses light top navigation. Compact windows use horizontally scrollable tabs. The scrollable page and fixed PLAY footer keep controls reachable after rotation, resizing and larger text. No portrait compatibility lock or fixed maximum aspect ratio is applied to HomeActivity. Mobile gameplay remains landscape; Touch portrait menus retain their original proportions while landscape scenes rotate in the renderer.

Display, touch/controller help, audio, language/credit, save export and updates have separate pages. Settings are saved automatically and applied at game launch. Existing native settings are imported when their file changes. Android controls and permissions follow Android APIs; Windows-specific Discord IPC and Windows key remapping are not presented as Android features. See [layout validation](../docs/ANDROID-LAYOUT.md).

### Virtual analog / buttons

The stick follows the thumb with a clamped circular knob, radial dead zone and hysteresis. Precise direction uses the existing movement hook; original animation-driven movement speed is retained. The checkmark separately sends Center/OK, the crosshair sends Action (and confirms menus), the eye sends first-person view, the bag opens equipment, and the pause button is at the top right. The new layout is applied once when upgrading; subsequent manual placements remain editable in the in-game Touch settings. Touch cancellation, focus loss and resize release held input. Keyboard/gamepad inputs remain available.
