# Cities: Skylines II optimisation mods

- **DiagnosticMod**: logs FPS, memory, entity counts and errors every 60 s to `Logs\DiagnosticMod.log`. Read-only.
- **CleanupMod**: demolishes abandoned buildings. Experimental, test on a copy of a save.

Install: unzip `Mods.zip` from the Releases page into `%LOCALAPPDATA%Low\Colossal Order\Cities Skylines II\Mods`, then enable in Options > Mods.
Build: install the in-game Modding Toolchain, then `dotnet build -c Release` (root and `DiagnosticMod`).
