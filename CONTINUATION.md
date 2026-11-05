# Continuation — 5 November 2026, R3

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
or expiry. R3 is packaged for owner retest. Gyro calibration
had rejected motion with only a tiny status message; R2 has visible Start/progress/
cancel and ON/OFF. Owner R2 retest question is pending. Physical trace has observed
three concurrent contacts in live R3 gameplay; four-contact acceptance remains
unverified. A stationary
tablet calibration was saved locally to the owner's personal profile; shipped
defaults contain no device-specific bias and gyro always starts OFF.

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

R3 includes runtime notices and a tested restore-defaults fix using approved
shipped JSON. It clears inputs and reconnects an invalid virtual target after
resume/disconnection using the existing driver. Actual physical suspend/resume
needs retest. CtrlAltQ fully exits. Never run old/new builds together.

R3 repeated the native five-contact checks, editor/defaults checks and 254 actual
XInput reports successfully. Final local folder:
C:\Users\arjun\Documents\Codex\GlassPad-Queens-Pilot\1.1.0-R3. Its BUILD.json records
the exact published source commit. Launch-GlassPad.cmd and the Desktop shortcut
start it; tools/Package-QueensPilot.ps1 can reproduce a new release folder.
SDK10 and VS2022 BuildTools are needed only for source builds; release is
self-contained. Installer/remover recognizes the earlier exact bridge hash as
well as its own build, preserving unknown DLLs.

Next: read owner retest result/live-status/trace and fix any remaining input issue.
Hold movement while swiping; add held attack and jump/dash using four fingers.
Stop/lift look finger and confirm turning stops. Enable Gyro and rotate detached
tablet to check direction/feel. Check menu/hide/alt-tab and sleep/resume with held
contacts. Native camera calibration and progression-dependent attacks/items remain
open. Existing PresentMon2.6 needs user-approved elevation for ETW capture.
No FPS/frame-time improvement claim. See docs/PERFORMANCE.md for CPU-only samples.

At the last observation the owner was actively playing through R3 from workspace
publish/QueensPilot-R3. Do not terminate it or launch a second instance during play.
The final release folder has the same engine/backend fixes plus updated labels.

Workspace deliverables contain the binary ZIP, exact source ZIP/bundle and clean
actual WPF previews. Publication receipt is workspace/github-publication.json.

Use native mcp__node_repl__js + @oai/sky for desktop, never another controller.
After minimized-window error: activate_window, get_window refresh, then state.
Coordinate actions need screenshot-backed geometry; AltF4 works for offscreen
close buttons. Refresh current game/window IDs; they change after restart.
Owner confirmed the reported restart was manual; don't infer a driver crash.

Old PC LAN links were killed by restarts. Restart workspace serve-deliverables.py
as a long-running direct exec, discover current IP/listener and verify full GET
hashes before linking. Exact allowlist only; no private backups/logs served.
No external file host. Root live-task-checkpoint.md holds desktop milestones.
