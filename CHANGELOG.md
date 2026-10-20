# Changelog

## 0.1.0 - 2026-10-20

Initial iteration.

- Solution with Contracts, Core, Engine, Platform.Windows and a WPF shell.
- `PathSafetyValidator`, `CleanupGate`, exclusions and JSON-lines history.
- Cleaners: user temp, Windows temp, thumbnail cache.
- WSL integration: reads regenerable caches inside each distro and cleans user-level ones.
- Dry run, file preview and a confirmation step before any deletion.
- xUnit suites for the validator, exclusions and the engine (27 tests).
- WinUI 3 was evaluated first; it requires the Visual Studio Appx build tasks, so the shell uses WPF.
