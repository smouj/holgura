# Architecture

## Layers

```
Holgura.App (WPF / .NET 10)
    |
Holgura.Engine  -------- Holgura.Platform.Windows
    |                             |
Holgura.Core  --------------------+
    |
Holgura.Contracts
```

`Holgura.Contracts` has no dependencies. Everyone else may depend on it.

## Scan

`CleanerEngine.ScanAsync` iterates the registered modules. A module returns `ScanItem` records:
a root, exact candidate count and size, a safety level and a bounded file preview. The preview keeps
the largest 2,000 matching files per root so the UI can show and select individual files without
retaining an unbounded file tree. The aggregate count and size still cover every candidate.

`ProtectedPathPolicy` filters any Project Infest path during scan. `CleanupGate` repeats this check
at the deletion boundary, so a reported item or a later UI bug cannot expose those files for deletion.

## Clean

```
CleanerEngine.CleanAsync
  -> for each selected item
       -> build a CleanupGate over the item root (or ExternalDeletionGate for WSL)
       -> module.CleanAsync(context)
            -> enumerate candidates or match the explicit file allow-list
            -> compare selected file size and timestamp with the scan snapshot
            -> gate.Evaluate(path)   (containment, Project Infest, exclusions, reparse points, existence)
            -> gate.Delete(path)     (validator re-check, then delete)
```

The gate is the only object that can delete. `ExternalDeletionGate` throws on `Delete`, so a module
that claims to handle its own deletion cannot reach the file system through it by accident.

Selecting a whole category creates a bounded root rule over its known cache directory. Selecting
individual files sends an exact, case-insensitive path allow-list and their scan-time `FileEntry`
metadata; the cleaner skips files outside that list and preserves a selected file if its size or
last-write time changed. A missing selected path is reported as `NotFound`.

## Installed applications

`InstalledAppsService` reads the uninstall registry entries for the current user and machine in
32-bit and 64-bit views. It hides Windows system components and locks entries whose name, install
location, uninstall command or registry key contains the protected Project Infest marker.
`UninstallerService` parses the registered command with `CommandLineToArgvW`, resolves the executable
to an existing file, blocks shell and script interpreters, then launches the vendor's visible
uninstaller. Holgura never removes application directories itself. Microsoft Store apps are handed
to Windows Settings.

`DriveSpaceService` reads ready fixed volumes through `DriveInfo`; it does not crawl the entire disk.
The storage card therefore shows current free/used space while the scan total stays scoped to the
known cleanup modules.

## WSL

`IWslBridge` runs `wsl.exe` with an argument list (never a composed shell string) and decodes the
UTF-16 output that `wsl.exe` emits when redirected. Measurement and cleanup scripts are sent over
standard input to `bash -s`; this avoids Windows command-line quote transformations and CRLF errors.
Cleanup is a fixed recipe from `WslRecipes`; anything not on that list resolves to `null` and the
module fails closed.

Freeing space inside a distro does not shrink `ext4.vhdx`. Compaction needs `wsl --shutdown`
followed by `Optimize-VHD` (or `diskpart compact`) with administrator rights, and must never run
while the distro is mounted. The app reports this instead of pretending to have reclaimed it.

## Threading

Cleanup uses a bounded `Parallel.ForEach`; scans are sequential per module. Both honour the
`CancellationToken` passed from the UI.
