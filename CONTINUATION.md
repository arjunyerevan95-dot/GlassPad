# Continuation — 5 November 2026, R4

WO-GLASSPAD-QUEENS-001. Owner CONFIRMED R4 simultaneous move + swipe look:
"Reliable now". Layout approved conditional on movable/resizable controls and
opacity; actual editor Save/Cancel/defaults checks pass. Do not call every
hardware/gameplay/performance acceptance item complete; see remaining checks below.

Fork: https://github.com/arjunyerevan95-dot/GlassPad
Branch: wo-glasspad-queens-001
Upstream base:94fd8c46610a5c614dc938f06648e1f7a30c7d4c.
Package BUILD.json records the exact source commit. Workspace publication receipt
records the verified GitHub commit/tree. Use matching source ZIP/bundle.

R3 still failed intermittently. The physical diagnostic captured3319 samples,
no raw cancels, and five losses of both router owners while Windows continued
sending both fingers every6-10ms. Background drawing timer starvation falsely
tripped the200ms UI watchdog. R4 renews heartbeat in the native window hook.
Sustained five-contact input with drawing ticks paused passes. Physical R4 trace
then exceeded8000 samples with zero unexplained owner losses and zero watchdog
releases, followed by owner confirmation. Earlier zero-delta frame handling fix
is also retained. Raw traces stay private in workspace/evidence and AppData.

Verified checks: upstream build/offline PadTest,1309 engine assertions,254 actual
XInput readbacks, native five-contact routing/partial release/expiry/nonactivation,
actual editor123 fields and persistence/defaults, tablet ACPI AMD gyro stream
and stationary calibration, real game binding pages and game-local bridge/menu
response. Read docs/TEST-RESULTS.md and individual logs for limits and later checks.

Game1.01.03 was discovered locally. Tap To Run Gamepad ON, Run L3, stickDZ0.16,
analog ON, inversions/swaps OFF, sensitivity1, acceleration1. Approved profile
uses a deliberate double tap sending one100ms L3 pulse and deadzone0.16. No game
options changed. Initial five slots empty; owner used an empty slot. Private
pre-gameplay save/config backup is workspace/private-backup. Never overwrite saves.

Game-local bridge remains in game Binaries/Win64/xinput1_3.dll, installed SHA256
3C8E08CB63F66AE3F9E0B56D285D8A5265C67CAA6664573EC0144B0FDA253542. It forwards System32
XInput and maps only this pilot pad to player0 while heartbeat is fresh. Loaded
by the actual game, >700 polls and D-pad menu response observed; route expiry and
disposal pass. Installer/remover recognizes this earlier exact build and its
packaged binary, preserving unknown DLLs. Receipt is in isolated AppData settings.
No driver installation or HC/HidHide/Steam Input/global remap/firmware changes.
Existing ViGEm kernel1.21.442 is used; HIDMaestro library2.0.3 is only retained for
upstream compatibility. Do not run the upstream live driver-installing harness.

Tablet gyro uses the verified Windows ACPI#AMDI0080 device, never a controller IMU.
A stationary calibration was saved ONLY to the personal profile. Shipped defaults
have no device bias and gyro always starts OFF. Actions > Gyro toggles after saved
calibration; otherwise visible Start/progress/cancel calibration is required.
Detached physical rotation/direction/feel still needs owner confirmation.

Release target: C:\Users\arjun\Documents\Codex\GlassPad-Queens-Pilot\1.1.0-R4.
Launch-GlassPad.cmd and Desktop shortcut start the overlay. R4 release is
self-contained Windows x64; SDK/runtime installation is unnecessary. Source build
uses SDK10/VS2022 BuildTools. tools/Package-QueensPilot.ps1 -Destination <new-folder>
reproduces the package. Never overwrite a running release. Isolated personal
settings are %AppData%\GlassPad-Queens-Pilot. Preserve custom profiles and backups.
Ctrl+Alt+G hides/recovers, Ctrl+Alt+M is MANUAL menu mode, Ctrl+Alt+Q exits fully.
App releases/reconnects through the existing driver after resume/disconnection.

Desktop automation became unavailable during the last handoff: native pipe could
not be found. Lightweight retry, node reset and final reinitialization all failed.
The game was no longer running. The idle test pilot was stopped via verified
process path; no game process was terminated. Native desktop work requires fresh
mcp__node_repl__js + @oai/sky inventory/state once the helper is available. Never
substitute another desktop controller or automate security/authentication UI.
Read shared live-task-checkpoint.md before desktop continuation.

Remaining: detached gyro direction/feel; physical four-contact movement + look +
held attack + jump/dash; native turn-rate/sensitivity/response calibration;
progression-dependent charge/special/throw/item behavior; physical sleep/resume
with held contacts. Owner's earlier restart was manual; no driver-crash conclusion.
PresentMon2.6 ETW capture needs owner-approved elevation; attempted CPU comparison
was rejected when scene/activity changed. No FPS/frame-time improvement claim.

Deliverables are in workspace/deliverables. serve-deliverables.py is a bounded
four-hour exact-allowlist LAN server; receipt is workspace/download-server.json.
Recheck current IP/listener and full GET hashes before sharing links. Restarts or
sleep can kill it. Same network and awake PC required. Never serve private saves,
backups, raw traces, source credentials or unrelated files. No external host.