using Holgura.Contracts;

namespace Holgura.Engine.Modules;

/// <summary>
/// Reports and, on explicit request, empties the recycle bin. It never runs automatically and it is
/// not a file operation, so it is marked as an external deletion target.
/// </summary>
public sealed class RecycleBinCleaner(IRecycleBin recycleBin) : ICleanerModule
{
    public string Id => "system.recyclebin";

    public string DisplayName => "Papelera de reciclaje";

    public ModuleCategory Category => ModuleCategory.Storage;

    public SafetyLevel SafetyLevel => SafetyLevel.Review;

    public Task<ScanResult> ScanAsync(ScanContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var stats = recycleBin.Query();

        if (stats.ItemCount == 0)
        {
            return Task.FromResult(new ScanResult { ModuleId = Id });
        }

        var item = new ScanItem
        {
            Id = "system.recyclebin:1",
            ModuleId = Id,
            Title = "Papelera de reciclaje",
            Description = $"Los elementos que has eliminado y aún no se han purgado. Vaciarla es definitivo.",
            Category = Category,
            SafetyLevel = SafetyLevel,
            RootPath = "shell://recyclebin",
            FileCount = stats.ItemCount,
            TotalBytes = stats.TotalBytes,
            SampleFiles = ["(contenido de la papelera)"],
            SelectedByDefault = false,
            DeletionScope = DeletionScope.External,
        };

        return Task.FromResult(new ScanResult { ModuleId = Id, Items = [item] });
    }

    public Task<CleanupResult> CleanAsync(CleanupContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var started = DateTimeOffset.UtcNow;

        if (context.DryRun)
        {
            return Task.FromResult(new CleanupResult
            {
                ModuleId = Id,
                ItemId = context.Item.Id,
                DryRun = true,
                Duration = DateTimeOffset.UtcNow - started,
                Items = [new CleanupItemResult(context.Item.RootPath, CleanupOutcome.WouldDelete)],
            });
        }

        var before = recycleBin.Query();
        var ok = recycleBin.Empty();
        var after = recycleBin.Query();

        return Task.FromResult(new CleanupResult
        {
            ModuleId = Id,
            ItemId = context.Item.Id,
            Duration = DateTimeOffset.UtcNow - started,
            BytesFreed = ok ? Math.Max(0, before.TotalBytes - after.TotalBytes) : 0,
            Items = [new CleanupItemResult(
                context.Item.RootPath,
                ok ? CleanupOutcome.Deleted : CleanupOutcome.Failed,
                ok ? "Papelera vaciada." : "Windows no pudo vaciar la papelera.")],
        });
    }
}
