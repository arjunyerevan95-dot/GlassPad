# Use and removal

Extract the whole x64 package, start Queen's Domain in borderless/windowed mode,
then open Launch-GlassPad.cmd. No SDK is required. Only this fork's temporary
virtual pad is created; existing controller settings are preserved.

If the game needs first-player compatibility, quit it and run
`tools\Install-QueensBridge.ps1 -GameExecutable "<shipping exe path>"` once. The
installer checks the verified executable SHA256, preserves unknown existing DLLs
and records a local installation receipt. Restart the game afterward.

Actions → More → Customize Controls opens the editor. Select and drag a control,
or use X/Y, Size and Opacity sliders. Global opacity is separate. Dotted circles
show effective targets. Save persists and keeps an atomic .bak; Cancel discards.
Import/export, defaults, assignments, zones, look and gyro calibration are present.

Actions can be selected by press-drag-release, or tap-open then select. Centre
cancels. Held actions open a Hold row; hold that row and lift to release. Drag
from a held attack to look. Promote frequent actions to direct controls in the
editor. Secondary attack is LT, not an assumed aim latch.

Menu mode is manual: More → Toggle menu mode or Ctrl+Alt+M. Return to gameplay
mode after closing game menus. Its controller D-pad/stick, Confirm/Back and
shoulders/triggers are separate from gameplay look/gyro. Ctrl+Alt+G hides/recovers;
a small upper-right Show tab remains. Ctrl+Alt+Q exits the app and releases its
controller. Closing the launcher also exits.

Gyro is off at launch. Select Gyro in Actions. If calibration is needed, keep the
tablet still and press Start in the visible setup panel; it settles for 600 ms
and samples for 1.5 seconds. A cancelled or moving calibration leaves gyro off.
After a saved calibration, selecting Gyro toggles it ON/OFF. The editor offers
recalibration, sensitivity, deadzone,
axis, display orientation and inversion. Menu/editor/hide/focus loss suppress it.

Settings are isolated in `%AppData%\GlassPad-Queens-Pilot`. Each game profile has
its own ID/JSON. Approved profiles match executable filename plus SHA256; manual
selection is available. Imported/unapproved profiles remain Draft and do not
become the permanent launch default. Original GlassPad settings are untouched.

To remove: exit the pilot and quit the game; run the bridge installer with
`-Remove -GameExecutable "<shipping exe path>"`. Only a DLL matching this fork's
hash is removed. Delete the extracted folder/shortcut if desired. To reset fork
settings, rename `%AppData%\GlassPad-Queens-Pilot` to a backup. No controller stack,
firmware or original layout restoration is necessary because none was changed.
