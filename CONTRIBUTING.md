# Contributing

## Ground rules

1. **Safety over features.** A pull request that adds a cleaner without a `PathSafetyValidator`
   test will not be merged.
2. **No new deletion paths.** Modules declare roots and candidates; the engine deletes.
3. **No telemetry, no network calls at runtime.**
4. **Tests are not optional.** Every module needs a test against the in-memory file system.

## Local workflow

```powershell
dotnet build -c Release
dotnet test -c Release
```

CI runs on `windows-latest` and must be green before review.

## Adding a cleaner

1. Derive from `DirectoryCleanerModule`.
2. Declare the roots and the safety level.
3. Add a fake-tree test that proves it cannot escape its root.
4. Add the module to `ModuleRegistry`.

## Commit style

Short imperative subject, one logical change per pull request.
