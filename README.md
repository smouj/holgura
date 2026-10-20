# Cleaner

**See it. Understand it. Clean it.**

A desktop cleaner for Windows that only removes what it can justify. No registry tweaks, no
"PC health" scores, no invented threats, no telemetry.

Cleaner scans the disposable parts of Windows and of your WSL distros, shows you every category
with its size, its location and the reason it can go, and lets you preview the exact files before
anything is deleted.

[![CI](https://github.com/smouj/cleaner/actions/workflows/ci.yml/badge.svg)](https://github.com/smouj/cleaner/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
![Windows](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D4)
![.NET](https://img.shields.io/badge/.NET-10-512BD4)

## Why another cleaner

Most Windows cleaners ask for trust and give you a progress bar. This one gives you the file list.

- Every deletion passes through a single gate that re-validates the root, the exclusions and the file.
- Protected paths (drive roots, Windows, Program Files, your user folders) can never become a deletion root.
- Reparse points and junctions are never followed.
- A locked file is skipped, not fatal.
- Nothing is deleted until you press the button, and **Simular limpieza** exists so you can see the plan first.

## Features

| Area | Status |
|---|---|
| User %TEMP% | Available |
| Windows Temp | Available (needs elevation for part of it) |
| Thumbnail cache | Available |
| WSL distro caches (apt, journald, pip, npm, uv) | Available |
| Dry run | Available |
| Exclusions (path prefix and glob patterns) | Available |
| Local history (JSON lines) | Available |
| Browser caches | Planned (v0.3) |
| Storage analyser | Planned (v0.4) |
| Recycle bin | Planned (v0.2) |

## Safety model

```
SCAN -> VALIDATE -> SELECT -> PRE-CLEAN VALIDATION -> DELETE -> VERIFY -> REPORT
```

1. A module can only declare a root directory. It never receives delete rights.
2. `PathSafetyValidator` rejects drive roots, protected folders and any root that contains one.
3. `CleanupGate` re-checks containment, exclusions and reparse points immediately before deleting.
4. Files newer than the scan (plus a five-minute tolerance) are reported as `ChangedSinceScan`.
5. A failure on one file is recorded and the run continues.

WSL is different by nature: the file gate cannot look inside `ext4.vhdx`. Those items are marked
`DeletionScope.External`, the engine hands them a gate that refuses every file deletion, and the
module may only run a fixed, whitelisted recipe inside the distro.

## Install

Portable build (no installer, no service, no scheduled task):

1. Download `Cleaner-win-x64.zip` from Releases.
2. Extract anywhere and run `Cleaner.App.exe`.

## Build

Requires the .NET 10 SDK on Windows.

```powershell
dotnet restore
dotnet build -c Release
dotnet test -c Release
dotnet publish src/Cleaner.App/Cleaner.App.csproj -c Release -r win-x64 --self-contained true
```

## Architecture

```
src/
  Cleaner.Contracts/         Interfaces, results, safety levels. No dependencies.
  Cleaner.Core/              PathSafetyValidator, exclusions, history, formatting.
  Cleaner.Engine/            Scan/clean pipeline, CleanupGate, cleaner modules.
  Cleaner.Platform.Windows/  File system, WSL bridge, Windows paths.
  Cleaner.App/               WPF shell (MVVM).
tests/
  Cleaner.Core.Tests/
  Cleaner.Engine.Tests/
```

The engine knows nothing about the UI, and the UI knows nothing about deletion rules.

### A note on the shell

The first iteration targeted WinUI 3. Building it needs the *Appx* MSBuild tasks that only ship with
the Visual Studio "Windows application development" workload, and this machine has Build Tools
without that workload. The XAML layer therefore uses WPF on .NET 10, which builds with the SDK
alone. Everything below `Cleaner.App` is unchanged, so swapping the shell later is a single-project
change.

## Roadmap

- **v0.1** foundation, engine, three system cleaners, WSL caches, tests, CI
- **v0.2** Windows Temp hardening, recycle bin, Windows Error Reporting, crash dumps
- **v0.3** browser caches (Edge, Chrome, Firefox, Brave), history/cookies/passwords always excluded
- **v0.4** storage analyser
- **v0.5** developer caches (NuGet, npm, pnpm, Yarn, pip, Gradle, Cargo, VS Code)
- **v0.6** richer exclusions and history details
- **v0.8** portable release packaging
- **v1.0** public release

## Privacy

Everything runs locally. The application has no account, no server, no analytics and no update
check that reports usage. Your file names never leave the machine.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). Security issues: [SECURITY.md](SECURITY.md).

## License

MIT. See [LICENSE](LICENSE).
