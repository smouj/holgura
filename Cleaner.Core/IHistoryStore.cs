namespace Cleaner.Core;

public interface IHistoryStore
{
    Task AppendAsync(HistoryEntry entry, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HistoryEntry>> LoadAsync(int limit = 100, CancellationToken cancellationToken = default);
}
