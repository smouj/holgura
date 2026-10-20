<p align="center">
  <img src="assets/logo.svg" width="104" height="104" alt="Cleaner" />
</p>

<h1 align="center">Cleaner</h1>

<p align="center">
  <strong>See it. Understand it. Clean it.</strong><br/>
  A Windows and WSL cleaner that shows you every file before it deletes anything.
</p>

<p align="center">
  <a href="https://github.com/smouj/cleaner/actions/workflows/ci.yml"><img alt="CI" src="https://img.shields.io/github/actions/workflow/status/smouj/cleaner/ci.yml?branch=main&label=build&logo=github"></a>
  <a href="https://github.com/smouj/cleaner/releases"><img alt="Release" src="https://img.shields.io/github/v/release/smouj/cleaner?include_prereleases&label=release"></a>
  <img alt="License" src="https://img.shields.io/github/license/smouj/cleaner">
  <img alt=".NET" src="https://img.shields.io/badge/.NET-10-512BD4">
  <img alt="Windows" src="https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D4">
  <img alt="Privacy" src="https://img.shields.io/badge/telemetry-none-2ea44f">
</p>

---

## Why another cleaner

Most Windows cleaners ask for trust and answer with a progress bar. This one answers with the file
list: every category, its size, its location and the reason it can go.

- Every deletion passes through a single gate that re-validates the root, the exclusions and the file.
- Drive roots, `Windows`, `Program Files` and your user folders can never become a deletion root.
- Junctions and reparse points are never followed.
- A locked file is skipped and reported, never fatal.
- Nothing is deleted until you press the button, and **Simular limpieza** shows the plan first.

## Features

| Area | Status | Notes |
|---|---|---|
| User `%TEMP%` | ✅ | Regenerated on demand |
| Windows `\Temp` | ✅ | Needs elevation for part of it; the rest is skipped and reported |
| Thumbnail cache | ✅ | Explorer rebuilds it |
| WSL distro caches | ✅ | apt, journald, pip, npm, uv — user caches cleaned directly |
| Dry run | ✅ | Nothing is touched |
| Exclusions | ✅ | Path prefixes and glob patterns |
| Local history | ✅ | JSON Lines, stays on the machine |
| Browser caches | 🚧 v0.3 | Cookies, history and passwords always excluded |
| Storage analyser | 🚧 v0.4 | — |
| Recycle bin | 🚧 v0.2 | — |

## Safety model

```
SCAN -> VALIDATE -> SELECT -> PRE-CLEAN VALIDATION -> DELETE -> VERIFY -> REPORT
```

1. A module only declares a root directory. It never receives delete rights.
2. `PathSafetyValidator` rejects drive roots, protected folders and any root that contains one.
3. `CleanupGate` re-checks containment, exclusions, reparse points and existence immediately before deleting.
4. Files newer than the scan (plus a five-minute tolerance) are reported as `ChangedSinceScan`.
5. A failure on one file is recorded and the run continues.
6. Candidate lists are processed in chunks of 512 so a huge folder cannot blow up memory.

## WSL

Windows cleaners cannot see inside `ext4.vhdx`, so WSL items are a separate path:

- `IWslBridge` runs `wsl.exe` with an argument list — never a composed shell string.
- Measurement is one `bash -lc` script that reports bytes and file counts per cache.
- A failed measurement is an error, not an empty result. The UI never claims "nothing to clean" when it could not look.
- Cleanup only runs a fixed recipe from `WslRecipes`; anything else resolves to `null` and fails closed.
- User-level caches are cleaned directly; `apt` and `journald` need passwordless `sudo` and are reported when unavailable.

Freeing space inside a distro does not shrink the VHDX. Compaction needs `wsl --shutdown` plus
`Optimize-VHD` with administrator rights, and the app says so instead of pretending.

## Install

Portable build — no installer, no service, no scheduled task:

1. Download `Cleaner-win-x64.zip` from [Releases](https://github.com/smouj/cleaner/releases).
2. Extract anywhere and run `Cleaner.App.exe`.

Diagnostics live in `%LOCALAPPDATA%\Cleaner\app.log`.

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
  Cleaner.Contracts/          Interfaces, results, safety levels. No dependencies.
  Cleaner.Core/               PathSafetyValidator, exclusions, history, formatting.
  Cleaner.Engine/             Scan/clean pipeline, CleanupGate, cleaner modules.
  Cleaner.Platform.Windows/   File system access, WSL bridge, Windows paths.
  Cleaner.App/                Desktop shell (WPF, MVVM).
tests/
  Cleaner.Core.Tests/
  Cleaner.Engine.Tests/
tools/
  SmokeTest/                  Headless scan + dry run against the real machine.
```

The engine knows nothing about the UI, and the UI knows nothing about deletion rules.

### A note on the shell

The first iteration targeted WinUI 3. Building it requires the *Appx* MSBuild tasks that only ship
with the Visual Studio "Windows application development" workload. The XAML layer therefore uses
WPF on .NET 10, which builds with the SDK alone. Everything below `Cleaner.App` is unchanged, so
swapping the shell is a single-project change.

## Verified on real hardware

```
== SCAN ==
items=3  files=985  bytes=805 MB  (3.5 s)  warnings=0
  514.0 MB | Review | C:\WINDOWS\Temp
  153.4 MB | Safe   | ...\Windows\Explorer (thumbnails)
  100.2 MB | Safe   | ...\AppData\Local\Temp

== DRY RUN ==
wouldDelete=1,002  skipped=0  errors=0  (0.7 s)

== SAFETY ==
rejects C:\, C:\Windows, C:\Program Files, C:\Users\<user>, Documents
```

27 unit tests cover the validator, the exclusions and the engine (locked files, reparse points,
protected roots, dry runs, cancellation and age filters).

## Roadmap

- **v0.1** foundation, engine, three system cleaners, WSL caches, tests, CI ✅
- **v0.2** Windows Temp hardening, recycle bin, Windows Error Reporting, crash dumps
- **v0.3** browser caches (Edge, Chrome, Firefox, Brave), with history, cookies and passwords always excluded
- **v0.4** storage analyser
- **v0.5** developer caches (NuGet, npm, pnpm, Yarn, pip, Gradle, Cargo, VS Code)
- **v0.6** richer exclusions and history details
- **v0.8** signed portable release
- **v1.0** public release

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). Security issues: [SECURITY.md](SECURITY.md).

## Privacy

Everything runs locally. No account, no server, no analytics, no update check that reports usage.
Your file names never leave the machine.

## License

MIT — see [LICENSE](LICENSE).
