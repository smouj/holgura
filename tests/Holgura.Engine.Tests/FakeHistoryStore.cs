using Holgura.Core;

namespace Holgura.Engine.Tests;

public sealed class FakeHistoryStore : IHistoryStore
{
    public List<HistoryEntry> Entries { get; } = [];

    public Task AppendAsync(HistoryEntry entry, CancellationToken cancellationToken = default)
    {
        Entries.Add(entry);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<HistoryEntry>> LoadAsync(int limit = 100, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<HistoryEntry>>(Entries.Take(limit).ToArray());
}
