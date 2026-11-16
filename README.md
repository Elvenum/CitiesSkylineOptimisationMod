# Cities: Skylines II optimisation mods

- **DiagnosticMod**: logs FPS, memory, entity counts and errors every 60 s to `Logs\DiagnosticMod.log`. Read-only.
- **CleanupMod**: demolishes abandoned buildings. Experimental, test on a copy of a save.

## Install (players)

Unzip `Mods.zip` from the Releases page into `%LOCALAPPDATA%Low\Colossal Order\Cities Skylines II\Mods`. No toolchain needed. The mods load on their own; if a playset is active, make sure they are enabled in it.

To check that DiagnosticMod is loaded, look for `Logs\DiagnosticMod.log` in the same game folder after a minute of play.

## Build (developers only)

Install the in-game Modding Toolchain, then run `dotnet build -c Release` in the root and in `DiagnosticMod`.
