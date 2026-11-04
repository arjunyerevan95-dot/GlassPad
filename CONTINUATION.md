# Continuation — 5 November 2026

Active WO-GLASSPAD-QUEENS-001. Do not call this acceptance-complete yet.
Fork arjunyerevan95-dot/GlassPad, branch wo-glasspad-queens-001; upstream base
94fd8c46610a5c614dc938f06648e1f7a30c7d4c. Layout approved conditional on movable,
resizable controls and opacity; actual editor Save/Cancel/values checks pass.

Verified: upstream build/offline PadTest;1309engine assertions;254actual XInput
readbacks; native injected5-contact/partial-release/expiry/nonactivation; tablet
ACPI AMD gyro112callbacks and still calibration. These do not replace owner tests.
Logs and actual game binding observations are under docs/.

Owner reported gyro not working and look failing while moving. Reproduced the
look bug: whole-frame stationary samples from another finger zeroed fresh swipe.
Regression failed before, passes after exact-zero samples stopped changing motion
or expiry. R2 is LIVE from publish/QueensPilot-R2 for owner retest. Gyro calibration
had rejected motion with only a tiny status message; R2 has visible Start/progress/
cancel and ON/OFF. Owner R2 retest question is pending. Physical trace has observed
two concurrent contacts; four-contact acceptance remains unverified.

Current game controls: Tap To Run Gamepad ON, Run L3, stickDZ0.16, analog ON,
inversions/swaps OFF, sensitivity1, acceleration1. Approved profile uses DoubleTap
one100msL3 pulse/DZ.16; draft retained. No options changed. Five slots were empty;
owner entered an empty slot. Private pre-gameplay save/config backups are outside
repo/package in workspace/private-backup. Never overwrite saves to reach checks.

Game-local bridge INSTALLED00:30IST in game Binaries/Win64/xinput1_3.dll; hash
3C8E08CB63F66AE3F9E0B56D285D8A5265C67CAA6664573EC0144B0FDA253542. It forwards System32
XInput and maps the pilot pad to player0 only while its heartbeat is fresh. Game
loaded it, >700polls and actual menu D-pad response observed; expiry/disposal tests
pass. No driver installs or HC/HidHide/global remap/firmware changes. Receipt in
AppData/Roaming/GlassPad-Queens-Pilot/bridge-install.json; installer/remover verifies
exe/DLL hashes and preserves unknown files.

Latest source additionally includes runtime notices and a restore-defaults fix
using approved shipped JSON. These require final publish after owner R2 feedback.
Do not overwrite a running publish folder. CtrlAltQ now exits the R2 app; close
the old R1 launcher with native AltF4 if needed. Both old/new must not run together.

Next: read owner R2 result/live-status/trace; fix any remaining input issue; run
appropriate final offline/native/UI checks; measure performance with existing
PresentMon2.6 (ETW needs an elevation prompt); package self-contained x64 with
launcher/notices/bridge installer and exact source commit/bundle; push branch.
No final ZIP yet. No performance claim. Remaining equipment/progression and
physical sleep/resume/gyro direction checks must be recorded honestly.

Use native mcp__node_repl__js + @oai/sky for desktop, never another controller.
After minimized-window error: activate_window, get_window refresh, then state.
Coordinate actions need screenshot-backed geometry; AltF4 works for offscreen
close buttons. Refresh current game/window IDs; they change after restart.
Owner confirmed the reported restart was manual; don't infer a driver crash.

Old PC LAN links were killed by restarts. Restart workspace serve-deliverables.py
as a long-running direct exec, discover current IP/listener and verify full GET
hashes before linking. Exact allowlist only; no private backups/logs served.
No external file host. Root live-task-checkpoint.md holds desktop milestones.
