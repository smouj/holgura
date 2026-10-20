using Holgura.Contracts;

namespace Holgura.Engine.Modules;

/// <summary>
/// Reads the regenerable caches inside each WSL distro. User-level caches are cleaned directly;
/// system-level ones (apt, journald) need passwordless sudo and are reported but never removed
/// behind the user's back.
/// </summary>
public sealed class WslCacheModule(IWslBridge bridge) : ICleanerModule
{
    private static readonly Dictionary<string, (string Title, string Description, SafetyLevel Level)> Catalog = new(StringComparer.OrdinalIgnoreCase)
    {
        ["apt"] = ("Caché de APT", "Paquetes .deb descargados en /var/cache/apt/archives. APT los vuelve a descargar si hacen falta.", SafetyLevel.Safe),
        ["journal"] = ("Registros de systemd", "/var/log/journal. Se conservan los últimos 100 MB.", SafetyLevel.Review),
        ["pip"] = ("Caché de pip", "Ruedas y paquetes de Python en ~/.cache/pip.", SafetyLevel.Safe),
        ["npm"] = ("Caché de npm", "Contenido de ~/.npm/_cacache. npm lo regenera al instalar.", SafetyLevel.Safe),
        ["uv"] = ("Caché de uv", "Entornos y ruedas de uv en ~/.cache/uv.", SafetyLevel.Safe),
    };

    public string Id => "wsl.caches";

    public string DisplayName => "Cachés dentro de WSL";

    public ModuleCategory Category => ModuleCategory.Development;

    public SafetyLevel SafetyLevel => SafetyLevel.Safe;

    public async Task<ScanResult> ScanAsync(ScanContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var items = new List<ScanItem>();
        var warnings = new List<string>();

        if (!await bridge.IsAvailableAsync(cancellationToken).ConfigureAwait(false))
        {
            return new ScanResult { ModuleId = Id, Warnings = ["WSL no está disponible en este equipo."] };
        }

        var distros = await bridge.ListDistrosAsync(cancellationToken).ConfigureAwait(false);
        foreach (var distro in distros)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyList<WslUsage> usage;
            try
            {
                usage = await bridge.MeasureAsync(distro.Name, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                warnings.Add($"WSL {distro.Name}: {ex.Message}");
                continue;
            }

            foreach (var entry in usage.Where(u => u.Bytes > 0 && Catalog.ContainsKey(u.Key)))
            {
                var meta = Catalog[entry.Key];
                items.Add(new ScanItem
                {
                    Id = $"wsl.caches:{distro.Name}:{entry.Key}",
                    ModuleId = Id,
                    Title = $"{meta.Title} · {distro.Name}",
                    Description = meta.Description,
                    Category = Category,
                    SafetyLevel = meta.Level,
                    RootPath = $"wsl://{distro.Name}/{entry.Key}",
                    FileCount = entry.FileCount,
                    TotalBytes = entry.Bytes,
                    SampleFiles = [$"/{entry.Key}"],
                    SelectedByDefault = meta.Level == SafetyLevel.Safe,
                    DeletionScope = DeletionScope.External,
                });
            }
        }

        return new ScanResult { ModuleId = Id, Items = items, Warnings = warnings };
    }

    public async Task<CleanupResult> CleanAsync(CleanupContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var started = DateTimeOffset.UtcNow;
        var (distro, key) = Parse(context.Item.RootPath);

        if (distro is null || key is null)
        {
            return Fail(context, started, "Item path is not a WSL reference.");
        }

        if (context.DryRun)
        {
            return new CleanupResult
            {
                ModuleId = Id,
                ItemId = context.Item.Id,
                DryRun = true,
                Duration = DateTimeOffset.UtcNow - started,
                Items = [new CleanupItemResult(context.Item.RootPath, CleanupOutcome.WouldDelete)],
            };
        }

        int exit;
        try
        {
            exit = await bridge.CleanAsync(distro, key, dryRun: false, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return Fail(context, started, ex.Message);
        }

        if (exit != 0)
        {
            var outcome = exit == 3 ? CleanupOutcome.SkippedAccessDenied : CleanupOutcome.Failed;
            return new CleanupResult
            {
                ModuleId = Id,
                ItemId = context.Item.Id,
                Duration = DateTimeOffset.UtcNow - started,
                Items = [new CleanupItemResult(context.Item.RootPath, outcome, $"Exit code {exit}. Puede requerir sudo sin contraseña.")],
            };
        }

        long freed = context.Item.TotalBytes;
        try
        {
            var after = await bridge.MeasureAsync(distro, cancellationToken).ConfigureAwait(false);
            var remaining = after.FirstOrDefault(u => string.Equals(u.Key, key, StringComparison.OrdinalIgnoreCase))?.Bytes ?? 0;
            freed = Math.Max(0, context.Item.TotalBytes - remaining);
        }
        catch (Exception)
        {
            // Keep the estimate; the cleanup itself succeeded.
        }

        return new CleanupResult
        {
            ModuleId = Id,
            ItemId = context.Item.Id,
            Duration = DateTimeOffset.UtcNow - started,
            BytesFreed = freed,
            Items = [new CleanupItemResult(context.Item.RootPath, CleanupOutcome.Deleted, $"Freed {freed} bytes")],
        };
    }

    private CleanupResult Fail(CleanupContext context, DateTimeOffset started, string message) => new()
    {
        ModuleId = Id,
        ItemId = context.Item.Id,
        Duration = DateTimeOffset.UtcNow - started,
        Items = [new CleanupItemResult(context.Item.RootPath, CleanupOutcome.Failed, message)],
    };

    private static (string? Distro, string? Key) Parse(string rootPath)
    {
        if (!rootPath.StartsWith("wsl://", StringComparison.OrdinalIgnoreCase))
        {
            return (null, null);
        }

        var rest = rootPath["wsl://".Length..];
        var separator = rest.LastIndexOf('/');
        return separator <= 0 ? (null, null) : (rest[..separator], rest[(separator + 1)..]);
    }
}
