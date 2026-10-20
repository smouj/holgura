using System.Collections.Concurrent;
using Cleaner.Contracts;
using Cleaner.Core;

namespace Cleaner.Engine;

public sealed class CleanerEngine
{
    private readonly IReadOnlyList<ICleanerModule> _modules;
    private readonly IFileSystem _fileSystem;
    private readonly PathSafetyValidator _validator;
    private readonly ExclusionService _exclusions;
    private readonly IHistoryStore _history;

    public CleanerEngine(
        IEnumerable<ICleanerModule> modules,
        IFileSystem fileSystem,
        PathSafetyValidator validator,
        ExclusionService exclusions,
        IHistoryStore history)
    {
        ArgumentNullException.ThrowIfNull(modules);
        _modules = modules.ToArray();
        _fileSystem = fileSystem;
        _validator = validator;
        _exclusions = exclusions;
        _history = history;
    }

    public IReadOnlyList<ICleanerModule> Modules => _modules;

    public async Task<ScanReport> ScanAsync(ScanContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        var started = DateTimeOffset.UtcNow;
        var items = new List<ScanItem>();
        var warnings = new List<string>();

        foreach (var module in _modules)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var result = await module.ScanAsync(context, cancellationToken).ConfigureAwait(false);
                items.AddRange(result.Items);
                warnings.AddRange(result.Warnings);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                warnings.Add($"{module.DisplayName}: {ex.Message}");
            }
        }

        return new ScanReport
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Duration = DateTimeOffset.UtcNow - started,
            Items = items,
            Warnings = warnings,
        };
    }

    public async Task<CleanupReport> CleanAsync(
        IEnumerable<ScanItem> selectedItems,
        DateTimeOffset scanTimestampUtc,
        bool dryRun,
        int maxParallelism = 4,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selectedItems);
        var started = DateTimeOffset.UtcNow;
        var items = selectedItems.ToArray();
        var results = new List<CleanupResult>(items.Length);
        var titles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            titles[item.Id] = item.Title;
            var module = _modules.FirstOrDefault(m => string.Equals(m.Id, item.ModuleId, StringComparison.OrdinalIgnoreCase));

            if (module is null)
            {
                results.Add(new CleanupResult
                {
                    ModuleId = item.ModuleId,
                    ItemId = item.Id,
                    DryRun = dryRun,
                    Items = [new CleanupItemResult(item.RootPath, CleanupOutcome.Failed, "Module not registered.")],
                });
                continue;
            }

            ICleanupGate gate;
            if (item.DeletionScope == DeletionScope.External)
            {
                // WSL and other out-of-process targets validate themselves; the file gate must not
                // be used for them, and this gate refuses every file deletion.
                gate = new ExternalDeletionGate();
            }
            else
            {
                try
                {
                    gate = new CleanupGate(_validator, _exclusions, _fileSystem, item.RootPath);
                }
                catch (ProtectedPathException ex)
                {
                    results.Add(new CleanupResult
                    {
                        ModuleId = item.ModuleId,
                        ItemId = item.Id,
                        DryRun = dryRun,
                        Items = [new CleanupItemResult(item.RootPath, CleanupOutcome.Excluded, ex.Reason)],
                    });
                    continue;
                }
            }

            var context = new CleanupContext
            {
                Item = item,
                FileSystem = _fileSystem,
                Gate = gate,
                DryRun = dryRun,
                ScanTimestampUtc = scanTimestampUtc,
                MaxParallelism = maxParallelism,
            };

            results.Add(await module.CleanAsync(context, cancellationToken).ConfigureAwait(false));
        }

        var report = new CleanupReport
        {
            DryRun = dryRun,
            Duration = DateTimeOffset.UtcNow - started,
            Results = results,
        };

        try
        {
            await _history.AppendAsync(report.ToHistoryEntry(titles), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // History is a convenience; failing to write it must never fail a cleanup.
        }

        return report;
    }
}
