# Architecture

## Layers

```
Cleaner.App (WinUI 3)
    |
Cleaner.Engine  -------- Cleaner.Platform.Windows
    |                             |
Cleaner.Core  --------------------+
    |
Cleaner.Contracts
```

`Cleaner.Contracts` has no dependencies. Everyone else may depend on it.

## Scan

`CleanerEngine.ScanAsync` iterates the registered modules. A module returns `ScanItem` records:
a root, a count, a total size, a capped sample list and a safety level. The engine never asks a
module to enumerate millions of paths at once; samples are capped and the full walk happens at
clean time.

## Clean

```
CleanerEngine.CleanAsync
  -> for each selected item
       -> build a CleanupGate over the item root (or ExternalDeletionGate for WSL)
       -> module.CleanAsync(context)
            -> enumerate candidates
            -> gate.Evaluate(path)   (containment, exclusions, reparse points, existence)
            -> gate.Delete(path)     (validator re-check, then delete)
```

The gate is the only object that can delete. `ExternalDeletionGate` throws on `Delete`, so a module
that claims to handle its own deletion cannot reach the file system through it by accident.

## WSL

`IWslBridge` runs `wsl.exe` with an argument list (never a composed shell string) and decodes the
UTF-16 output that `wsl.exe` emits when redirected. Measurement is a single `bash -lc` script.
Cleanup is a fixed recipe from `WslRecipes`; anything not on that list resolves to `null` and the
module fails closed.

Freeing space inside a distro does not shrink `ext4.vhdx`. Compaction needs `wsl --shutdown`
followed by `Optimize-VHD` (or `diskpart compact`) with administrator rights, and must never run
while the distro is mounted. The app reports this instead of pretending to have reclaimed it.

## Threading

Cleanup uses a bounded `Parallel.ForEach`; scans are sequential per module. Both honour the
`CancellationToken` passed from the UI.
