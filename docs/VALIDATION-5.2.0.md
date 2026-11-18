# v5.2.0 validation

Windows x64, 2026-11-18. Issues reviewed with all available comments and attached screenshots: [#1](https://github.com/TheAti1/metal-gear-solid-mobile-patriots/issues/1), [#2](https://github.com/TheAti1/metal-gear-solid-mobile-patriots/issues/2).

## Rendering

The previous texture filter preference was global, and MSAA applied to screen-space bitmap geometry as well as the world. Enabling original pixel resolution instead reduced the full render target, explaining why that workaround also reduced 3D detail.

The new 2D draw scope forces base-level nearest atlas sampling and suppresses MSAA for that draw, then restores the original sampler/AA state. The projection classification matches the runtime's existing screen-space HUD detection. It does not change the game's render-target dimensions, 3D geometry or source textures. Supersampling still resolves the final image; the original bitmap assets retain their native detail.

- Native LLVM-MinGW build passed.
- Real OpenGL state regression passed on Intel HD Graphics 620: two independent texture units, a texture shared by both units, nearest UI filters, original linear/mipmapped filters restored, original MSAA restored, and disabled-mode behavior.
- Native boot with 4× MSAA and 2× supersampling showed sharp dialogue/HUD while world rendering stayed at the high-resolution target. UI/world trace entries confirmed both paths in the same session.
- Clean English/Turkish launcher quality pages rendered and reviewed.

The opt-in GPU check tests GL state restoration. Visual checks use actual game framebuffers. It is not a full GPU-vendor compatibility suite.

## Inventory navigation

- The native policy check covers every guest key code, original/reversed modes, reversible Up/Down mapping and unchanged gameplay/pause/Codec/front-end states.
- A real native play session reached the weapon/equipment inventory. Logged reversed guest key queries included held, pressed and released bits, and the inventory screenshot retained dynamic PC-button text.
- Physical controller input was excluded from the isolated keyboard integration run using SDL's test-device allowlist; keyboard, controller D-pad and stick all feed the same guest-action mapping before this policy.
- Launcher service tests passed for new preference → native INI → preference round trips, live inversion command output, and v5.1 preference files defaulting Crisp Interface on/inversion off.
- Native prompt/inventory policy checks passed with English, Turkish and an independent locale ID.

## Localization and package

- Current templates: **299 launcher entries**, **1,936 game/reference/runtime records**.
- All 294 previous launcher IDs and English source values were preserved. Five new settings entries were appended; the game catalogue is unchanged.
- Isolated importer checks passed, including importing a 294-entry v5.1 launcher pack with English fallback for the new labels.
- Upgrade tool tests passed: existing launcher/game translations and credits preserved, new entries blank, modified protected source rejected without writing output.
- No synthetic locale, test save or personal settings is included in the clean release. Translation templates are editable working files and excluded from the immutable game-file integrity manifest.
