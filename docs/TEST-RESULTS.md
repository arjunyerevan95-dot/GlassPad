# Validation record — 5 November 2026

Base: `94fd8c46610a5c614dc938f06648e1f7a30c7d4c`. SDK10.0.302. Windows x64.
Original upstream build and offline PadTest pass; HIDMaestro library2.0.3 retained.
The normal pilot never calls its driver installer.

## Verified

- Engine: **1,309 assertions** pass. Ownership, duplicate contacts, same-action
  owners, floating neutral/re-anchor/clamp, source/delivery/FPS phase matrix,
  expiry, slow corrections, delayed/out-of-order events, no backlog, attack-drag,
  radial cancellation/hold semantics, sprint gestures, profile roundtrip/validation,
  DPI scaling, gyro composition and focus/mode release are covered.
- Actual WPF editor:123 fields; scroll extent3495 > viewport599. Cancel preserves
  original and disk. Save persists position, size and both opacity levels atomically.
  Restore Defaults restores the approved layout, double-tap sprint and deadzone0.16.
- Existing ViGEm driver1.21.442: **254 actual XInput readback reports** pass,
  including simultaneous engine state, triggers, axes/buttons/hats,100 press/release
  cycles, neutralization, disposal and reconnection. No driver installation.
- Tablet gyro: verified Windows ACPI AMDI0080 source;112 live callbacks and a
  successful stationary calibration with zero observed stick drift in that test.
  Default off and menu/focus suspension pass. Controller IMUs are excluded.
- Native touch: OS-injected five-contact test on the actual overlay passes
  move+look+attack+jump+dash, expiry while buttons remain held, partial release
  preserving other fingers, final neutral and nonactivation. The legacy touch
  path lost contacts and was replaced by WM_POINTER whole-frame history.
- Game: actual binding menus match the supplied controller mapping. Tap-to-run
  Gamepad ON, Run L3, deadzone0.16, analog ON, swaps/inversions OFF, look sensitivity
  1.00 and acceleration1.0 were observed. No game options were changed.
- Game-local bridge routing/timeout/disposal checks pass. The game loaded it and
  polled it >700 times during a bounded D-pad test; menu response was observed.
- Self-contained x64 build launches with the game, aligns visibly to its client
  area and connects its temporary controller. Manual menu mode is visible.

## Owner-discovered bug and R2 repair

Physical testing revealed that swipe look stopped while movement was held. Whole
pointer frames repeat stationary samples for other fingers; those zero deltas
were cancelling fresh swipe output. A regression first failed, then passed after
stationary duplicates stopped changing the last motion timestamp/value/expiry.
The watchdog still neutralizes genuine stopped motion. See the before/final logs.

The owner also reported gyro not working. The initial calibration had rejected
tablet motion but communicated that only in a small status strip. R2 adds a
visible Start-calibration panel, settling interval, progress state and cancel,
and labels the radial entry ON/OFF. Gyro remains off if calibration fails.

R3 repeats all five-contact native checks and 254 live XInput readbacks successfully.
A stationary tablet calibration was saved locally to the owner's isolated profile;
device-specific bias is excluded from the shipped default. After an extended
interruption, the older target reported InvalidTarget. R3 clears input, tolerates
an already disconnected target and reconnects through the existing driver after
resume or a disconnection. Actual physical suspend/resume still needs retesting.

The owner resumed gameplay in R3 and the live trace reached three simultaneous
physical contacts, but reported move+look still fails intermittently ("very spotty").
The earlier zero-delta repair was insufficient; the R4 diagnosis below followed.
Injection is not
physical touchscreen evidence; observed real touch contacts are not full gameplay
acceptance. Charged/special/throw/item semantics depend on available equipment.

## R4 sustained-input repair

The owner's diagnostic captured 3,319 raw physical samples with no cancellation
events. Five times, the router lost both contact roles while Windows continued
delivering both fingers every 6–10 ms. Ownership disappeared about 200 ms after
the second finger arrived. The watchdog used a Background-priority drawing timer
as its UI heartbeat, so sustained native input could falsely look like a UI freeze.

R4 also renews the heartbeat when the native window hook dispatches messages.
The independent publisher still releases inputs if the UI actually stops
progressing. The native integration check now pauses background drawing and
delivers 80 sustained five-contact updates for >600 ms: all owners/buttons remain,
swipe output is present, stationary expiry and final release still pass. R4's
physical trace then exceeded 8,000 samples with no unexplained owner losses and
zero watchdog releases. The owner confirmed: **"Reliable now"** for move + swipe look.

The actual overlay's independent publisher also neutralizes a held action during
a deliberate 275 ms UI-thread freeze. This check passes independently of touch
injection. During the last handoff the native desktop pipe disappeared and the
game was no longer running. An initial native recheck failed first-contact
delivery. A new self-target guard confirmed the input lab was not the visible
target at its injection points and withheld further injection. The final repeat
of that native portion is therefore unavailable in the changed desktop session;
the earlier R4 native pass and the owner's real gameplay confirmation remain the
evidence for sustained touch. No broad desktop automation fallback was used.

## Remaining checks

Four-contact physical move/look/attack/jump-or-dash; detached-tablet gyro direction
and feel after calibration; native camera turn-rate/deadzone/response calibration;
progression-dependent actions; sleep/resume with physical held contacts.

PresentMon2.6 ETW capture requires elevation (access denied without it). No FPS,
frame-time or performance-improvement claim is made. No graphics/TDP/frame-cap
settings were changed. The owner confirmed the reported restart was manual.
An attempted CPU comparison was rejected because the scene changed during the
sample; see PERFORMANCE.md.

Existing saves/config were backed up locally outside the repo and package. The
initial five game slots were empty; live testing started in an empty slot.

```powershell
dotnet run --project src/GlassPad.PadTest -c Release -- --offline
dotnet run --project src/GlassPad.Engine.Tests -c Release
dotnet run --project src/GlassPad.SystemTest -c Release -- --live
dotnet run --project src/GlassPad.SystemTest -c Release -- --gyro-live
GlassPad.Queens.exe --ui-checks <output-directory>
GlassPad.Queens.exe --input-checks <output-directory>
dotnet publish src/GlassPad -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o publish/QueensPilot
```

The broad live report sweep refuses to drive a foreground game. The injected-touch
integration lab emits no controller output. Native game pulse mode is explicit.
