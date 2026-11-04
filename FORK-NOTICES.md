# GlassPad Queen’s Domain pilot — 4 November 2026

Fork of dansunmi/GlassPad at 94fd8c46610a5c614dc938f06648e1f7a30c7d4c.
Original GlassPad files remain under their MIT license (GlassPad-LICENSE.txt).
HIDMaestro 2.0.3 remains bundled for the upstream harness, with its original notices.

New Engine and Queens files are GPL-3.0-or-later; the combined fork is distributed
under GPL-3.0-or-later. See COPYING and the corresponding source in this fork.
SwipeAim.cs adapts BCDMouseJoystick.swift from arjunkl/bcd-touch, branch
wo-bcd-touch-001, commit 907c6ea4638d5a5d0af93b15b3fc091aff20f113,
whose per-file notice is GPL-3.0-or-later, original touch-fork implementation,
WO-BCD-TOUCH-001 (November 1, 2026). Modification: Windows absolute samples,
source/delivery validation and configurable response, November 4, 2026.
Other BCD sources were read as behavioral references. No BCD Material source
carrying the additional GPL section 7 terms, Apple SF Symbols, game assets,
game binaries, saves, or Apple system code is included.

Nefarius.ViGEm.Client 1.21.256: Copyright © Nefarius Software Solutions e.U.
2017–2023, MIT. https://github.com/nefarius/ViGEm.NET
This pilot uses the already installed ViGEm driver and creates only its own
temporary Xbox 360 target. It does not install drivers or modify Handheld
Companion, HidHide, Steam Input, physical-controller firmware or remappings.

Flat vector icons in Queens/Icons.cs are original drawings for this fork.
The optional game-local XInput bridge is new GPL-3.0-or-later source; it forwards
to the installed Microsoft System32 XInput library and does not redistribute it.
The self-contained package includes Microsoft .NET10.0.10 runtime notices under
notices/. The runtime is distributed under its own accompanying license.
This is an unofficial draft pilot; no endorsement by upstream authors is implied.
