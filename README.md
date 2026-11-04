# GlassPad — Queen's Domain Windows pilot

Editable touchscreen controls for the original Legion Go. Fork of
[dansunmi/GlassPad](https://github.com/dansunmi/GlassPad), base
`94fd8c46610a5c614dc938f06648e1f7a30c7d4c`.
[Original README](docs/UPSTREAM-README.md).

Floating movement, velocity-based XInput swipe look, attack-drag look, an Actions
radial, equipment popup, manual menu mode, per-game profiles, editor and native
tablet gyro are implemented. Arjun approved the layout conditional on editable
position, size and opacity. The actual editor's Save/Cancel, resize, position and
both opacity levels are tested. The original draft profile remains separate.

The self-contained x64 package runs through `Launch-GlassPad.cmd`; no development
SDK or separate .NET runtime is required. This pilot uses the **existing ViGEm
driver**. It never installs drivers or changes Handheld Companion, HidHide,
Steam Input, controller firmware, graphics/TDP settings or the original GlassPad
layout. Use a borderless/windowed game.

Queen's Domain can ignore secondary XInput players. The optional game-local
`xinput1_3.dll` routes this pilot's own pad to its first player while its heartbeat
is fresh, forwards other calls to System32 XInput, and restores the original route
when the pilot stops. No game executable patch or global remapping is used. The
installer verifies the exact game executable and refuses to overwrite unknown DLLs.

Use Actions → More → Customize Controls. Drag controls, resize them, adjust
per-control/global opacity, change action/icon assignment, show/hide, promote
radial actions to direct buttons, edit zones and sensitivity, import/export,
Save/Cancel or restore defaults. Profiles live only in
`%AppData%\GlassPad-Queens-Pilot\profiles`, with stable IDs and atomic backups.

Movement anchors where a finger begins and vanishes on release. Swipe look uses
source timestamps and a separate expiry clock; stationary displacement does not
keep turning. Hold either attack and drag to look without releasing it. Jump,
Wind Dash and Interact are direct. Radial held actions open a Hold row so a
cancelled selection never fires an attack. Equipment entries cycle; no direct
weapon-slot bindings were invented.

Ctrl+Alt+G hides/recovers, Ctrl+Alt+M toggles manual menu mode, Ctrl+Alt+Q stops the
overlay. Closing the launcher exits. The app does not infer an internal game menu
from a Start/Back press. Mode, focus, profile, editor, power and exit transitions
release inputs.

The actual game has Tap To Run – Gamepad ON and Run on L3, so the approved profile
uses a deliberate double-tap pulse. Observed stick deadzone0.16 is reflected in
calibration. Swipe sensitivity remains bounded by the game's maximum turn rate.
Gyro defaults off and uses the ACPI AMD tablet sensor, not a controller IMU. Keep
the tablet still for the first 1.5-second calibration. Motion rejects calibration.
Sensitivity, axes, orientation, mode and calibration are editable.

Build with `src\GlassPad.XInputBridge\build.cmd`, then
`dotnet build src\GlassPad -c Release`. Engine checks:
`dotnet run --project src\GlassPad.Engine.Tests -c Release`. The original upstream
offline PadTest remains available. Full test records, current limitations and
owner checks are in `docs/` and `CONTINUATION.md`. A live physical input problem
reported by the owner is being debugged; passing engine tests alone are not
native gameplay acceptance.

Original GlassPad and dependency MIT notices are preserved. New engine/Queens
code is GPL-3.0-or-later; SwipeAim adapts BCDMouseJoystick from the touch fork.
See [FORK-NOTICES.md](FORK-NOTICES.md) and [COPYING](COPYING). No SF Symbols or game
assets are included.
