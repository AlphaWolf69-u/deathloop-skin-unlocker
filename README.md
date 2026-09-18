# Deathloop Skin Unlocker

Unlock Colt's and Julianna's outfits and select them normally in the game.
Runs in the Windows notification area and reapplies unlocks when you return from a level.

## Download

Get **AlphaWolf's Deathloop Skin Unlocker.exe** from the [latest release](https://github.com/AlphaWolf69-u/deathloop-skin-unlocker/releases/latest).

## Use

1. Run the unlocker and Deathloop.
2. Open the loadout and choose your outfit in the game's outfit screen.
3. Keep the unlocker running while playing.

Right-click its tray icon to see the current status, pause unlocking, or exit.
The first connection may take a little time to locate the menu. Loading screens
are handled automatically. If access is denied, run the unlocker as administrator.

No Cheat Engine, Python, or anti-cheat disabling is required.
Unlocks are maintained while the app runs; this does not permanently grant account entitlements.

The app creates no settings files, logs, or save backups. Outfit choices are
remembered only in memory while it runs.

## Build

Run `build.ps1` on Windows with .NET Framework 4.x installed. The executable is
written to `dist`. No external packages are needed.

The app reads and writes outfit data in the game process. Menu refreshes use a
short-lived game-thread callback through the message-pump import; no debugger
is attached. The source includes layout checks for the memory it accesses.

Created by **AlphaWolf**.
