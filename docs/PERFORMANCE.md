# Performance status — 5 November 2026

No FPS, frame-time or performance-improvement result is claimed.

The installed PresentMon2.6 ETW capture refused to start without elevated access.
No account membership, privilege, graphics, TDP or frame-cap settings were changed.
A same-scene frame-time capture remains outstanding and needs owner involvement
for the elevation prompt and a stationary scene.

Process CPU/working-set sampling was attempted in the title menu, with and without
the overlay. The owner then entered gameplay and operated physical touch controls
during the comparison. The scene and activity differed, so the attempted comparison
was rejected rather than interpreted as overlay overhead. Raw samples remain
outside the release/source under workspace/evidence for diagnosis only.

For the remaining comparison: choose the same stationary gameplay view/settings,
capture at least 30 seconds without the overlay and 30 seconds with it, then a
second baseline to check drift. Record FPS and frame-time milliseconds separately,
plus overlay process CPU/working set. Repeat active multitouch/gyro responsiveness
separately. Never infer camera latency or FPS from process CPU percentages.
