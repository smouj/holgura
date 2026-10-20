using System.Collections.Concurrent;
using Holgura.Contracts;

namespace Holgura.Engine;

/// <summary>
/// Shared implementation for "look inside these known directories and offer their contents".
/// Subclasses only decide which roots apply and which files qualify.
/// </summary>
public abstract class DirectoryCleanerModule : ICleanerModule
{
    public abstract string Id { get; }

    public abstract string DisplayName { get; }

    public ModuleCategory Category => ModuleCategory.System;

    public SafetyLevel SafetyLevel { get; protected init; } = SafetyLevel.Safe;

    protected abstract IEnumerable<ScanRoot> GetRoots(ScanContext context);

    protected virtual bool Include(FileEntry file, ScanRoot root, ScanContext context) =>
        root.MinimumAge is null || file.LastWriteUtc <= DateTime.UtcNow - root.MinimumAge.Value;

    public virtual Task<ScanResult> ScanAsync(ScanContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var items = new List<ScanItem>();
        var warnings = new List<string>();
        var index = 0;

        foreach (var root in GetRoots(context))
        {
            cancellationToken.ThrowIfCancellationRequested();
            index++;

            if (string.IsNullOrWhiteSpace(root.Path) || !context.FileSystem.DirectoryExists(root.Path))
            {
                warnings.Add($"'{root.Title}' no existe en este equipo.");
                continue;
            }

            long count = 0;
            long bytes = 0;
            DateTime lastWrite = DateTime.MinValue;
            var samples = new List<string>(Math.Min(context.MaxSampleFiles, 100));

            foreach (var file in context.FileSystem.EnumerateFiles(root.Path, root.Recursive))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!Include(file, root, context))
                {
                    continue;
                }

                count++;
                bytes += file.Length;
                if (file.LastWriteUtc > lastWrite)
                {
                    lastWrite = file.LastWriteUtc;
                }

                if (samples.Count < context.MaxSampleFiles)
                {
                    samples.Add(file.FullName);
                }
            }

            if (count == 0)
            {
                continue;
            }

            items.Add(new ScanItem
            {
                Id = $"{Id}:{index}",
                ModuleId = Id,
                Title = root.Title,
                Description = root.Description,
                Category = Category,
                SafetyLevel = root.SafetyLevel,
                RootPath = root.Path,
                FileCount = count,
                TotalBytes = bytes,
                LastWriteUtc = lastWrite == DateTime.MinValue ? null : new DateTimeOffset(lastWrite, TimeSpan.Zero),
                SampleFiles = samples,
                SelectedByDefault = root.SelectedByDefault && root.SafetyLevel == SafetyLevel.Safe,
            });
        }

        return Task.FromResult(new ScanResult { ModuleId = Id, Items = items, Warnings = warnings });
    }

    public virtual Task<CleanupResult> CleanAsync(CleanupContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var started = DateTimeOffset.UtcNow;
        var results = new ConcurrentBag<CleanupItemResult>();
        var freed = new long[1];

        var options = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = Math.Max(1, context.MaxParallelism),
        };

        // Chunked so a folder with hundreds of thousands of files cannot balloon the working set;
        // the whole point of this cleaner is to run on machines that are already short on memory.
        const int ChunkSize = 512;
        var buffer = new List<FileEntry>(ChunkSize);

        foreach (var file in context.FileSystem.EnumerateFiles(context.Item.RootPath, recursive: true))
        {
            options.CancellationToken.ThrowIfCancellationRequested();
            buffer.Add(file);

            if (buffer.Count < ChunkSize)
            {
                continue;
            }

            Parallel.ForEach(buffer, options, ProcessOne);
            buffer.Clear();
        }

        if (buffer.Count > 0)
        {
            Parallel.ForEach(buffer, options, ProcessOne);
        }

        return Task.FromResult(new CleanupResult
        {
            ModuleId = Id,
            ItemId = context.Item.Id,
            Items = results.OrderBy(r => r.Path, StringComparer.OrdinalIgnoreCase).ToArray(),
            Duration = DateTimeOffset.UtcNow - started,
            DryRun = context.DryRun,
            BytesFreed = context.DryRun ? 0 : freed[0],
        });

        void ProcessOne(FileEntry file)
        {
            options.CancellationToken.ThrowIfCancellationRequested();

            var decision = context.Gate.Evaluate(file.FullName);
            if (decision != CleanupDecision.Allow)
            {
                results.Add(new CleanupItemResult(file.FullName, decision switch
                {
                    CleanupDecision.Excluded => CleanupOutcome.Excluded,
                    CleanupDecision.NotFound => CleanupOutcome.NotFound,
                    _ => CleanupOutcome.SkippedAccessDenied,
                }, decision.ToString()));
                return;
            }

            // A file that appeared or changed after the scan is not the file we inspected.
            if (file.LastWriteUtc > context.ScanTimestampUtc.UtcDateTime.AddMinutes(5))
            {
                results.Add(new CleanupItemResult(file.FullName, CleanupOutcome.ChangedSinceScan));
                return;
            }

            if (context.DryRun)
            {
                results.Add(new CleanupItemResult(file.FullName, CleanupOutcome.WouldDelete));
                return;
            }

            try
            {
                context.Gate.Delete(file.FullName);
                Interlocked.Add(ref freed[0], file.Length);
                results.Add(new CleanupItemResult(file.FullName, CleanupOutcome.Deleted));
            }
            catch (UnauthorizedAccessException ex)
            {
                results.Add(new CleanupItemResult(file.FullName, CleanupOutcome.SkippedAccessDenied, ex.Message));
            }
            catch (IOException ex)
            {
                results.Add(new CleanupItemResult(file.FullName, CleanupOutcome.SkippedLocked, ex.Message));
            }
            catch (Exception ex)
            {
                results.Add(new CleanupItemResult(file.FullName, CleanupOutcome.Failed, ex.Message));
            }
        }
    }
}
